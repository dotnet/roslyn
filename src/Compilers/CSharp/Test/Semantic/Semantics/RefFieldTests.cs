// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RefFieldTests : CSharpTestBase
    {
        private static bool IsNet70OrGreater()
        {
#if NET7_0_OR_GREATER
            return true;
#else
            return false;
#endif
        }

        private static string IncludeExpectedOutput(string expectedOutput) => IsNet70OrGreater() ? expectedOutput : null;

        [CombinatorialData]
        [Theory]
        public void LanguageVersionDiagnostics(bool useCompilationReference)
        {
            var sourceA =
@"public ref struct S<T>
{
    public ref T F1;
    public ref readonly T F2;
    public S(ref T t)
    {
        F1 = ref t;
        F2 = ref t;
    }
    S(object unused, T t0)
    {
        this = default;
        this = new S<T>();
        this = new S<T> { F1 = t0 };
    }
    static void M1(T t1)
    {
        S<T> s1;
        s1 = default;
        s1 = new S<T>();
        s1 = new S<T> { F1 = t1 };
    }
    static void M2(ref T t2)
    {
        S<T> s2;
        s2 = new S<T>(ref t2);
        s2 = new S<T> { F1 = t2 };
    }
    static void M3(S<T> s3)
    {
        var other = s3;
        M1(s3.F1);
        M1(s3.F2);
        M2(ref s3.F1);
    }
    void M4(T t4)
    {
        this = default;
        this = new S<T>();
        this = new S<T> { F1 = t4 };
    }
    void M5(S<T> s5)
    {
        var other = this;
        M1(F1);
        M1(F2);
        M2(ref F1);
        M1(this.F1);
        M1(this.F2);
        M2(ref this.F1);
        M1(s5.F1);
        M1(s5.F2);
        M2(ref s5.F1);
    }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref T F1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref T").WithArguments("ref fields", "11.0").WithLocation(3, 12),
                // (4,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref readonly T F2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref readonly T").WithArguments("ref fields", "11.0").WithLocation(4, 12));

            comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program
{
    static void M1<T>(T t)
    {
        S<T> s1;
        s1 = default;
        s1 = new S<T>();
        s1 = new S<T> { F1 = t };
    }
    static void M2<T>(ref T t)
    {
        S<T> s2;
        s2 = new S<T>(ref t);
        s2 = new S<T> { F1 = t };
    }
    static void M3<T>(S<T> s)
    {
        var s3 = s;
        M1(s.F1);
        M1(s.F2);
        M2(ref s.F1);
    }
}";

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (8,25): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         s1 = new S<T> { F1 = t };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "F1").WithArguments("ref fields", "11.0").WithLocation(8, 25),
                // (14,25): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         s2 = new S<T> { F1 = t };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "F1").WithArguments("ref fields", "11.0").WithLocation(14, 25),
                // (19,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         M1(s.F1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F1").WithArguments("ref fields", "11.0").WithLocation(19, 12),
                // (20,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         M1(s.F2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F2").WithArguments("ref fields", "11.0").WithLocation(20, 12),
                // (21,16): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         M2(ref s.F1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F1").WithArguments("ref fields", "11.0").WithLocation(21, 16));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F1"), "ref T S<T>.F1", RefKind.Ref, new string[0]);
            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F2"), "ref readonly T S<T>.F2", RefKind.RefReadOnly, new string[0]);

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F1"), "ref T S<T>.F1", RefKind.Ref, new string[0]);
            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F2"), "ref readonly T S<T>.F2", RefKind.RefReadOnly, new string[0]);
        }

        [CombinatorialData]
        [Theory]
        public void RefField(bool useCompilationReference)
        {
            var sourceA =
@"public ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref T F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref T").WithArguments("ref fields", "11.0").WithLocation(3, 12));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref T S<T>.F", RefKind.Ref, new string[0]);

            comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);
            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref T S<T>.F", RefKind.Ref, new string[0]);

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        int x = 1;
        var s = new S<int>(ref x);
        s.F = 2;
        Console.WriteLine(s.F);
        Console.WriteLine(x);
        x = 3;
        Console.WriteLine(s.F);
        Console.WriteLine(x);
    }
}";

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (8,9): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         s.F = 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F").WithArguments("ref fields", "11.0").WithLocation(8, 9),
                // (9,27): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         Console.WriteLine(s.F);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F").WithArguments("ref fields", "11.0").WithLocation(9, 27),
                // (12,27): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         Console.WriteLine(s.F);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F").WithArguments("ref fields", "11.0").WithLocation(12, 27));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref T S<T>.F", RefKind.Ref, new string[0]);

            var verifier = CompileAndVerify(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular11, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"2
2
3
3
"));
            comp = (CSharpCompilation)verifier.Compilation;
            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref T S<T>.F", RefKind.Ref, new string[0]);
        }

        [CombinatorialData]
        [Theory]
        public void RefReadonlyField(bool useCompilationReference)
        {
            var sourceA =
@"public ref struct S<T>
{
    public ref readonly T F;
    public S(in T t)
    {
        F = ref t;
    }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref readonly T F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref readonly T").WithArguments("ref fields", "11.0").WithLocation(3, 12));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref readonly T S<T>.F", RefKind.RefReadOnly, new string[0]);

            comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);
            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref readonly T S<T>.F", RefKind.RefReadOnly, new string[0]);

            var sourceB =
@"using System;
class A
{
    internal int G;
}
class Program
{
    static void Main()
    {
        A a = new A();
        a.G = 1;
        var s = new S<A>(in a);
        s.F.G = 2;
        Console.WriteLine(s.F.G);
        Console.WriteLine(a.G);
        a.G = 3;
        Console.WriteLine(s.F.G);
        Console.WriteLine(a.G);
    }
}";

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         s.F.G = 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F").WithArguments("ref fields", "11.0").WithLocation(13, 9),
                // (14,27): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         Console.WriteLine(s.F.G);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F").WithArguments("ref fields", "11.0").WithLocation(14, 27),
                // (17,27): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         Console.WriteLine(s.F.G);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "s.F").WithArguments("ref fields", "11.0").WithLocation(17, 27));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref readonly T S<T>.F", RefKind.RefReadOnly, new string[0]);

            var verifier = CompileAndVerify(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular11, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"2
2
3
3
"));
            comp = (CSharpCompilation)verifier.Compilation;
            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref readonly T S<T>.F", RefKind.RefReadOnly, new string[0]);
        }

        [Fact]
        public void SubstitutedField()
        {
            var sourceA =
@".class public A<T>
{
  .field public !0& modopt(object) modopt(int8) F
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"#pragma warning disable 169
class B
{
    static A<int> A;
}";
            var comp = CreateCompilation(sourceB, new[] { refA });
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp);

            var field = (SubstitutedFieldSymbol)comp.GetMember<FieldSymbol>("B.A").Type.GetMember("F");
            VerifyFieldSymbol(field, "ref modopt(System.SByte) modopt(System.Object) System.Int32 A<System.Int32>.F", RefKind.Ref, new[] { "System.SByte", "System.Object" });
        }

        [Fact]
        public void RetargetingField()
        {
            var sourceA =
@"public ref struct A
{
    public ref readonly int F;
}
";
            var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Mscorlib40);
            var refA = comp.ToMetadataReference();

            var sourceB =
@"#pragma warning disable 169
ref struct B
{
    A A;
}";
            comp = CreateCompilation(sourceB, new[] { refA }, targetFramework: TargetFramework.Mscorlib45);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, verify: Verification.Skipped);

            var field = (RetargetingFieldSymbol)comp.GetMember<FieldSymbol>("B.A").Type.GetMember("F");
            // Currently, source symbols cannot declare RefCustomModifiers. If that
            // changes, update this test to verify retargeting of RefCutomModifiers.
            VerifyFieldSymbol(field, "ref readonly System.Int32 A.F", RefKind.RefReadOnly, new string[0]);
        }

        [Fact]
        public void TupleField()
        {
            var sourceA =
@".class public sealed System.ValueTuple`2<T1, T2> extends [mscorlib]System.ValueType
{
  .field public !T1& Item1
  .field public !T2& modopt(int8) modopt(object) Item2
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"class B
{
    static (int, object) F() => default;
}";
            var comp = CreateCompilation(sourceB, targetFramework: TargetFramework.Mscorlib40, references: new[] { refA });
            comp.VerifyEmitDiagnostics();

            var tupleType = (NamedTypeSymbol)comp.GetMember<MethodSymbol>("B.F").ReturnType;
            VerifyFieldSymbol(tupleType.GetField("Item1"), "ref System.Int32 (System.Int32, System.Object).Item1", RefKind.Ref, new string[0] { });
            VerifyFieldSymbol(tupleType.GetField("Item2"), "ref modopt(System.Object) modopt(System.SByte) System.Object (System.Int32, System.Object).Item2", RefKind.Ref, new[] { "System.Object", "System.SByte" });
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void EmbeddedField()
        {
            var sourceA =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly A
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = {string('_.dll')}
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = {string('EE8A431D-7D0C-4E13-80C7-52B9C93B9B76')}
}
.class public sealed S extends [mscorlib]System.ValueType
{
  .field public int32& modopt(object) modopt(int32) F
}";
            var refA = CompileIL(sourceA, prependDefaultHeader: false, embedInteropTypes: true);

            var sourceB =
@"class Program
{
    static void Main()
    {
        F(new S());
    }
    static void F(object o)
    {
    }
}";
            var comp = CreateCompilation(sourceB, references: new[] { refA, CSharpRef });
            var refB = comp.EmitToImageReference();

            comp = CreateCompilation("", new[] { refB });
            var module = (PEModuleSymbol)comp.GetReferencedAssemblySymbol(refB).Modules[0];
            // Read from metadata directly to inspect the embedded type.
            var decoder = new MetadataDecoder(module);
            var reader = module.Module.MetadataReader;
            var fieldHandle = reader.FieldDefinitions.Single(handle => reader.GetString(reader.GetFieldDefinition(handle).Name) == "F");
            var fieldInfo = decoder.DecodeFieldSignature(fieldHandle);
            Assert.True(fieldInfo.IsByRef);
            Assert.Equal(new[] { "System.Int32", "System.Object" }, fieldInfo.RefCustomModifiers.SelectAsArray(m => m.Modifier.ToTestDisplayString()));
        }

        [Fact]
        public void FixedField_01()
        {
            var source =
@"unsafe ref struct S
{
    public fixed ref int F1[3];
    public fixed ref readonly int F2[3];
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (3,26): error CS9049: A fixed field must not be a ref field.
                //     public fixed ref int F1[3];
                Diagnostic(ErrorCode.ERR_FixedFieldMustNotBeRef, "F1").WithLocation(3, 26),
                // (4,35): error CS9049: A fixed field must not be a ref field.
                //     public fixed ref readonly int F2[3];
                Diagnostic(ErrorCode.ERR_FixedFieldMustNotBeRef, "F2").WithLocation(4, 35));
        }

        [Fact]
        public void FixedField_02()
        {
            var sourceA =
@".class public sealed S extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .class sequential nested public sealed FixedBuffer extends [mscorlib]System.ValueType
  {
    .pack 0
    .size 12
    .field public int32 FixedElementField
  }
  .field public valuetype S/FixedBuffer& F
  .custom instance void [mscorlib]System.Runtime.CompilerServices.FixedBufferAttribute::.ctor(class [mscorlib]System.Type, int32) = { type([mscorlib]System.Int32) int32(3) }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class Program
{
    unsafe static void Main()
    {
        var s = new S();
        Console.WriteLine(s.F[1]);
    }
}";
            var comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (7,29): error CS0570: 'S.F' is not supported by the language
                //         Console.WriteLine(s.F[1]);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F").WithArguments("S.F").WithLocation(7, 29));
        }

        [Fact]
        public void Volatile()
        {
            var sourceA =
@".class public sealed R extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public int32& modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) F1
  .field public int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile)& F2
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        var r = new R();
        Console.WriteLine(r.F1);
        Console.WriteLine(r.F2);
    }
}";
            var comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (7,29): error CS0570: 'R.F1' is not supported by the language
                //         Console.WriteLine(r.F1);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("R.F1").WithLocation(7, 29),
                // (8,29): error CS0570: 'R.F2' is not supported by the language
                //         Console.WriteLine(r.F2);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F2").WithArguments("R.F2").WithLocation(8, 29));
        }

        [Fact]
        public void Modifiers()
        {
            var source =
@"#pragma warning disable 0169
ref struct R
{
    static ref int _s1;
    static ref readonly int _s2;
    const ref int _c1 = default;
    const ref readonly int _c2 = default;
    volatile ref int _v1;
    volatile ref readonly int _v2;
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,20): error CS0106: The modifier 'static' is not valid for this item
                //     static ref int _s1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_s1").WithArguments("static").WithLocation(4, 20),
                // (5,29): error CS0106: The modifier 'static' is not valid for this item
                //     static ref readonly int _s2;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_s2").WithArguments("static").WithLocation(5, 29),
                // (6,19): error CS0106: The modifier 'static' is not valid for this item
                //     const ref int _c1 = default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_c1").WithArguments("static").WithLocation(6, 19),
                // (6,19): error CS0106: The modifier 'const' is not valid for this item
                //     const ref int _c1 = default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_c1").WithArguments("const").WithLocation(6, 19),
                // (7,28): error CS0106: The modifier 'static' is not valid for this item
                //     const ref readonly int _c2 = default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_c2").WithArguments("static").WithLocation(7, 28),
                // (7,28): error CS0106: The modifier 'const' is not valid for this item
                //     const ref readonly int _c2 = default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_c2").WithArguments("const").WithLocation(7, 28),
                // (8,22): error CS0106: The modifier 'volatile' is not valid for this item
                //     volatile ref int _v1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_v1").WithArguments("volatile").WithLocation(8, 22),
                // (9,31): error CS0106: The modifier 'volatile' is not valid for this item
                //     volatile ref readonly int _v2;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_v2").WithArguments("volatile").WithLocation(9, 31));
        }

        /// <summary>
        /// Unexpected modreq().
        /// </summary>
        [Fact]
        public void RefCustomModifiers_UseSiteDiagnostic_02()
        {
            var sourceA =
@".class public sealed A extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public int32& modreq(int32) F
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        var a = new A();
        Console.WriteLine(a.F);
    }
}";
            var comp = CreateCompilation(sourceB, new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (7,29): error CS0570: 'A.F' is not supported by the language
                //         Console.WriteLine(a.F);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F").WithArguments("A.F").WithLocation(7, 29));
        }

        /// <summary>
        /// modreq(System.Runtime.InteropServices.InAttribute).
        /// Should we allow this modreq() even though it is not generated by the compiler?
        /// </summary>
        [Fact]
        public void RefCustomModifiers_UseSiteDiagnostic_01()
        {
            var sourceA =
@".class public sealed A extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) F
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        var a = new A();
        Console.WriteLine(a.F);
    }
}";
            var comp = CreateCompilation(sourceB, new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (7,29): error CS0570: 'A.F' is not supported by the language
                //         Console.WriteLine(a.F);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F").WithArguments("A.F").WithLocation(7, 29));
        }

        /// <summary>
        /// modopt() with missing type.
        /// </summary>
        [Fact]
        public void RefCustomModifiers_UseSiteDiagnostic_03()
        {
            var sourceA =
@".assembly extern mscorlib { }
.assembly A { }
.class public A
{
}";
            var refA = CompileIL(sourceA, prependDefaultHeader: false);

            var sourceB =
@".assembly extern A { }
.class public sealed B extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public int32& modopt(int8) modopt([A]A) F
}";
            var refB = CompileIL(sourceB);

            var sourceC =
@"using System;
class Program
{
    static void Main()
    {
        var b = new B();
        Console.WriteLine(b.F);
    }
}";
            var comp = CreateCompilation(sourceC, new[] { refB });
            comp.VerifyEmitDiagnostics(
                // (7,29): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         Console.WriteLine(b.F);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "F").WithArguments("A", "A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 29));
        }

        [Fact]
        public void MemberRefMetadataDecoder_FindFieldBySignature()
        {
            var sourceA =
@".class public sealed R<T> extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field private !0 F1
  .field public !0& F1
  .field private !0& modopt(int32) F2
  .field public !0& modopt(object) F2
  .field private int32& F3
  .field public int8& F3
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"class B
{
    static object F1() => new R<object>().F1;
    static object F2() => new R<object>().F2;
    static int F3() => new R<object>().F3;
}";
            var verifier = CompileAndVerify(sourceB, new[] { refA }, verify: Verification.Skipped);
            // MemberRefMetadataDecoder.FindFieldBySignature() is used to find fields when realIL: true.
            verifier.VerifyIL("B.F1", realIL: true, expectedIL:
@"{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (R<object> V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""R<object>""
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""ref object R<object>.F1""
  IL_000e:  ldind.ref
  IL_000f:  ret
}");

            var refB = verifier.Compilation.EmitToImageReference();

            var comp = CreateCompilation("", references: new[] { refA, refB });
            comp.VerifyEmitDiagnostics();

            // Call MemberRefMetadataDecoder.FindFieldBySignature() indirectly from MetadataDecoder.GetSymbolForILToken().
            var module = (PEModuleSymbol)comp.GetReferencedAssemblySymbol(refB).Modules[0];

            var decoder = new MetadataDecoder(module);
            var reader = module.Module.MetadataReader;
            var fieldReferences = reader.MemberReferences.
                Where(handle => reader.GetString(reader.GetMemberReference(handle).Name) is "F1" or "F2" or "F3").
                Select(handle => decoder.GetSymbolForILToken(handle)).
                ToArray();

            var containingType = fieldReferences[0].ContainingType;
            var fieldMembers = containingType.GetMembers().WhereAsArray(m => m.Kind == SymbolKind.Field);
            var expectedMembers = new[]
            {
                "System.Object R<System.Object>.F1",
                "ref System.Object R<System.Object>.F1",
                "ref modopt(System.Int32) System.Object R<System.Object>.F2",
                "ref modopt(System.Object) System.Object R<System.Object>.F2",
                "ref System.Int32 R<System.Object>.F3",
                "ref System.SByte R<System.Object>.F3"
            };
            AssertEx.Equal(expectedMembers, fieldMembers.ToTestDisplayStrings());

            var expectedReferences = new[]
            {
                "ref System.Object R<System.Object>.F1",
                "ref modopt(System.Object) System.Object R<System.Object>.F2",
                "ref System.SByte R<System.Object>.F3"
            };
            AssertEx.Equal(expectedReferences, fieldReferences.ToTestDisplayStrings());
        }

        [WorkItem(62596, "https://github.com/dotnet/roslyn/issues/62596")]
        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void NonRefStructContainer(string type)
        {
            var source =
$@"#pragma warning disable 169
{type} R
{{
    ref int F;
}}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (4,5): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     ref int F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref int").WithArguments("ref fields", "11.0").WithLocation(4, 5),
                // (4,13): error CS9059: A ref field can only be declared in a ref struct.
                //     ref int F;
                Diagnostic(ErrorCode.ERR_RefFieldInNonRefStruct, "F").WithLocation(4, 13));

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,13): error CS9059: A ref field can only be declared in a ref struct.
                //     ref int F;
                Diagnostic(ErrorCode.ERR_RefFieldInNonRefStruct, "F").WithLocation(4, 13));
        }

        /// <summary>
        /// Determination of enum underlying type should ignore ref fields
        /// and fields with required custom modifiers.
        /// </summary>
        [Fact]
        public void EnumUnderlyingType()
        {
            var sourceA =
@".class public sealed E extends [mscorlib]System.Enum
{
  .field public int64 modreq(object) value1
  .field public int32& modopt(object) value2
  .field public int32& value3
  .field public int16 value4
  .field public static literal valuetype E A = int16(0x01)
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"class Program
{
    static void Main()
    {
        _ = E.A;
    }
}";
            var comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics();

            var type = (NamedTypeSymbol)comp.GetTypeByMetadataName("E");
            Assert.Equal(SpecialType.System_Int16, type.EnumUnderlyingType.SpecialType);
        }

        private static void VerifyFieldSymbol(FieldSymbol field, string expectedDisplayString, RefKind expectedRefKind, string[] expectedRefCustomModifiers)
        {
            Assert.Equal(expectedRefKind, field.RefKind);
            Assert.Equal(expectedRefCustomModifiers, field.RefCustomModifiers.SelectAsArray(m => m.Modifier.ToTestDisplayString()));
            Assert.Equal(expectedDisplayString, field.ToTestDisplayString());
        }

        [WorkItem(62131, "https://github.com/dotnet/roslyn/issues/62131")]
        [CombinatorialData]
        [Theory]
        public void RuntimeFeature(bool useCompilationReference)
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public class Attribute
    {
    }
}";
            var sourceB =
@"namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        public const string ByRefFields = nameof(ByRefFields);
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA }, parseOptions: TestOptions.Regular10);
            var refA = AsReference(comp, useCompilationReference);

            comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular10);
            var refAB = AsReference(comp, useCompilationReference);

            var source =
@"ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static ref T F<T>(R<T> r)
    {
        return ref r.F;
    }
}";

            comp = CreateEmptyCompilation(source, references: new[] { refA }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref T F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref T").WithArguments("ref fields", "11.0").WithLocation(3, 12),
                // (10,20): error CS8167: Cannot return by reference a member of parameter 'r' because it is not a ref or out parameter
                //         return ref r.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r").WithArguments("r").WithLocation(10, 20));
            Assert.False(comp.Assembly.RuntimeSupportsByRefFields);

            comp = CreateEmptyCompilation(source, references: new[] { refAB }, parseOptions: TestOptions.Regular10);
            // https://github.com/dotnet/roslyn/issues/62131: Enable updated escape rules if
            // System.Runtime.CompilerServices.RuntimeFeature.ByRefFields exists.
            comp.VerifyDiagnostics(
                // (3,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref T F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref T").WithArguments("ref fields", "11.0").WithLocation(3, 12),
                // (10,20): error CS8167: Cannot return by reference a member of parameter 'r' because it is not a ref or out parameter
                //         return ref r.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r").WithArguments("r").WithLocation(10, 20));
            Assert.True(comp.Assembly.RuntimeSupportsByRefFields);

            comp = CreateEmptyCompilation(source, references: new[] { refA });
            comp.VerifyDiagnostics();
            Assert.False(comp.Assembly.RuntimeSupportsByRefFields);

            comp = CreateEmptyCompilation(source, references: new[] { refAB });
            comp.VerifyDiagnostics();
            Assert.True(comp.Assembly.RuntimeSupportsByRefFields);
        }

        /// <summary>
        /// Ref fields of ref struct type are not supported.
        /// </summary>
        [Fact]
        public void RefFieldTypeRefStruct_01()
        {
            var source =
@"#pragma warning disable 169
ref struct R1<T>
{
}
ref struct R2<T>
{
    public ref R1<T> F;
}
class Program
{
    static void F(ref R1<int> r1)
    {
        var r2 = new R2<int>();
        r2.F = ref r1;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,12): error CS9050: A ref field cannot refer to a ref struct.
                //     public ref R1<T> F;
                Diagnostic(ErrorCode.ERR_RefFieldCannotReferToRefStruct, "ref R1<T>").WithLocation(7, 12));
        }

        /// <summary>
        /// Ref fields of ref struct type are not supported.
        /// </summary>
        [Fact]
        public void RefFieldTypeRefStruct_02()
        {
            var sourceA =
@".class public sealed R1 extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
}
.class public sealed R2 extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public valuetype R1& F
}
";
            var refA = CompileIL(sourceA);

            var sourceB =
@"class Program
{
    static void F(ref R1 r1)
    {
        var r2 = new R2();
        r2.F = ref r1;
    }
}";
            var comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (6,12): error CS0570: 'R2.F' is not supported by the language
                //         r2.F = ref r1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "F").WithArguments("R2.F").WithLocation(6, 12));
        }

        [Fact]
        public void RefFields_RefEscape()
        {
            var source =
@"ref struct R<T>
{
    ref T F;
    R(ref T t) { F = ref t; }
    ref T F0() => ref this.F;
    static ref T F1(R<T> r1)
    {
        return ref r1.F;
    }
    static ref T F2(T t)
    {
        var r2 = new R<T>(ref t);
        return ref r2.F;
    }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
                // (3,5): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     ref T F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref T").WithArguments("ref fields", "11.0").WithLocation(3, 5),
                // (5,23): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     ref T F0() => ref this.F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this.F").WithLocation(5, 23),
                // (8,20): error CS8167: Cannot return by reference a member of parameter 'r1' because it is not a ref or out parameter
                //         return ref r1.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r1").WithArguments("r1").WithLocation(8, 20),
                // (13,20): error CS8169: Cannot return a member of local 'r2' by reference because it is not a ref local
                //         return ref r2.F;
                Diagnostic(ErrorCode.ERR_RefReturnLocal2, "r2").WithArguments("r2").WithLocation(13, 20)
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (13,20): error CS8352: Cannot use variable 'r2' in this context because it may expose referenced variables outside of their declaration scope
                //         return ref r2.F;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r2.F").WithArguments("r2").WithLocation(13, 20),
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void RefFields_RefReassignment()
        {
            var source =
@"ref struct R<T>
{
    ref T F;
    R(ref T t) { F = ref t; }
    R<T> F0(ref T t)
    {
        F = ref t;
        return this;
    }
    static R<T> F1(R<T> r1, ref T t)
    {
        r1.F = ref t;
        return r1;
    }
    static R<T> F2(R<T> r2)
    {
        T t = default;
        r2.F = ref t;
        return r2;
    }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
                // (3,5): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     ref T F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref T").WithArguments("ref fields", "11.0").WithLocation(3, 5)
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (18,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r2.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r2.F = ref t").WithArguments("F", "t").WithLocation(18, 9)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void RefThis()
        {
            var source =
@"struct S<T>
{
    ref S<T> F() => ref this;
}
ref struct R<T>
{
    ref R<T> F() => ref this;
}";

            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (3,25): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     ref S<T> F() => ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(3, 25),
                // (7,25): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     ref R<T> F() => ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(7, 25)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void RefParameter()
        {
            var source =
@"class C { }
struct S { }
ref struct R { }
class Program
{
    static ref C F1(ref C c) => ref c;
    static ref S F2(ref S s) => ref s;
    static ref R F3(ref R r) => ref r;
    static ref readonly C F4(in C c) => ref c;
    static ref readonly S F5(in S s) => ref s;
    static ref readonly R F6(in R r) => ref r;
    static ref C F7(out C c) { c = default; return ref c; } // 1
    static ref S F8(out S s) { s = default; return ref s; } // 2
    static ref R F9(out R r) { r = default; return ref r; } // 3
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (12,56): error CS8166: Cannot return a parameter by reference 'c' because it is not a ref parameter
                //     static ref C F7(out C c) { c = default; return ref c; } // 1
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "c").WithArguments("c").WithLocation(12, 56),
                // (13,56): error CS8166: Cannot return a parameter by reference 's' because it is not a ref parameter
                //     static ref S F8(out S s) { s = default; return ref s; } // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "s").WithArguments("s").WithLocation(13, 56),
                // (14,56): error CS8166: Cannot return a parameter by reference 'r' because it is not a ref parameter
                //     static ref R F9(out R r) { r = default; return ref r; } // 3
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "r").WithArguments("r").WithLocation(14, 56)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Lvalue_01()
        {
            var source =
@"struct S
{
    internal ref T F0<T>(T t) => throw null;
    internal ref T F1<T>(ref T t) => throw null;
    internal ref T F2<T>(in T t) => throw null;
    internal ref T F3<T>(out T t) => throw null;
}
class Program
{
    static ref T F00<T>() { S s = default; T t = default; return ref s.F0(t); }
    static ref T F01<T>() { S s = default; T t = default; return ref s.F1(ref t); } // 1
    static ref T F02<T>() { S s = default; T t = default; return ref s.F2(in t); } // 2
    static ref T F03<T>() { S s = default; T t = default; return ref s.F2(t); } // 3
    static ref T F04<T>() { S s = default; T t = default; return ref s.F3(out t); }
    static ref T F10<T>(ref T t) { S s = default; return ref s.F0(t); }
    static ref T F11<T>(ref T t) { S s = default; return ref s.F1(ref t); }
    static ref T F12<T>(ref T t) { S s = default; return ref s.F2(in t); }
    static ref T F13<T>(ref T t) { S s = default; return ref s.F2(t); }
    static ref T F14<T>(ref T t) { S s = default; return ref s.F3(out t); }
    static ref T F20<T>(in T t) { S s = default; return ref s.F0(t); }
    static ref T F22<T>(in T t) { S s = default; return ref s.F2(in t); }
    static ref T F23<T>(in T t) { S s = default; return ref s.F2(t); }
    static ref T F30<T>(out T t) { S s = default; t = default; return ref s.F0(t); }
    static ref T F31<T>(out T t) { S s = default; t = default; return ref s.F1(ref t); } // 4
    static ref T F32<T>(out T t) { S s = default; t = default; return ref s.F2(in t); } // 5
    static ref T F33<T>(out T t) { S s = default; t = default; return ref s.F2(t); } // 6
    static ref T F34<T>(out T t) { S s = default; t = default; return ref s.F3(out t); }
    static ref T F41<T>(ref S s) { T t = default; return ref s.F0(t); }
    static ref T F42<T>(in S s) { T t = default; return ref s.F1(ref t); } // 7
    static ref T F43<T>(out S s) { s = default; T t = default; return ref s.F2(in t); } // 8
    static ref T F51<T>(ref S s, ref T t) { return ref s.F0(t); }
    static ref T F52<T>(in S s, ref T t) { return ref s.F1(ref t); }
    static ref T F53<T>(out S s, ref T t) { s = default; return ref s.F2(in t); }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
                // (11,70): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F01<T>() { S s = default; T t = default; return ref s.F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(11, 70),
                // (11,79): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F01<T>() { S s = default; T t = default; return ref s.F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(11, 79),
                // (12,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F02<T>() { S s = default; T t = default; return ref s.F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(12, 70),
                // (12,78): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F02<T>() { S s = default; T t = default; return ref s.F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(12, 78),
                // (13,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F03<T>() { S s = default; T t = default; return ref s.F2(t); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(13, 70),
                // (13,75): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F03<T>() { S s = default; T t = default; return ref s.F2(t); } // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(13, 75),
                // (14,70): error CS8347: Cannot use a result of 'S.F3<T>(out T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F04<T>() { S s = default; T t = default; return ref s.F3(out t); }
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F3(out t)").WithArguments("S.F3<T>(out T)", "t").WithLocation(14, 70),
                // (14,79): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F04<T>() { S s = default; T t = default; return ref s.F3(out t); }
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(14, 79),
                // (29,61): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F42<T>(in S s) { T t = default; return ref s.F1(ref t); } // 7
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(29, 61),
                // (29,70): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F42<T>(in S s) { T t = default; return ref s.F1(ref t); } // 7
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(29, 70),
                // (30,75): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F43<T>(out S s) { s = default; T t = default; return ref s.F2(in t); } // 8
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(30, 75),
                // (30,83): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F43<T>(out S s) { s = default; T t = default; return ref s.F2(in t); } // 8
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(30, 83)
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (11,70): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F01<T>() { S s = default; T t = default; return ref s.F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(11, 70),
                // (11,79): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F01<T>() { S s = default; T t = default; return ref s.F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(11, 79),
                // (12,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F02<T>() { S s = default; T t = default; return ref s.F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(12, 70),
                // (12,78): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F02<T>() { S s = default; T t = default; return ref s.F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(12, 78),
                // (13,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F03<T>() { S s = default; T t = default; return ref s.F2(t); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(13, 70),
                // (13,75): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F03<T>() { S s = default; T t = default; return ref s.F2(t); } // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(13, 75),
                // (24,75): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F31<T>(out T t) { S s = default; t = default; return ref s.F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(24, 75),
                // (24,84): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static ref T F31<T>(out T t) { S s = default; t = default; return ref s.F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(24, 84),
                // (25,75): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F32<T>(out T t) { S s = default; t = default; return ref s.F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(25, 75),
                // (25,83): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static ref T F32<T>(out T t) { S s = default; t = default; return ref s.F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(25, 83),
                // (26,75): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F33<T>(out T t) { S s = default; t = default; return ref s.F2(t); } // 6
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(26, 75),
                // (26,80): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static ref T F33<T>(out T t) { S s = default; t = default; return ref s.F2(t); } // 6
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(26, 80),
                // (29,61): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F42<T>(in S s) { T t = default; return ref s.F1(ref t); } // 7
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(29, 61),
                // (29,70): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F42<T>(in S s) { T t = default; return ref s.F1(ref t); } // 7
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(29, 70),
                // (30,75): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static ref T F43<T>(out S s) { s = default; T t = default; return ref s.F2(in t); } // 8
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(30, 75),
                // (30,83): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static ref T F43<T>(out S s) { s = default; T t = default; return ref s.F2(in t); } // 8
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(30, 83)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Lvalue_02()
        {
            var source =
@"class C
{
    ref T F0<T>(T t) => throw null;
    ref T F1<T>(ref T t) => throw null;
    ref T F2<T>(in T t) => throw null;
    ref T F3<T>(out T t) => throw null;

    ref T F00<T>() { T t = default; return ref F0(t); }
    ref T F01<T>() { T t = default; return ref F1(ref t); } // 1
    ref T F02<T>() { T t = default; return ref F2(in t); } // 2
    ref T F03<T>() { T t = default; return ref F2(t); } // 3
    ref T F04<T>() { T t = default; return ref F3(out t); }
    ref T F10<T>(ref T t) { return ref F0(t); }
    ref T F11<T>(ref T t) { return ref F1(ref t); }
    ref T F12<T>(ref T t) { return ref F2(in t); }
    ref T F13<T>(ref T t) { return ref F2(t); }
    ref T F14<T>(ref T t) { return ref F3(out t); }
    ref T F20<T>(in T t) { return ref F0(t); }
    ref T F22<T>(in T t) { return ref F2(in t); }
    ref T F23<T>(in T t) { return ref F2(t); }
    ref T F30<T>(out T t) { t = default; return ref F0(t); }
    ref T F31<T>(out T t) { t = default; return ref F1(ref t); } // 4
    ref T F32<T>(out T t) { t = default; return ref F2(in t); } // 5
    ref T F33<T>(out T t) { t = default; return ref F2(t); } // 6
    ref T F34<T>(out T t) { t = default; return ref F3(out t); }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
                // (9,48): error CS8347: Cannot use a result of 'C.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F01<T>() { T t = default; return ref F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(ref t)").WithArguments("C.F1<T>(ref T)", "t").WithLocation(9, 48),
                // (9,55): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     ref T F01<T>() { T t = default; return ref F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(9, 55),
                // (10,48): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F02<T>() { T t = default; return ref F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(in t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(10, 48),
                // (10,54): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     ref T F02<T>() { T t = default; return ref F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(10, 54),
                // (11,48): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F03<T>() { T t = default; return ref F2(t); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(11, 48),
                // (11,51): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     ref T F03<T>() { T t = default; return ref F2(t); } // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(11, 51),
                // (12,48): error CS8347: Cannot use a result of 'C.F3<T>(out T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F04<T>() { T t = default; return ref F3(out t); }
                Diagnostic(ErrorCode.ERR_EscapeCall, "F3(out t)").WithArguments("C.F3<T>(out T)", "t").WithLocation(12, 48),
                // (12,55): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     ref T F04<T>() { T t = default; return ref F3(out t); }
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(12, 55)
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (9,48): error CS8347: Cannot use a result of 'C.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F01<T>() { T t = default; return ref F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(ref t)").WithArguments("C.F1<T>(ref T)", "t").WithLocation(9, 48),
                // (9,55): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     ref T F01<T>() { T t = default; return ref F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(9, 55),
                // (10,48): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F02<T>() { T t = default; return ref F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(in t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(10, 48),
                // (10,54): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     ref T F02<T>() { T t = default; return ref F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(10, 54),
                // (11,48): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F03<T>() { T t = default; return ref F2(t); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(11, 48),
                // (11,51): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     ref T F03<T>() { T t = default; return ref F2(t); } // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(11, 51),
                // (22,53): error CS8347: Cannot use a result of 'C.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F31<T>(out T t) { t = default; return ref F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(ref t)").WithArguments("C.F1<T>(ref T)", "t").WithLocation(22, 53),
                // (22,60): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     ref T F31<T>(out T t) { t = default; return ref F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(22, 60),
                // (23,53): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F32<T>(out T t) { t = default; return ref F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(in t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(23, 53),
                // (23,59): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     ref T F32<T>(out T t) { t = default; return ref F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(23, 59),
                // (24,53): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     ref T F33<T>(out T t) { t = default; return ref F2(t); } // 6
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(24, 53),
                // (24,56): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     ref T F33<T>(out T t) { t = default; return ref F2(t); } // 6
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(24, 56)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Lvalue_03()
        {
            var source =
@"class Program
{
    static ref T F0<T, U>(T t, U u) => throw null;
    static ref T F1<T, U>(T t, ref U u) => throw null;
    static ref T F2<T, U>(T t, in U u) => throw null;
    static ref T F3<T, U>(T t, out U u) => throw null;
    static ref T F4<T, U>(ref T t, ref U u) => throw null;
    static ref T F5<T, U>(ref T t, in U u) => throw null;
    static ref T F6<T, U>(ref T t, out U u) => throw null;
    static ref T F7<T, U>(in T t, in U u) => throw null;
    static ref T F8<T, U>(in T t, out U u) => throw null;
    static ref T F9<T, U>(out T t, out U u) => throw null;
    static ref T G0<T, U>()
    {
        T t = default;
        U u = default;
        return ref F0(t, u);
    }
    static ref T G1<T, U>()
    {
        T t = default;
        U u = default;
        return ref F1(t, ref u); // 1
    }
    static ref T G2A<T, U>()
    {
        T t = default;
        U u = default;
        return ref F2(t, in u); // 2
    }
    static ref T G2B<T, U>()
    {
        T t = default;
        U u = default;
        return ref F2(t, u); // 3
    }
    static ref T G3<T, U>()
    {
        T t = default;
        U u = default;
        return ref F3(t, out u); // *
    }
    static ref T G4<T, U>()
    {
        T t = default;
        U u = default;
        return ref F4(ref t, ref u); // 4
    }
    static ref T G5A<T, U>()
    {
        T t = default;
        U u = default;
        return ref F5(ref t, in u); // 5
    }
    static ref T G5B<T, U>()
    {
        T t = default;
        U u = default;
        return ref F5(ref t, u); // 6
    }
    static ref T G6<T, U>()
    {
        T t = default;
        U u = default;
        return ref F6(ref t, out u); // 7
    }
    static ref T G7A<T, U>()
    {
        T t = default;
        U u = default;
        return ref F7(in t, in u); // 8
    }
    static ref T G7B<T, U>()
    {
        T t = default;
        U u = default;
        return ref F7(t, u); // 9
    }
    static ref T G8A<T, U>()
    {
        T t = default;
        U u = default;
        return ref F8(in t, out u); // 10
    }
    static ref T G8B<T, U>()
    {
        T t = default;
        U u = default;
        return ref F8(t, out u); // 11
    }
    static ref T G9<T, U>()
    {
        T t = default;
        U u = default;
        return ref F9(out t, out u); // *
    }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
                // (23,20): error CS8347: Cannot use a result of 'Program.F1<T, U>(T, ref U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return ref F1(t, ref u); // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(t, ref u)").WithArguments("Program.F1<T, U>(T, ref U)", "u").WithLocation(23, 20),
                // (23,30): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return ref F1(t, ref u); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(23, 30),
                // (29,20): error CS8347: Cannot use a result of 'Program.F2<T, U>(T, in U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return ref F2(t, in u); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t, in u)").WithArguments("Program.F2<T, U>(T, in U)", "u").WithLocation(29, 20),
                // (29,29): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return ref F2(t, in u); // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(29, 29),
                // (35,20): error CS8347: Cannot use a result of 'Program.F2<T, U>(T, in U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return ref F2(t, u); // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t, u)").WithArguments("Program.F2<T, U>(T, in U)", "u").WithLocation(35, 20),
                // (35,26): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return ref F2(t, u); // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(35, 26),
                // (41,20): error CS8347: Cannot use a result of 'Program.F3<T, U>(T, out U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return ref F3(t, out u); // *
                Diagnostic(ErrorCode.ERR_EscapeCall, "F3(t, out u)").WithArguments("Program.F3<T, U>(T, out U)", "u").WithLocation(41, 20),
                // (41,30): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return ref F3(t, out u); // *
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(41, 30),
                // (47,20): error CS8347: Cannot use a result of 'Program.F4<T, U>(ref T, ref U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F4(ref t, ref u); // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "F4(ref t, ref u)").WithArguments("Program.F4<T, U>(ref T, ref U)", "t").WithLocation(47, 20),
                // (47,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F4(ref t, ref u); // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(47, 27),
                // (53,20): error CS8347: Cannot use a result of 'Program.F5<T, U>(ref T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F5(ref t, in u); // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "F5(ref t, in u)").WithArguments("Program.F5<T, U>(ref T, in U)", "t").WithLocation(53, 20),
                // (53,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F5(ref t, in u); // 5
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(53, 27),
                // (59,20): error CS8347: Cannot use a result of 'Program.F5<T, U>(ref T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F5(ref t, u); // 6
                Diagnostic(ErrorCode.ERR_EscapeCall, "F5(ref t, u)").WithArguments("Program.F5<T, U>(ref T, in U)", "t").WithLocation(59, 20),
                // (59,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F5(ref t, u); // 6
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(59, 27),
                // (65,20): error CS8347: Cannot use a result of 'Program.F6<T, U>(ref T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F6(ref t, out u); // 7
                Diagnostic(ErrorCode.ERR_EscapeCall, "F6(ref t, out u)").WithArguments("Program.F6<T, U>(ref T, out U)", "t").WithLocation(65, 20),
                // (65,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F6(ref t, out u); // 7
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(65, 27),
                // (71,20): error CS8347: Cannot use a result of 'Program.F7<T, U>(in T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F7(in t, in u); // 8
                Diagnostic(ErrorCode.ERR_EscapeCall, "F7(in t, in u)").WithArguments("Program.F7<T, U>(in T, in U)", "t").WithLocation(71, 20),
                // (71,26): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F7(in t, in u); // 8
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(71, 26),
                // (77,20): error CS8347: Cannot use a result of 'Program.F7<T, U>(in T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F7(t, u); // 9
                Diagnostic(ErrorCode.ERR_EscapeCall, "F7(t, u)").WithArguments("Program.F7<T, U>(in T, in U)", "t").WithLocation(77, 20),
                // (77,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F7(t, u); // 9
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(77, 23),
                // (83,20): error CS8347: Cannot use a result of 'Program.F8<T, U>(in T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F8(in t, out u); // 10
                Diagnostic(ErrorCode.ERR_EscapeCall, "F8(in t, out u)").WithArguments("Program.F8<T, U>(in T, out U)", "t").WithLocation(83, 20),
                // (83,26): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F8(in t, out u); // 10
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(83, 26),
                // (89,20): error CS8347: Cannot use a result of 'Program.F8<T, U>(in T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F8(t, out u); // 11
                Diagnostic(ErrorCode.ERR_EscapeCall, "F8(t, out u)").WithArguments("Program.F8<T, U>(in T, out U)", "t").WithLocation(89, 20),
                // (89,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F8(t, out u); // 11
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(89, 23),
                // (95,20): error CS8347: Cannot use a result of 'Program.F9<T, U>(out T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F9(out t, out u); // *
                Diagnostic(ErrorCode.ERR_EscapeCall, "F9(out t, out u)").WithArguments("Program.F9<T, U>(out T, out U)", "t").WithLocation(95, 20),
                // (95,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F9(out t, out u); // *
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(95, 27)
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (23,20): error CS8347: Cannot use a result of 'Program.F1<T, U>(T, ref U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return ref F1(t, ref u); // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(t, ref u)").WithArguments("Program.F1<T, U>(T, ref U)", "u").WithLocation(23, 20),
                // (23,30): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return ref F1(t, ref u); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(23, 30),
                // (29,20): error CS8347: Cannot use a result of 'Program.F2<T, U>(T, in U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return ref F2(t, in u); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t, in u)").WithArguments("Program.F2<T, U>(T, in U)", "u").WithLocation(29, 20),
                // (29,29): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return ref F2(t, in u); // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(29, 29),
                // (35,20): error CS8347: Cannot use a result of 'Program.F2<T, U>(T, in U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return ref F2(t, u); // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t, u)").WithArguments("Program.F2<T, U>(T, in U)", "u").WithLocation(35, 20),
                // (35,26): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return ref F2(t, u); // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(35, 26),
                // (47,20): error CS8347: Cannot use a result of 'Program.F4<T, U>(ref T, ref U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F4(ref t, ref u); // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "F4(ref t, ref u)").WithArguments("Program.F4<T, U>(ref T, ref U)", "t").WithLocation(47, 20),
                // (47,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F4(ref t, ref u); // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(47, 27),
                // (53,20): error CS8347: Cannot use a result of 'Program.F5<T, U>(ref T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F5(ref t, in u); // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "F5(ref t, in u)").WithArguments("Program.F5<T, U>(ref T, in U)", "t").WithLocation(53, 20),
                // (53,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F5(ref t, in u); // 5
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(53, 27),
                // (59,20): error CS8347: Cannot use a result of 'Program.F5<T, U>(ref T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F5(ref t, u); // 6
                Diagnostic(ErrorCode.ERR_EscapeCall, "F5(ref t, u)").WithArguments("Program.F5<T, U>(ref T, in U)", "t").WithLocation(59, 20),
                // (59,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F5(ref t, u); // 6
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(59, 27),
                // (65,20): error CS8347: Cannot use a result of 'Program.F6<T, U>(ref T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F6(ref t, out u); // 7
                Diagnostic(ErrorCode.ERR_EscapeCall, "F6(ref t, out u)").WithArguments("Program.F6<T, U>(ref T, out U)", "t").WithLocation(65, 20),
                // (65,27): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F6(ref t, out u); // 7
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(65, 27),
                // (71,20): error CS8347: Cannot use a result of 'Program.F7<T, U>(in T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F7(in t, in u); // 8
                Diagnostic(ErrorCode.ERR_EscapeCall, "F7(in t, in u)").WithArguments("Program.F7<T, U>(in T, in U)", "t").WithLocation(71, 20),
                // (71,26): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F7(in t, in u); // 8
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(71, 26),
                // (77,20): error CS8347: Cannot use a result of 'Program.F7<T, U>(in T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F7(t, u); // 9
                Diagnostic(ErrorCode.ERR_EscapeCall, "F7(t, u)").WithArguments("Program.F7<T, U>(in T, in U)", "t").WithLocation(77, 20),
                // (77,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F7(t, u); // 9
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(77, 23),
                // (83,20): error CS8347: Cannot use a result of 'Program.F8<T, U>(in T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F8(in t, out u); // 10
                Diagnostic(ErrorCode.ERR_EscapeCall, "F8(in t, out u)").WithArguments("Program.F8<T, U>(in T, out U)", "t").WithLocation(83, 20),
                // (83,26): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F8(in t, out u); // 10
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(83, 26),
                // (89,20): error CS8347: Cannot use a result of 'Program.F8<T, U>(in T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F8(t, out u); // 11
                Diagnostic(ErrorCode.ERR_EscapeCall, "F8(t, out u)").WithArguments("Program.F8<T, U>(in T, out U)", "t").WithLocation(89, 20),
                // (89,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F8(t, out u); // 11
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(89, 23)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Rvalue_01()
        {
            var source =
@"struct S
{
    internal T F0<T>(T t) => throw null;
    internal T F1<T>(ref T t) => throw null;
    internal T F2<T>(in T t) => throw null;
    internal T F3<T>(out T t) => throw null;
}
class Program
{
    static T F00<T>() { S s = default; T t = default; return s.F0(t); }
    static T F01<T>() { S s = default; T t = default; return s.F1(ref t); }
    static T F02<T>() { S s = default; T t = default; return s.F2(in t); }
    static T F03<T>() { S s = default; T t = default; return s.F2(t); }
    static T F04<T>() { S s = default; T t = default; return s.F3(out t); }
    static T F10<T>(ref T t) { S s = default; return s.F0(t); }
    static T F11<T>(ref T t) { S s = default; return s.F1(ref t); }
    static T F12<T>(ref T t) { S s = default; return s.F2(in t); }
    static T F13<T>(ref T t) { S s = default; return s.F2(t); }
    static T F14<T>(ref T t) { S s = default; return s.F3(out t); }
    static T F20<T>(in T t) { S s = default; return s.F0(t); }
    static T F22<T>(in T t) { S s = default; return s.F2(in t); }
    static T F23<T>(in T t) { S s = default; return s.F2(t); }
    static T F30<T>(out T t) { S s = default; t = default; return s.F0(t); }
    static T F31<T>(out T t) { S s = default; t = default; return s.F1(ref t); }
    static T F32<T>(out T t) { S s = default; t = default; return s.F2(in t); }
    static T F33<T>(out T t) { S s = default; t = default; return s.F2(t); }
    static T F34<T>(out T t) { S s = default; t = default; return s.F3(out t); }
    static T F41<T>(ref S s) { T t = default; return s.F0(t); }
    static T F42<T>(in S s) { T t = default; return s.F1(ref t); }
    static T F43<T>(out S s) { s = default; T t = default; return s.F2(in t); }
    static T F51<T>(ref S s, ref T t) { return s.F0(t); }
    static T F52<T>(in S s, ref T t) { return s.F1(ref t); }
    static T F53<T>(out S s, ref T t) { s = default; return s.F2(in t); }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Rvalue_02()
        {
            var source =
@"class C
{
    T F0<T>(T t) => throw null;
    T F1<T>(ref T t) => throw null;
    T F2<T>(in T t) => throw null;
    T F3<T>(out T t) => throw null;

    T F00<T>() { T t = default; return F0(t); }
    T F01<T>() { T t = default; return F1(ref t); }
    T F02<T>() { T t = default; return F2(in t); }
    T F03<T>() { T t = default; return F2(t); }
    T F04<T>() { T t = default; return F3(out t); }
    T F10<T>(ref T t) { return F0(t); }
    T F11<T>(ref T t) { return F1(ref t); }
    T F12<T>(ref T t) { return F2(in t); }
    T F13<T>(ref T t) { return F2(t); }
    T F14<T>(ref T t) { return F3(out t); }
    T F20<T>(in T t) { return F0(t); }
    T F22<T>(in T t) { return F2(in t); }
    T F23<T>(in T t) { return F2(t); }
    T F30<T>(out T t) { t = default; return F0(t); }
    T F31<T>(out T t) { t = default; return F1(ref t); }
    T F32<T>(out T t) { t = default; return F2(in t); }
    T F33<T>(out T t) { t = default; return F2(t); }
    T F34<T>(out T t) { t = default; return F3(out t); }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Rvalue_03()
        {
            var source =
@"class Program
{
    static T F0<T, U>(T t, U u) => throw null;
    static T F1<T, U>(T t, ref U u) => throw null;
    static T F2<T, U>(T t, in U u) => throw null;
    static T F3<T, U>(T t, out U u) => throw null;
    static T F4<T, U>(ref T t, ref U u) => throw null;
    static T F5<T, U>(ref T t, in U u) => throw null;
    static T F6<T, U>(ref T t, out U u) => throw null;
    static T F7<T, U>(in T t, in U u) => throw null;
    static T F8<T, U>(in T t, out U u) => throw null;
    static T F9<T, U>(out T t, out U u) => throw null;

    static T G0<T, U>()
    {
        T t = default;
        U u = default;
        return F0(t, u);
    }
    static T G1<T, U>()
    {
        T t = default;
        U u = default;
        return F1(t, ref u);
    }
    static T G2A<T, U>()
    {
        T t = default;
        U u = default;
        return F2(t, in u); 
    }
    static T G2B<T, U>()
    {
        T t = default;
        U u = default;
        return F2(t, u); 
    }
    static T G3<T, U>()
    {
        T t = default;
        U u = default;
        return F3(t, out u); 
    }
    static T G4<T, U>()
    {
        T t = default;
        U u = default;
        return F4(ref t, ref u); 
    }
    static T G5A<T, U>()
    {
        T t = default;
        U u = default;
        return F5(ref t, in u); 
    }
    static T G5B<T, U>()
    {
        T t = default;
        U u = default;
        return F5(ref t, u); 
    }
    static T G6<T, U>()
    {
        T t = default;
        U u = default;
        return F6(ref t, out u); 
    }
    static T G7A<T, U>()
    {
        T t = default;
        U u = default;
        return F7(in t, in u); 
    }
    static T G7B<T, U>()
    {
        T t = default;
        U u = default;
        return F7(t, u); 
    }
    static T G8A<T, U>()
    {
        T t = default;
        U u = default;
        return F8(in t, out u);
    }
    static T G8B<T, U>()
    {
        T t = default;
        U u = default;
        return F8(t, out u);
    }
    static T G9<T, U>()
    {
        T t = default;
        U u = default;
        return F9(out t, out u); 
    }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Rvalue_04()
        {
            var source =
@"ref struct R<T>
{
}
struct S
{
    internal R<T> F0<T>(T t) => throw null;
    internal R<T> F1<T>(ref T t) => throw null;
    internal R<T> F2<T>(in T t) => throw null;
    internal R<T> F3<T>(out T t) => throw null;
}
class Program
{
    static R<T> F00<T>() { S s = default; T t = default; return s.F0(t); }
    static R<T> F01<T>() { S s = default; T t = default; return s.F1(ref t); } // 1
    static R<T> F02<T>() { S s = default; T t = default; return s.F2(in t); } // 2
    static R<T> F03<T>() { S s = default; T t = default; return s.F2(t); } // 3
    static R<T> F04<T>() { S s = default; T t = default; return s.F3(out t); }
    static R<T> F10<T>(ref T t) { S s = default; return s.F0(t); }
    static R<T> F11<T>(ref T t) { S s = default; return s.F1(ref t); }
    static R<T> F12<T>(ref T t) { S s = default; return s.F2(in t); }
    static R<T> F13<T>(ref T t) { S s = default; return s.F2(t); }
    static R<T> F14<T>(ref T t) { S s = default; return s.F3(out t); }
    static R<T> F20<T>(in T t) { S s = default; return s.F0(t); }
    static R<T> F22<T>(in T t) { S s = default; return s.F2(in t); }
    static R<T> F23<T>(in T t) { S s = default; return s.F2(t); }
    static R<T> F30<T>(out T t) { S s = default; t = default; return s.F0(t); }
    static R<T> F31<T>(out T t) { S s = default; t = default; return s.F1(ref t); } // 4
    static R<T> F32<T>(out T t) { S s = default; t = default; return s.F2(in t); } // 5
    static R<T> F33<T>(out T t) { S s = default; t = default; return s.F2(t); } // 6
    static R<T> F34<T>(out T t) { S s = default; t = default; return s.F3(out t); }
    static R<T> F40<T>(ref S s) { T t = default; return s.F0(t); }
    static R<T> F41<T>(in S s) { T t = default; return s.F1(ref t); } // 7
    static R<T> F42<T>(out S s) { s = default; T t = default; return s.F2(in t); } // 8
    static R<T> F43<T>(out S s) { s = default; T t = default; return s.F2(t); } // 9
    static R<T> F44<T>(out S s) { s = default; T t = default; return s.F3(out t); }
    static R<T> F50<T>(ref S s, ref T t) { return s.F0(t); }
    static R<T> F51<T>(in S s, ref T t) { return s.F1(ref t); }
    static R<T> F52<T>(out S s, ref T t) { s = default; return s.F2(in t); }
    static R<T> F53<T>(out S s, ref T t) { s = default; return s.F2(t); }
    static R<T> F54<T>(out S s, ref T t) { s = default; return s.F3(out t); }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (14,65): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F01<T>() { S s = default; T t = default; return s.F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(14, 65),
                // (14,74): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static R<T> F01<T>() { S s = default; T t = default; return s.F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(14, 74),
                // (15,65): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F02<T>() { S s = default; T t = default; return s.F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(15, 65),
                // (15,73): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static R<T> F02<T>() { S s = default; T t = default; return s.F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(15, 73),
                // (16,65): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F03<T>() { S s = default; T t = default; return s.F2(t); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(16, 65),
                // (16,70): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static R<T> F03<T>() { S s = default; T t = default; return s.F2(t); } // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(16, 70),
                // (27,70): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F31<T>(out T t) { S s = default; t = default; return s.F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(27, 70),
                // (27,79): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static R<T> F31<T>(out T t) { S s = default; t = default; return s.F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(27, 79),
                // (28,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F32<T>(out T t) { S s = default; t = default; return s.F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(28, 70),
                // (28,78): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static R<T> F32<T>(out T t) { S s = default; t = default; return s.F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(28, 78),
                // (29,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F33<T>(out T t) { S s = default; t = default; return s.F2(t); } // 6
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(29, 70),
                // (29,75): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static R<T> F33<T>(out T t) { S s = default; t = default; return s.F2(t); } // 6
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(29, 75),
                // (32,56): error CS8347: Cannot use a result of 'S.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F41<T>(in S s) { T t = default; return s.F1(ref t); } // 7
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F1(ref t)").WithArguments("S.F1<T>(ref T)", "t").WithLocation(32, 56),
                // (32,65): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static R<T> F41<T>(in S s) { T t = default; return s.F1(ref t); } // 7
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(32, 65),
                // (33,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F42<T>(out S s) { s = default; T t = default; return s.F2(in t); } // 8
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(in t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(33, 70),
                // (33,78): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static R<T> F42<T>(out S s) { s = default; T t = default; return s.F2(in t); } // 8
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(33, 78),
                // (34,70): error CS8347: Cannot use a result of 'S.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F43<T>(out S s) { s = default; T t = default; return s.F2(t); } // 9
                Diagnostic(ErrorCode.ERR_EscapeCall, "s.F2(t)").WithArguments("S.F2<T>(in T)", "t").WithLocation(34, 70),
                // (34,75): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     static R<T> F43<T>(out S s) { s = default; T t = default; return s.F2(t); } // 9
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(34, 75)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Rvalue_05()
        {
            var source =
@"ref struct R
{
}
class C
{
    R F0<T>(T t) => throw null;
    R F1<T>(ref T t) => throw null;
    R F2<T>(in T t) => throw null;
    R F3<T>(out T t) => throw null;

    R F00<T>() { T t = default; return F0(t); }
    R F01<T>() { T t = default; return F1(ref t); } // 1
    R F02<T>() { T t = default; return F2(in t); } // 2
    R F03<T>() { T t = default; return F2(t); } // 3
    R F04<T>() { T t = default; return F3(out t); }
    R F10<T>(ref T t) { return F0(t); }
    R F11<T>(ref T t) { return F1(ref t); }
    R F12<T>(ref T t) { return F2(in t); }
    R F13<T>(ref T t) { return F2(t); }
    R F14<T>(ref T t) { return F3(out t); }
    R F20<T>(in T t) { return F0(t); }
    R F22<T>(in T t) { return F2(in t); }
    R F23<T>(in T t) { return F2(t); }
    R F30<T>(out T t) { t = default; return F0(t); }
    R F31<T>(out T t) { t = default; return F1(ref t); } // 4
    R F32<T>(out T t) { t = default; return F2(in t); } // 5
    R F33<T>(out T t) { t = default; return F2(t); } // 6
    R F34<T>(out T t) { t = default; return F3(out t); }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (12,40): error CS8347: Cannot use a result of 'C.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     R F01<T>() { T t = default; return F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(ref t)").WithArguments("C.F1<T>(ref T)", "t").WithLocation(12, 40),
                // (12,47): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     R F01<T>() { T t = default; return F1(ref t); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(12, 47),
                // (13,40): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     R F02<T>() { T t = default; return F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(in t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(13, 40),
                // (13,46): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     R F02<T>() { T t = default; return F2(in t); } // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(13, 46),
                // (14,40): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     R F03<T>() { T t = default; return F2(t); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(14, 40),
                // (14,43): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //     R F03<T>() { T t = default; return F2(t); } // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(14, 43),
                // (25,45): error CS8347: Cannot use a result of 'C.F1<T>(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     R F31<T>(out T t) { t = default; return F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(ref t)").WithArguments("C.F1<T>(ref T)", "t").WithLocation(25, 45),
                // (25,52): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     R F31<T>(out T t) { t = default; return F1(ref t); } // 4
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(25, 52),
                // (26,45): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     R F32<T>(out T t) { t = default; return F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(in t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(26, 45),
                // (26,51): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     R F32<T>(out T t) { t = default; return F2(in t); } // 5
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(26, 51),
                // (27,45): error CS8347: Cannot use a result of 'C.F2<T>(in T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     R F33<T>(out T t) { t = default; return F2(t); } // 6
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t)").WithArguments("C.F2<T>(in T)", "t").WithLocation(27, 45),
                // (27,48): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     R F33<T>(out T t) { t = default; return F2(t); } // 6
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(27, 48)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Rvalue_06()
        {
            var source =
@"ref struct R<T>
{
}
class Program
{
    static R<T> F0<T, U>(T t, U u) => throw null;
    static R<T> F1<T, U>(T t, ref U u) => throw null;
    static R<T> F2<T, U>(T t, in U u) => throw null;
    static R<T> F3<T, U>(T t, out U u) => throw null;
    static R<T> F4<T, U>(ref T t, ref U u) => throw null;
    static R<T> F5<T, U>(ref T t, in U u) => throw null;
    static R<T> F6<T, U>(ref T t, out U u) => throw null;
    static R<T> F7<T, U>(in T t, in U u) => throw null;
    static R<T> F8<T, U>(in T t, out U u) => throw null;
    static R<T> F9<T, U>(out T t, out U u) => throw null;
    static R<T> G0<T, U>()
    {
        T t = default;
        U u = default;
        return F0(t, u);
    }
    static R<T> G1<T, U>()
    {
        T t = default;
        U u = default;
        return F1(t, ref u); // 1
    }
    static R<T> G2A<T, U>()
    {
        T t = default;
        U u = default;
        return F2(t, in u); // 2
    }
    static R<T> G2B<T, U>()
    {
        T t = default;
        U u = default;
        return F2(t, u); // 3
    }
    static R<T> G3<T, U>()
    {
        T t = default;
        U u = default;
        return F3(t, out u);
    }
    static R<T> G4<T, U>()
    {
        T t = default;
        U u = default;
        return F4(ref t, ref u); // 4
    }
    static R<T> G5A<T, U>()
    {
        T t = default;
        U u = default;
        return F5(ref t, in u); // 5
    }
    static R<T> G5B<T, U>()
    {
        T t = default;
        U u = default;
        return F5(ref t, u); // 6
    }
    static R<T> G6<T, U>()
    {
        T t = default;
        U u = default;
        return F6(ref t, out u); // 7
    }
    static R<T> G7A<T, U>()
    {
        T t = default;
        U u = default;
        return F7(in t, in u); // 8
    }
    static R<T> G7B<T, U>()
    {
        T t = default;
        U u = default;
        return F7(t, u); // 9
    }
    static R<T> G8A<T, U>()
    {
        T t = default;
        U u = default;
        return F8(in t, out u); // 10
    }
    static R<T> G8B<T, U>()
    {
        T t = default;
        U u = default;
        return F8(t, out u); // 11
    }
    static R<T> G9<T, U>()
    {
        T t = default;
        U u = default;
        return F9(out t, out u);
    }
}";

            var expectedLegacyDiagnostics = new DiagnosticDescription[]
            {
            };

            var expectedUpdatedDiagnostics = new DiagnosticDescription[]
            {
                // (26,16): error CS8347: Cannot use a result of 'Program.F1<T, U>(T, ref U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return F1(t, ref u); // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(t, ref u)").WithArguments("Program.F1<T, U>(T, ref U)", "u").WithLocation(26, 16),
                // (26,26): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return F1(t, ref u); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(26, 26),
                // (32,16): error CS8347: Cannot use a result of 'Program.F2<T, U>(T, in U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return F2(t, in u); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t, in u)").WithArguments("Program.F2<T, U>(T, in U)", "u").WithLocation(32, 16),
                // (32,25): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return F2(t, in u); // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(32, 25),
                // (38,16): error CS8347: Cannot use a result of 'Program.F2<T, U>(T, in U)' in this context because it may expose variables referenced by parameter 'u' outside of their declaration scope
                //         return F2(t, u); // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(t, u)").WithArguments("Program.F2<T, U>(T, in U)", "u").WithLocation(38, 16),
                // (38,22): error CS8168: Cannot return local 'u' by reference because it is not a ref local
                //         return F2(t, u); // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "u").WithArguments("u").WithLocation(38, 22),
                // (50,16): error CS8347: Cannot use a result of 'Program.F4<T, U>(ref T, ref U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F4(ref t, ref u); // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "F4(ref t, ref u)").WithArguments("Program.F4<T, U>(ref T, ref U)", "t").WithLocation(50, 16),
                // (50,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F4(ref t, ref u); // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(50, 23),
                // (56,16): error CS8347: Cannot use a result of 'Program.F5<T, U>(ref T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F5(ref t, in u); // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "F5(ref t, in u)").WithArguments("Program.F5<T, U>(ref T, in U)", "t").WithLocation(56, 16),
                // (56,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F5(ref t, in u); // 5
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(56, 23),
                // (62,16): error CS8347: Cannot use a result of 'Program.F5<T, U>(ref T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F5(ref t, u); // 6
                Diagnostic(ErrorCode.ERR_EscapeCall, "F5(ref t, u)").WithArguments("Program.F5<T, U>(ref T, in U)", "t").WithLocation(62, 16),
                // (62,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F5(ref t, u); // 6
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(62, 23),
                // (68,16): error CS8347: Cannot use a result of 'Program.F6<T, U>(ref T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F6(ref t, out u); // 7
                Diagnostic(ErrorCode.ERR_EscapeCall, "F6(ref t, out u)").WithArguments("Program.F6<T, U>(ref T, out U)", "t").WithLocation(68, 16),
                // (68,23): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F6(ref t, out u); // 7
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(68, 23),
                // (74,16): error CS8347: Cannot use a result of 'Program.F7<T, U>(in T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F7(in t, in u); // 8
                Diagnostic(ErrorCode.ERR_EscapeCall, "F7(in t, in u)").WithArguments("Program.F7<T, U>(in T, in U)", "t").WithLocation(74, 16),
                // (74,22): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F7(in t, in u); // 8
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(74, 22),
                // (80,16): error CS8347: Cannot use a result of 'Program.F7<T, U>(in T, in U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F7(t, u); // 9
                Diagnostic(ErrorCode.ERR_EscapeCall, "F7(t, u)").WithArguments("Program.F7<T, U>(in T, in U)", "t").WithLocation(80, 16),
                // (80,19): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F7(t, u); // 9
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(80, 19),
                // (86,16): error CS8347: Cannot use a result of 'Program.F8<T, U>(in T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F8(in t, out u); // 10
                Diagnostic(ErrorCode.ERR_EscapeCall, "F8(in t, out u)").WithArguments("Program.F8<T, U>(in T, out U)", "t").WithLocation(86, 16),
                // (86,22): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F8(in t, out u); // 10
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(86, 22),
                // (92,16): error CS8347: Cannot use a result of 'Program.F8<T, U>(in T, out U)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return F8(t, out u); // 11
                Diagnostic(ErrorCode.ERR_EscapeCall, "F8(t, out u)").WithArguments("Program.F8<T, U>(in T, out U)", "t").WithLocation(92, 16),
                // (92,19): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return F8(t, out u); // 11
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(92, 19)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(comp.Assembly.RuntimeSupportsByRefFields ? expectedUpdatedDiagnostics : expectedLegacyDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(expectedUpdatedDiagnostics);
        }

        [Fact]
        public void MethodInvocation_Scoped_Lvalue()
        {
            var source =
@"class Program
{
    static ref T F0<T>(ref R<T> x, ref R<T> y) => throw null;
    static ref T F2<T>(ref R<T> x, scoped ref R<T> y) => throw null;
    static ref T F5<T>(scoped ref R<T> x, scoped ref R<T> y) => throw null;

    static ref T F00<T>(ref R<T> x) { R<T> y = default; return ref F0(ref x, ref y); } // 1
    static ref T F02<T>(ref R<T> x) { R<T> y = default; return ref F2(ref x, ref y); }
    static ref T F05<T>(ref R<T> x) { R<T> y = default; return ref F5(ref x, ref y); }

    static ref T F20<T>(scoped ref R<T> x) { R<T> y = default; return ref F0(ref x, ref y); } // 2
    static ref T F22<T>(scoped ref R<T> x) { R<T> y = default; return ref F2(ref x, ref y); } // 3 
    static ref T F25<T>(scoped ref R<T> x) { R<T> y = default; return ref F5(ref x, ref y); }
}
ref struct R<T> { }
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,68): error CS8347: Cannot use a result of 'Program.F0<T>(ref R<T>, ref R<T>)' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //     static ref T F00<T>(ref R<T> x) { R<T> y = default; return ref F0(ref x, ref y); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F0(ref x, ref y)").WithArguments("Program.F0<T>(ref R<T>, ref R<T>)", "y").WithLocation(7, 68),
                // (7,82): error CS8168: Cannot return local 'y' by reference because it is not a ref local
                //     static ref T F00<T>(ref R<T> x) { R<T> y = default; return ref F0(ref x, ref y); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "y").WithArguments("y").WithLocation(7, 82),
                // (11,75): error CS8347: Cannot use a result of 'Program.F0<T>(ref R<T>, ref R<T>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     static ref T F20<T>(scoped ref R<T> x) { R<T> y = default; return ref F0(ref x, ref y); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F0(ref x, ref y)").WithArguments("Program.F0<T>(ref R<T>, ref R<T>)", "x").WithLocation(11, 75),
                // (11,82): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     static ref T F20<T>(scoped ref R<T> x) { R<T> y = default; return ref F0(ref x, ref y); } // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(11, 82),
                // (12,75): error CS8347: Cannot use a result of 'Program.F2<T>(ref R<T>, ref R<T>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     static ref T F22<T>(scoped ref R<T> x) { R<T> y = default; return ref F2(ref x, ref y); } // 3 
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2(ref x, ref y)").WithArguments("Program.F2<T>(ref R<T>, ref R<T>)", "x").WithLocation(12, 75),
                // (12,82): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     static ref T F22<T>(scoped ref R<T> x) { R<T> y = default; return ref F2(ref x, ref y); } // 3 
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(12, 82));
        }

        [Fact]
        public void MethodInvocation_Scoped_Rvalue()
        {
            var source =
@"ref struct R
{
    public R(ref int i) { }
}
class Program
{
    static R F0(R x, R y) => throw null;
    static R F1(R x, scoped R y) => throw null;
    static R F2(scoped R x, scoped R y) => throw null;

    static R F00(R x, int i) { var y = new R(ref i); return F0(x, y); } // 1
    static R F01(R x, int i) { var y = new R(ref i); return F1(x, y); }
    static R F02(R x, int i) { var y = new R(ref i); return F2(x, y); }

    static R F10(scoped R x, int i) { var y = new R(ref i); return F0(x, y); } // 2
    static R F11(scoped R x, int i) { var y = new R(ref i); return F1(x, y); } // 3
    static R F12(scoped R x, int i) { var y = new R(ref i); return F2(x, y); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (11,61): error CS8347: Cannot use a result of 'Program.F0(R, R)' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //     static R F00(R x, int i) { var y = new R(ref i); return F0(x, y); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F0(x, y)").WithArguments("Program.F0(R, R)", "y").WithLocation(11, 61),
                // (11,67): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //     static R F00(R x, int i) { var y = new R(ref i); return F0(x, y); } // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 67),
                // (15,68): error CS8347: Cannot use a result of 'Program.F0(R, R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     static R F10(scoped R x, int i) { var y = new R(ref i); return F0(x, y); } // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "F0(x, y)").WithArguments("Program.F0(R, R)", "x").WithLocation(15, 68),
                // (15,71): error CS8352: Cannot use variable 'R' in this context because it may expose referenced variables outside of their declaration scope
                //     static R F10(scoped R x, int i) { var y = new R(ref i); return F0(x, y); } // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("R").WithLocation(15, 71),
                // (16,68): error CS8347: Cannot use a result of 'Program.F1(R, R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     static R F11(scoped R x, int i) { var y = new R(ref i); return F1(x, y); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(x, y)").WithArguments("Program.F1(R, R)", "x").WithLocation(16, 68),
                // (16,71): error CS8352: Cannot use variable 'R' in this context because it may expose referenced variables outside of their declaration scope
                //     static R F11(scoped R x, int i) { var y = new R(ref i); return F1(x, y); } // 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("R").WithLocation(16, 71));
        }

        [Fact]
        public void MethodArgumentsMustMatch_01()
        {
            var source =
@"ref struct R
{
    public void F0(ref R r) => throw null;
    public void F2(scoped ref R r) => throw null;
}
class Program
{
    static void F00(ref R x, ref R y) { x.F0(ref y); }
    static void F02(ref R x, ref R y) { x.F2(ref y); }

    static void F20(ref R x, scoped ref R y) { x.F0(ref y); }
    static void F22(ref R x, scoped ref R y) { x.F2(ref y); }

    static void F60(scoped ref R x, ref R y) { x.F0(ref y); }
    static void F62(scoped ref R x, ref R y) { x.F2(ref y); }

    static void F80(scoped ref R x, scoped ref R y) { x.F0(ref y); }
    static void F82(scoped ref R x, scoped ref R y) { x.F2(ref y); }
}";
            var comp = CreateCompilation(source);
            // Should we also report ErrorCode.ERR_CallArgMixing for 3, 4, 5, 6? See the call to
            // CheckValEscape() for the receiver at the end of CheckInvocationArgMixing().
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MethodArgumentsMustMatch_02()
        {
            var source =
@"ref struct R
{
}
class Program
{
    static void F0(ref R a, ref R b) => throw null;
    static void F2(ref R a, scoped ref R b) => throw null;
    static void F5(scoped ref R a, scoped ref R b) => throw null;

    static void F00(ref R x, ref R y) { F0(ref x, ref y); }
    static void F02(ref R x, ref R y) { F2(ref x, ref y); }
    static void F05(ref R x, ref R y) { F5(ref x, ref y); }

    static void F20(ref R x, scoped ref R y) { F0(ref x, ref y); }
    static void F22(ref R x, scoped ref R y) { F2(ref x, ref y); }
    static void F25(ref R x, scoped ref R y) { F5(ref x, ref y); }

    static void F50(scoped ref R x, scoped ref R y) { F0(ref x, ref y); }
    static void F52(scoped ref R x, scoped ref R y) { F2(ref x, ref y); }
    static void F55(scoped ref R x, scoped ref R y) { F5(ref x, ref y); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MethodArgumentsMustMatch_03()
        {
            var source =
@"ref struct R<T>
{
    public R(ref T t) { }
}
class Program
{
    static void F0<T>(ref R<T> a, ref R<T> b) => throw null;
    static void F2<T>(ref R<T> a, scoped ref R<T> b) => throw null;
    static void F5<T>(scoped ref R<T> a, scoped ref R<T> b) => throw null;

    static void F<T>(ref R<T> x)
    {
        T t = default;
        R<T> y = new R<T>(ref t);

        F0(ref x, ref x);
        F2(ref x, ref x);
        F5(ref x, ref x);

        F0(ref x, ref y); // 1
        F2(ref x, ref y); // 2
        F5(ref x, ref y); // 3

        F0(ref y, ref x); // 4
        F2(ref y, ref x); // 5
        F5(ref y, ref x); // 6

        F0(ref y, ref y);
        F2(ref y, ref y);
        F5(ref y, ref y);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (20,9): error CS8350: This combination of arguments to 'Program.F0<T>(ref R<T>, ref R<T>)' is disallowed because it may expose variables referenced by parameter 'b' outside of their declaration scope
                //         F0(ref x, ref y); // 1
                Diagnostic(ErrorCode.ERR_CallArgMixing, "F0(ref x, ref y)").WithArguments("Program.F0<T>(ref R<T>, ref R<T>)", "b").WithLocation(20, 9),
                // (20,23): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         F0(ref x, ref y); // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(20, 23),
                // (21,9): error CS8350: This combination of arguments to 'Program.F2<T>(ref R<T>, ref R<T>)' is disallowed because it may expose variables referenced by parameter 'b' outside of their declaration scope
                //         F2(ref x, ref y); // 2
                Diagnostic(ErrorCode.ERR_CallArgMixing, "F2(ref x, ref y)").WithArguments("Program.F2<T>(ref R<T>, ref R<T>)", "b").WithLocation(21, 9),
                // (21,23): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         F2(ref x, ref y); // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(21, 23),
                // (22,9): error CS8350: This combination of arguments to 'Program.F5<T>(ref R<T>, ref R<T>)' is disallowed because it may expose variables referenced by parameter 'b' outside of their declaration scope
                //         F5(ref x, ref y); // 3
                Diagnostic(ErrorCode.ERR_CallArgMixing, "F5(ref x, ref y)").WithArguments("Program.F5<T>(ref R<T>, ref R<T>)", "b").WithLocation(22, 9),
                // (22,23): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         F5(ref x, ref y); // 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(22, 23),
                // (24,9): error CS8350: This combination of arguments to 'Program.F0<T>(ref R<T>, ref R<T>)' is disallowed because it may expose variables referenced by parameter 'a' outside of their declaration scope
                //         F0(ref y, ref x); // 4
                Diagnostic(ErrorCode.ERR_CallArgMixing, "F0(ref y, ref x)").WithArguments("Program.F0<T>(ref R<T>, ref R<T>)", "a").WithLocation(24, 9),
                // (24,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         F0(ref y, ref x); // 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(24, 16),
                // (25,9): error CS8350: This combination of arguments to 'Program.F2<T>(ref R<T>, ref R<T>)' is disallowed because it may expose variables referenced by parameter 'a' outside of their declaration scope
                //         F2(ref y, ref x); // 5
                Diagnostic(ErrorCode.ERR_CallArgMixing, "F2(ref y, ref x)").WithArguments("Program.F2<T>(ref R<T>, ref R<T>)", "a").WithLocation(25, 9),
                // (25,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         F2(ref y, ref x); // 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(25, 16),
                // (26,9): error CS8350: This combination of arguments to 'Program.F5<T>(ref R<T>, ref R<T>)' is disallowed because it may expose variables referenced by parameter 'a' outside of their declaration scope
                //         F5(ref y, ref x); // 6
                Diagnostic(ErrorCode.ERR_CallArgMixing, "F5(ref y, ref x)").WithArguments("Program.F5<T>(ref R<T>, ref R<T>)", "a").WithLocation(26, 9),
                // (26,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         F5(ref y, ref x); // 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(26, 16));
        }

        [Fact]
        public void MethodArgumentsMustMatch_04()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F0(ref R x, in R y) => throw null;
    static void F2(ref R x, scoped in R y) => throw null;

    static void F00(ref R x, in R y) { F0(ref x, in y); }
    static void F02(ref R x, in R y) { F2(ref x, in y); }
    static void F20(ref R x, scoped in R y) { F0(ref x, in y); }
    static void F22(ref R x, scoped in R y) { F2(ref x, in y); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MethodArgumentsMustMatch_05()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F0(ref R x, out R y) => throw null;
    static void F2(ref R x, scoped out R y) => throw null;

    static void F00(ref R x, out R y) { F0(ref x, out y); }
    static void F02(ref R x, out R y) { F2(ref x, out y); }
    static void F20(ref R x, scoped out R y) { F0(ref x, out y); }
    static void F22(ref R x, scoped out R y) { F2(ref x, out y); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MethodArgumentsMustMatch_06()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F0(__arglist) { }
    static void F1(ref R a, __arglist) { }

    static void F00(ref R x, ref R y) { F0(__arglist(ref x, ref y)); }
    static void F01(ref R x, ref R y) { F1(ref x, __arglist(ref y)); }
    static void F20(ref R x, scoped ref R y) { F0(__arglist(ref x, ref y)); }
    static void F21(ref R x, scoped ref R y) { F1(ref x, __arglist(ref y)); }
    static void F50(scoped ref R x, scoped ref R y) { F0(__arglist(ref x, ref y)); }
    static void F51(scoped ref R x, scoped ref R y) { F1(ref x, __arglist(ref y)); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void NestedFieldAccessor()
        {
            var source =
@"ref struct R<T>
{
    private ref T _t;
    public R(ref T t) { _t = t; }
    public ref T F0() => ref _t;
    ref T F1() => ref F0();
}
class Program
{
    static ref T F2<T>(ref R<T> r) => ref r.F0();
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Constructors()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    public S(ref T t)
    {
        F = ref t;
    }
    S(object unused, T t0)
    {
        this = default;
        this = new S<T>();
        this = new S<T>(ref t0);
        this = new S<T> { F = t0 };
    }
    void M1(T t1)
    {
        this = default;
        this = new S<T>();
        this = new S<T>(ref t1);
        this = new S<T> { F = t1 };
    }
    static void M2(T t2)
    {
        S<T> s2;
        s2 = default;
        s2 = new S<T>();
        s2 = new S<T>(ref t2);
        s2 = new S<T> { F = t2 };
    }
    static void M3(ref T t3)
    {
        S<T> s3;
        s3 = new S<T>(ref t3);
        s3 = new S<T> { F = t3 };
    }
    static void M4(T t4)
    {
        S<T> s;
        s = new S<T>();
        s = new S<T>(ref t4);
        s = new S<T> { F = t4 };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,16): error CS8347: Cannot use a result of 'S<T>.S(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         this = new S<T>(ref t0);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S<T>(ref t0)").WithArguments("S<T>.S(ref T)", "t").WithLocation(12, 16),
                // (12,29): error CS8166: Cannot return a parameter by reference 't0' because it is not a ref parameter
                //         this = new S<T>(ref t0);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t0").WithArguments("t0").WithLocation(12, 29),
                // (19,16): error CS8347: Cannot use a result of 'S<T>.S(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         this = new S<T>(ref t1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S<T>(ref t1)").WithArguments("S<T>.S(ref T)", "t").WithLocation(19, 16),
                // (19,29): error CS8166: Cannot return a parameter by reference 't1' because it is not a ref parameter
                //         this = new S<T>(ref t1);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t1").WithArguments("t1").WithLocation(19, 29),
                // (27,14): error CS8347: Cannot use a result of 'S<T>.S(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         s2 = new S<T>(ref t2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S<T>(ref t2)").WithArguments("S<T>.S(ref T)", "t").WithLocation(27, 14),
                // (27,27): error CS8166: Cannot return a parameter by reference 't2' because it is not a ref parameter
                //         s2 = new S<T>(ref t2);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t2").WithArguments("t2").WithLocation(27, 27),
                // (40,13): error CS8347: Cannot use a result of 'S<T>.S(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         s = new S<T>(ref t4);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S<T>(ref t4)").WithArguments("S<T>.S(ref T)", "t").WithLocation(40, 13),
                // (40,26): error CS8166: Cannot return a parameter by reference 't4' because it is not a ref parameter
                //         s = new S<T>(ref t4);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t4").WithArguments("t4").WithLocation(40, 26));
        }

        [Fact]
        public void DefiniteAssignment_01()
        {
            var source =
@"ref struct S1<T>
{
    public ref T F;
}
ref struct S2<T>
{
    public ref T F;
    public S2(ref T t) { }
}
ref struct S3<T>
{
    public ref T F;
    public S3(ref T t) : this() { }
}
ref struct S4<T>
{
    public ref T F;
    public S4(ref T t)
    {
        this = default;
    }
}
class Program
{
    static void F<T>(ref T t)
    {
        new S1<T>().F = ref t;
        new S2<T>().F = ref t;
        new S3<T>().F = ref t;
        new S4<T>().F = ref t;
        new S1<T>().F = t;
        new S2<T>().F = t;
        new S3<T>().F = t;
        new S4<T>().F = t;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (27,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new S1<T>().F = ref t;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new S1<T>().F").WithLocation(27, 9),
                // (28,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new S2<T>().F = ref t;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new S2<T>().F").WithLocation(28, 9),
                // (29,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new S3<T>().F = ref t;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new S3<T>().F").WithLocation(29, 9),
                // (30,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new S4<T>().F = ref t;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new S4<T>().F").WithLocation(30, 9));
        }

        [Fact]
        public void DefiniteAssignment_02()
        {
            // Should we report a warning when assigning a value rather than a ref in the
            // constructor, because a NullReferenceException will be thrown at runtime?
            var source =
@"ref struct S<T>
{
    public ref T F;
    public S(ref T t)
    {
        F = t;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void AssignLocal()
        {
            var source =
@"ref struct S<T>
{
    public ref T F1;
    public ref readonly T F2;
    public readonly ref T F3;
    public readonly ref readonly T F4;
    public S()
    {
        T t = default;
        F1 = ref t;
        F2 = ref t;
        F3 = ref t;
        F4 = ref t;
    }
}
class Program
{
    static void M<T>(ref S<T> x)
    {
        T t = default;
        x.F1 = ref t;
        x.F2 = ref t;
        x.F3 = ref t;
        x.F4 = ref t;
        S<T> y = new S<T>();
        y.F1 = ref t;
        y.F2 = ref t;
        y.F3 = ref t;
        y.F4 = ref t;
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (10,9): error CS8374: Cannot ref-assign 't' to 'F1' because 't' has a narrower escape scope than 'F1'.
                //         F1 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F1 = ref t").WithArguments("F1", "t").WithLocation(10, 9),
                // (11,9): error CS8374: Cannot ref-assign 't' to 'F2' because 't' has a narrower escape scope than 'F2'.
                //         F2 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F2 = ref t").WithArguments("F2", "t").WithLocation(11, 9),
                // (12,9): error CS8374: Cannot ref-assign 't' to 'F3' because 't' has a narrower escape scope than 'F3'.
                //         F3 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F3 = ref t").WithArguments("F3", "t").WithLocation(12, 9),
                // (13,9): error CS8374: Cannot ref-assign 't' to 'F4' because 't' has a narrower escape scope than 'F4'.
                //         F4 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F4 = ref t").WithArguments("F4", "t").WithLocation(13, 9),
                // (21,9): error CS8374: Cannot ref-assign 't' to 'F1' because 't' has a narrower escape scope than 'F1'.
                //         x.F1 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "x.F1 = ref t").WithArguments("F1", "t").WithLocation(21, 9),
                // (22,9): error CS8374: Cannot ref-assign 't' to 'F2' because 't' has a narrower escape scope than 'F2'.
                //         x.F2 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "x.F2 = ref t").WithArguments("F2", "t").WithLocation(22, 9),
                // (23,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         x.F3 = ref t;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "x.F3").WithLocation(23, 9),
                // (24,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         x.F4 = ref t;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "x.F4").WithLocation(24, 9),
                // (26,9): error CS8374: Cannot ref-assign 't' to 'F1' because 't' has a narrower escape scope than 'F1'.
                //         y.F1 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "y.F1 = ref t").WithArguments("F1", "t").WithLocation(26, 9),
                // (27,9): error CS8374: Cannot ref-assign 't' to 'F2' because 't' has a narrower escape scope than 'F2'.
                //         y.F2 = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "y.F2 = ref t").WithArguments("F2", "t").WithLocation(27, 9),
                // (28,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         y.F3 = ref t;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "y.F3").WithLocation(28, 9),
                // (29,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         y.F4 = ref t;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "y.F4").WithLocation(29, 9));
        }

        [Fact]
        public void AssignValueTo_InstanceMethod_RefField()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = tValue;
        F = tRef;
        F = tOut;
        F = tIn;
    }
    object P
    {
        init
        {
            F = GetValue();
            F = GetRef();
            F = GetRefReadonly();
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void AssignValueTo_InstanceMethod_RefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = tValue; // 1
        F = tRef; // 2
        F = tOut; // 3
        F = tIn; // 4
    }
    object P
    {
        init
        {
            F = GetValue(); // 5
            F = GetRef(); // 6
            F = GetRefReadonly(); // 7
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tValue; // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(7, 9),
                // (8,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tRef; // 2
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(8, 9),
                // (9,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tOut; // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(9, 9),
                // (10,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tIn; // 4
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(10, 9),
                // (16,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetValue(); // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(16, 13),
                // (17,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRef(); // 6
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(17, 13),
                // (18,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRefReadonly(); // 7
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(18, 13));
        }

        [Fact]
        public void AssignValueTo_InstanceMethod_ReadonlyRefField()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = tValue;
        F = tRef;
        F = tOut;
        F = tIn;
    }
    object P
    {
        init
        {
            F = GetValue();
            F = GetRef();
            F = GetRefReadonly();
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void AssignValueTo_InstanceMethod_ReadonlyRefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = tValue; // 1
        F = tRef; // 2
        F = tOut; // 3
        F = tIn; // 4
    }
    object P
    {
        init
        {
            F = GetValue(); // 5
            F = GetRef(); // 6
            F = GetRefReadonly(); // 7
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tValue; // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(7, 9),
                // (8,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tRef; // 2
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(8, 9),
                // (9,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tOut; // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(9, 9),
                // (10,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tIn; // 4
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(10, 9),
                // (16,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetValue(); // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(16, 13),
                // (17,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRef(); // 6
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(17, 13),
                // (18,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRefReadonly(); // 7
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(18, 13));
        }

        [Fact]
        public void AssignRefTo_InstanceMethod_RefField()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = ref tValue; // 1
        F = ref tRef;
        F = ref tOut; // 2
        F = ref tIn; // 3
    }
    T P
    {
        init
        {
            F = ref value; // 4
            F = ref GetRef();
            F = ref GetRefReadonly(); // 5
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //         F = ref tValue; // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tValue").WithArguments("F", "tValue").WithLocation(7, 9),
                // (9,9): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //         F = ref tOut; // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tOut").WithArguments("F", "tOut").WithLocation(9, 9),
                // (10,17): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //         F = ref tIn; // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(10, 17),
                // (16,13): error CS8374: Cannot ref-assign 'value' to 'F' because 'value' has a narrower escape scope than 'F'.
                //             F = ref value; // 4
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref value").WithArguments("F", "value").WithLocation(16, 13),
                // (18,21): error CS8331: Cannot assign to method 'S<T>.GetRefReadonly()' because it is a readonly variable
                //             F = ref GetRefReadonly(); // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "GetRefReadonly()").WithArguments("method", "S<T>.GetRefReadonly()").WithLocation(18, 21));
        }

        [Fact]
        public void AssignRefTo_InstanceMethod_RefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = ref tValue; // 1
        F = ref tRef;
        F = ref tOut; // 2
        F = ref tIn;
    }
    T P
    {
        init
        {
            F = ref value; // 3
            F = ref GetRef();
            F = ref GetRefReadonly();
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //         F = ref tValue; // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tValue").WithArguments("F", "tValue").WithLocation(7, 9),
                // (9,9): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //         F = ref tOut; // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tOut").WithArguments("F", "tOut").WithLocation(9, 9),
                // (16,13): error CS8374: Cannot ref-assign 'value' to 'F' because 'value' has a narrower escape scope than 'F'.
                //             F = ref value; // 3
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref value").WithArguments("F", "value").WithLocation(16, 13));
        }

        [Fact]
        public void AssignRefTo_InstanceMethod_ReadonlyRefField()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = ref tValue; // 1
        F = ref tRef;
        F = ref tOut; // 2
        F = ref tIn; // 3
    }
    T P
    {
        init
        {
            F = ref value; // 4
            F = ref GetRef();
            F = ref GetRefReadonly(); // 5
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //         F = ref tValue; // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tValue").WithArguments("F", "tValue").WithLocation(7, 9),
                // (9,9): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //         F = ref tOut; // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tOut").WithArguments("F", "tOut").WithLocation(9, 9),
                // (10,17): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //         F = ref tIn; // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(10, 17),
                // (16,13): error CS8374: Cannot ref-assign 'value' to 'F' because 'value' has a narrower escape scope than 'F'.
                //             F = ref value; // 4
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref value").WithArguments("F", "value").WithLocation(16, 13),
                // (18,21): error CS8331: Cannot assign to method 'S<T>.GetRefReadonly()' because it is a readonly variable
                //             F = ref GetRefReadonly(); // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "GetRefReadonly()").WithArguments("method", "S<T>.GetRefReadonly()").WithLocation(18, 21));
        }

        [Fact]
        public void AssignRefTo_InstanceMethod_ReadonlyRefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
    public S(T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        F = ref tValue; // 1
        F = ref tRef;
        F = ref tOut; // 2
        F = ref tIn;
    }
    T P
    {
        init
        {
            F = ref value; // 3
            F = ref GetRef();
            F = ref GetRefReadonly();
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //         F = ref tValue; // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tValue").WithArguments("F", "tValue").WithLocation(7, 9),
                // (9,9): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //         F = ref tOut; // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref tOut").WithArguments("F", "tOut").WithLocation(9, 9),
                // (16,13): error CS8374: Cannot ref-assign 'value' to 'F' because 'value' has a narrower escape scope than 'F'.
                //             F = ref value; // 3
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "F = ref value").WithArguments("F", "value").WithLocation(16, 13));
        }

        [Fact]
        public void AssignValueTo_RefField()
        {
            var source =
@"using System;

ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = tValue; }
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = tOut; }
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = tIn; }

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = tValue; }
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = tOut; }
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = tIn; }

    static void AssignValueToOut<T>(out S<T> sOut, S<T> sInit, T tValue) { sOut = sInit; sOut.F = tValue; }
    static void AssignRefToOut<T>(out S<T> sOut, S<T> sInit, ref T tRef) { sOut = sInit; sOut.F = tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, S<T> sInit, out T tOut) { sOut = sInit; tOut = default; sOut.F = tOut; }
    static void AssignInToOut<T>(out S<T> sOut, S<T> sInit, in T tIn)    { sOut = sInit; sOut.F = tIn; }

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; }
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; }
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; }
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; }

    static void Main()
    {
        int x, y;
        scoped S<int> s;

        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignValueToValue(s, y);
        Console.WriteLine(s.F);
        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignRefToValue(s, ref y);
        Console.WriteLine(s.F);
        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignOutToValue(s, out y);
        Console.WriteLine(s.F);
        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignInToValue(s, y);
        Console.WriteLine(s.F);

        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignValueToRef(ref s, y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignRefToRef(ref s, ref y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignOutToRef(ref s, out y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignInToRef(ref s, y);
        Console.WriteLine(s.F);

        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignValueToOut(out s, new S<int>(ref x), y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignRefToOut(out s, new S<int>(ref x), ref y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignOutToOut(out s, new S<int>(ref x), out y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignInToOut(out s, new S<int>(ref x), y);
        Console.WriteLine(s.F);

        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignValueToIn(s, y);
        Console.WriteLine(s.F);
        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignRefToIn(s, ref y);
        Console.WriteLine(s.F);
        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignOutToIn(s, out y);
        Console.WriteLine(s.F);
        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignInToIn(s, y);
        Console.WriteLine(s.F);
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"2
2
0
2
4
4
0
4
6
6
0
6
8
8
0
8"));
            verifier.VerifyILMultiple(
                "Program.AssignValueToValue<T>",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  stobj      ""T""
  IL_000c:  ret
}",
                "Program.AssignRefToValue<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignOutToValue<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.1
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignInToValue<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignValueToRef<T>",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  stobj      ""T""
  IL_000c:  ret
}",
                "Program.AssignRefToRef<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignOutToRef<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.1
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignInToRef<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignValueToOut<T>",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.2
  IL_000e:  stobj      ""T""
  IL_0013:  ret
}",
                "Program.AssignRefToOut<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.2
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignOutToOut<T>",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.2
  IL_0008:  initobj    ""T""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""ref T S<T>.F""
  IL_0014:  ldarg.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stobj      ""T""
  IL_001f:  ret
}",
                "Program.AssignInToOut<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.2
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignValueToIn<T>",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  stobj      ""T""
  IL_000c:  ret
}",
                "Program.AssignRefToIn<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignOutToIn<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.1
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignInToIn<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}");
        }

        [Fact]
        public void AssignValueTo_RefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = tValue; } // 1
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = tRef; } // 2
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = tOut; } // 3
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = tIn; } // 4

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = tValue; } // 5
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = tRef; } // 6
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = tOut; } // 7
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = tIn; } // 8

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = tValue; } // 9
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = tRef; } // 10
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = tOut; } // 11
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = tIn; } // 12

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; } // 13
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; } // 14
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; } // 15
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; } // 16
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,59): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = tValue; } // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(9, 59),
                // (10,59): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = tRef; } // 2
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(10, 59),
                // (11,75): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = tOut; } // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(11, 75),
                // (12,59): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = tIn; } // 4
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(12, 59),
                // (14,64): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = tValue; } // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(14, 64),
                // (15,64): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = tRef; } // 6
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(15, 64),
                // (16,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = tOut; } // 7
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(16, 80),
                // (17,64): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = tIn; } // 8
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(17, 64),
                // (19,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = tValue; } // 9
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(19, 80),
                // (20,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = tRef; } // 10
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(20, 80),
                // (21,96): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = tOut; } // 11
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(21, 96),
                // (22,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = tIn; } // 12
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(22, 80),
                // (24,61): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; } // 13
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(24, 61),
                // (25,61): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; } // 14
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(25, 61),
                // (26,77): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; } // 15
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(26, 77),
                // (27,61): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; } // 16
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(27, 61));
        }

        [Fact]
        public void AssignValueTo_ReadonlyRefField()
        {
            var source =
@"using System;

ref struct S<T>
{
    public readonly ref T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = tValue; }
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = tOut; }
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = tIn; }

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = tValue; }
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = tOut; }
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = tIn; }

    static void AssignValueToOut<T>(out S<T> sOut, S<T> sInit, T tValue) { sOut = sInit; sOut.F = tValue; }
    static void AssignRefToOut<T>(out S<T> sOut, S<T> sInit, ref T tRef) { sOut = sInit; sOut.F = tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, S<T> sInit, out T tOut) { sOut = sInit; tOut = default; sOut.F = tOut; }
    static void AssignInToOut<T>(out S<T> sOut, S<T> sInit, in T tIn)    { sOut = sInit; sOut.F = tIn; }

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; }
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; }
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; }
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; }

    static void Main()
    {
        int x, y;
        scoped S<int> s;

        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignValueToValue(s, y);
        Console.WriteLine(s.F);
        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignRefToValue(s, ref y);
        Console.WriteLine(s.F);
        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignOutToValue(s, out y);
        Console.WriteLine(s.F);
        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignInToValue(s, y);
        Console.WriteLine(s.F);

        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignValueToRef(ref s, y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignRefToRef(ref s, ref y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignOutToRef(ref s, out y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignInToRef(ref s, y);
        Console.WriteLine(s.F);

        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignValueToOut(out s, new S<int>(ref x), y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignRefToOut(out s, new S<int>(ref x), ref y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignOutToOut(out s, new S<int>(ref x), out y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignInToOut(out s, new S<int>(ref x), y);
        Console.WriteLine(s.F);

        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignValueToIn(s, y);
        Console.WriteLine(s.F);
        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignRefToIn(s, ref y);
        Console.WriteLine(s.F);
        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignOutToIn(s, out y);
        Console.WriteLine(s.F);
        x = 7; y = 8;
        s = new S<int>(ref x);
        AssignInToIn(s, y);
        Console.WriteLine(s.F);
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"2
2
0
2
4
4
0
4
6
6
0
6
8
8
0
8"));
            verifier.VerifyILMultiple(
                "Program.AssignValueToValue<T>",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  stobj      ""T""
  IL_000c:  ret
}",
                "Program.AssignRefToValue<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignOutToValue<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.1
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignInToValue<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignValueToRef<T>",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  stobj      ""T""
  IL_000c:  ret
}",
                "Program.AssignRefToRef<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignOutToRef<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.1
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignInToRef<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignValueToOut<T>",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.2
  IL_000e:  stobj      ""T""
  IL_0013:  ret
}",
                "Program.AssignRefToOut<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.2
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignOutToOut<T>",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.2
  IL_0008:  initobj    ""T""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""ref T S<T>.F""
  IL_0014:  ldarg.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stobj      ""T""
  IL_001f:  ret
}",
                "Program.AssignInToOut<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.2
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignValueToIn<T>",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  stobj      ""T""
  IL_000c:  ret
}",
                "Program.AssignRefToIn<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}",
                "Program.AssignOutToIn<T>",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""ref T S<T>.F""
  IL_000d:  ldarg.1
  IL_000e:  ldobj      ""T""
  IL_0013:  stobj      ""T""
  IL_0018:  ret
}",
                "Program.AssignInToIn<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""T""
  IL_000c:  stobj      ""T""
  IL_0011:  ret
}");
        }

        [Fact]
        public void AssignValueTo_ReadonlyRefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = tValue; } // 1
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = tRef; } // 2
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = tOut; } // 3
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = tIn; } // 4

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = tValue; } // 5
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = tRef; } // 6
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = tOut; } // 7
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = tIn; } // 8

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = tValue; } // 9
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = tRef; } // 10
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = tOut; } // 11
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = tIn; } // 12

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; } // 13
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; } // 14
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; } // 15
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; } // 16
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,59): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = tValue; } // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(9, 59),
                // (10,59): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = tRef; } // 2
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(10, 59),
                // (11,75): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = tOut; } // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(11, 75),
                // (12,59): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = tIn; } // 4
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(12, 59),
                // (14,64): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = tValue; } // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(14, 64),
                // (15,64): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = tRef; } // 6
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(15, 64),
                // (16,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = tOut; } // 7
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(16, 80),
                // (17,64): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = tIn; } // 8
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sRef.F").WithArguments("field", "S<T>.F").WithLocation(17, 64),
                // (19,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = tValue; } // 9
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(19, 80),
                // (20,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = tRef; } // 10
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(20, 80),
                // (21,96): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = tOut; } // 11
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(21, 96),
                // (22,80): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = tIn; } // 12
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sOut.F").WithArguments("field", "S<T>.F").WithLocation(22, 80),
                // (24,61): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; } // 13
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(24, 61),
                // (25,61): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; } // 14
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(25, 61),
                // (26,77): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; } // 15
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(26, 77),
                // (27,61): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; } // 16
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "sIn.F").WithArguments("field", "S<T>.F").WithLocation(27, 61));
        }

        [Fact]
        public void AssignRefTo_RefField()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 2
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 3

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 4
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 5
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 6

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 7
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 8
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 9

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 10
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 11
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 12
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 13
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,59): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s.F = ref tValue").WithArguments("F", "tValue").WithLocation(9, 59),
                // (11,75): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //     static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s.F = ref tOut").WithArguments("F", "tOut").WithLocation(11, 75),
                // (12,69): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //     static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(12, 69),
                // (14,64): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 4
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sRef.F = ref tValue").WithArguments("F", "tValue").WithLocation(14, 64),
                // (16,80): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //     static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 5
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sRef.F = ref tOut").WithArguments("F", "tOut").WithLocation(16, 80),
                // (17,77): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //     static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 6
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(17, 77),
                // (19,80): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 7
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sOut.F = ref tValue").WithArguments("F", "tValue").WithLocation(19, 80),
                // (21,96): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //     static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 8
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sOut.F = ref tOut").WithArguments("F", "tOut").WithLocation(21, 96),
                // (22,93): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //     static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 9
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(22, 93),
                // (24,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 10
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(24, 61),
                // (25,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 11
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(25, 61),
                // (26,77): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 12
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(26, 77),
                // (27,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 13
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(27, 61));

            // Valid cases from above.
            source =
@"using System;

ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }

    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }

    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }

    static void Main()
    {
        int x, y;
        scoped S<int> s;

        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignRefToValue(s, ref y);
        Console.WriteLine(s.F);

        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignRefToRef(ref s, ref y);
        Console.WriteLine(s.F);

        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignRefToOut(out s, ref y);
        Console.WriteLine(s.F);
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"1
4
6"));
            verifier.VerifyILMultiple(
                "Program.AssignRefToValue<T>",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  stfld      ""ref T S<T>.F""
  IL_0008:  ret
}",
                "Program.AssignRefToRef<T>",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""ref T S<T>.F""
  IL_0007:  ret
}",
                "Program.AssignRefToOut<T>",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""ref T S<T>.F""
  IL_000e:  ret
}");
        }

        [Fact]
        public void AssignRefTo_RefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 2
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; }

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 3
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 4
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; }

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 5
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 6
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; }

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 7
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 8
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 9
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 10
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,59): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s.F = ref tValue").WithArguments("F", "tValue").WithLocation(9, 59),
                // (11,75): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //     static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s.F = ref tOut").WithArguments("F", "tOut").WithLocation(11, 75),
                // (14,64): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 3
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sRef.F = ref tValue").WithArguments("F", "tValue").WithLocation(14, 64),
                // (16,80): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //     static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 4
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sRef.F = ref tOut").WithArguments("F", "tOut").WithLocation(16, 80),
                // (19,80): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 5
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sOut.F = ref tValue").WithArguments("F", "tValue").WithLocation(19, 80),
                // (21,96): error CS8374: Cannot ref-assign 'tOut' to 'F' because 'tOut' has a narrower escape scope than 'F'.
                //     static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 6
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sOut.F = ref tOut").WithArguments("F", "tOut").WithLocation(21, 96),
                // (24,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 7
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(24, 61),
                // (25,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 8
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(25, 61),
                // (26,77): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 9
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(26, 77),
                // (27,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 10
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(27, 61));

            // Valid cases from above.
            source =
@"using System;

ref struct S<T>
{
    public ref readonly T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; }

    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; }

    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; }

    static void Main()
    {
        int x, y;
        scoped S<int> s;

        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignRefToValue(s, ref y);
        Console.WriteLine(s.F);
        x = 1; y = 2;
        s = new S<int>(ref x);
        AssignInToValue(s, y);
        Console.WriteLine(s.F);

        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignRefToRef(ref s, ref y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignInToRef(ref s, y);
        Console.WriteLine(s.F);

        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignRefToOut(out s, ref y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignInToOut(out s, y);
        Console.WriteLine(s.F);
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"1
1
4
4
6
6"));
            verifier.VerifyILMultiple(
                "Program.AssignRefToValue<T>",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  stfld      ""ref readonly T S<T>.F""
  IL_0008:  ret
}",
                "Program.AssignInToValue<T>",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  stfld      ""ref readonly T S<T>.F""
  IL_0008:  ret
}",
                "Program.AssignRefToRef<T>",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""ref readonly T S<T>.F""
  IL_0007:  ret
}",
                "Program.AssignInToRef<T>",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""ref readonly T S<T>.F""
  IL_0007:  ret
}",
                "Program.AssignRefToOut<T>",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""ref readonly T S<T>.F""
  IL_000e:  ret
}",
                "Program.AssignInToOut<T>",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S<T>""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""ref readonly T S<T>.F""
  IL_000e:  ret
}");
        }

        [Fact]
        public void AssignRefTo_ReadonlyRefField()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; } // 2
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 3
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 4

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 5
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; } // 6
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 7
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 8

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 9
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; } // 10
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 11
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 12

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 13
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 14
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 15
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 16
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,59): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(9, 59),
                // (10,59): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; } // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(10, 59),
                // (11,75): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(11, 75),
                // (12,59): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(12, 59),
                // (14,64): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(14, 64),
                // (15,64): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; } // 6
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(15, 64),
                // (16,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 7
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(16, 80),
                // (17,64): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 8
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(17, 64),
                // (19,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 9
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(19, 80),
                // (20,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; } // 10
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(20, 80),
                // (21,96): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 11
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(21, 96),
                // (22,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 12
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(22, 80),
                // (24,61): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 13
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(24, 61),
                // (25,61): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 14
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(25, 61),
                // (26,77): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 15
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(26, 77),
                // (27,61): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 16
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(27, 61));
        }

        [Fact]
        public void AssignRefTo_ReadonlyRefReadonlyField()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
    public S(ref T t) { F = ref t; }
}

class Program
{
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; } // 2
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 3
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 4

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 5
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; } // 6
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 7
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 8

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 9
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; } // 10
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 11
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 12

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 13
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 14
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 15
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 16
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,59): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; } // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(9, 59),
                // (10,59): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; } // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(10, 59),
                // (11,75): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; } // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(11, 75),
                // (12,59): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(12, 59),
                // (14,64): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 5
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(14, 64),
                // (15,64): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; } // 6
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(15, 64),
                // (16,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; } // 7
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(16, 80),
                // (17,64): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 8
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sRef.F").WithLocation(17, 64),
                // (19,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 9
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(19, 80),
                // (20,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; } // 10
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(20, 80),
                // (21,96): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; } // 11
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(21, 96),
                // (22,80): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 12
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sOut.F").WithLocation(22, 80),
                // (24,61): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 13
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(24, 61),
                // (25,61): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 14
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(25, 61),
                // (26,77): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 15
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(26, 77),
                // (27,61): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 16
                Diagnostic(ErrorCode.ERR_AssgReadonly, "sIn.F").WithLocation(27, 61));
        }

        [Fact]
        public void AssignOutParameterFrom_RefField()
        {
            var source =
@"#pragma warning disable 649
ref struct S<T>
{
    public ref T Ref;
    public ref readonly T RefReadonly;
    public readonly ref T ReadonlyRef;
    public readonly ref readonly T ReadonlyRefReadonly;
}

class Program
{
    static void FromValueRef<T>(S<T> s, out T t)                 { t = s.Ref; }
    static void FromValueRefReadonly<T>(S<T> s, out T t)         { t = s.RefReadonly; }
    static void FromValueReadonlyRef<T>(S<T> s, out T t)         { t = s.ReadonlyRef; }
    static void FromValueReadonlyRefReadonly<T>(S<T> s, out T t) { t = s.ReadonlyRefReadonly; }

    static void FromRefRef<T>(ref S<T> s, out T t)                 { t = s.Ref; }
    static void FromRefRefReadonly<T>(ref S<T> s, out T t)         { t = s.RefReadonly; }
    static void FromRefReadonlyRef<T>(ref S<T> s, out T t)         { t = s.ReadonlyRef; }
    static void FromRefReadonlyRefReadonly<T>(ref S<T> s, out T t) { t = s.ReadonlyRefReadonly; }

    static void FromOutRef<T>(out S<T> s, out T t)                 { s = default; t = s.Ref; }
    static void FromOutRefReadonly<T>(out S<T> s, out T t)         { s = default; t = s.RefReadonly; }
    static void FromOutReadonlyRef<T>(out S<T> s, out T t)         { s = default; t = s.ReadonlyRef; }
    static void FromOutReadonlyRefReadonly<T>(out S<T> s, out T t) { s = default; t = s.ReadonlyRefReadonly; }

    static void FromInRef<T>(in S<T> s, out T t)                 { t = s.Ref; }
    static void FromInRefReadonly<T>(in S<T> s, out T t)         { t = s.RefReadonly; }
    static void FromInReadonlyRef<T>(in S<T> s, out T t)         { t = s.ReadonlyRef; }
    static void FromInReadonlyRefReadonly<T>(in S<T> s, out T t) { t = s.ReadonlyRefReadonly; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void AssignRefLocalFrom_RefField()
        {
            var source =
@"ref struct S<T>
{
    public ref T Ref;
    public ref readonly T RefReadonly;
    public readonly ref T ReadonlyRef;
    public readonly ref readonly T ReadonlyRefReadonly;
}

class Program
{
    static void FromValueRef<T>(S<T> s)                 { ref T t = ref s.Ref; }
    static void FromValueRefReadonly<T>(S<T> s)         { ref T t = ref s.RefReadonly; } // 1
    static void FromValueReadonlyRef<T>(S<T> s)         { ref T t = ref s.ReadonlyRef; }
    static void FromValueReadonlyRefReadonly<T>(S<T> s) { ref T t = ref s.ReadonlyRefReadonly; } // 2

    static void FromRefRef<T>(ref S<T> s)                 { ref T t = ref s.Ref; }
    static void FromRefRefReadonly<T>(ref S<T> s)         { ref T t = ref s.RefReadonly; } // 3
    static void FromRefReadonlyRef<T>(ref S<T> s)         { ref T t = ref s.ReadonlyRef; }
    static void FromRefReadonlyRefReadonly<T>(ref S<T> s) { ref T t = ref s.ReadonlyRefReadonly; } // 4

    static void FromOutRef<T>(out S<T> s)                 { s = default; ref T t = ref s.Ref; }
    static void FromOutRefReadonly<T>(out S<T> s)         { s = default; ref T t = ref s.RefReadonly; } // 5
    static void FromOutReadonlyRef<T>(out S<T> s)         { s = default; ref T t = ref s.ReadonlyRef; }
    static void FromOutReadonlyRefReadonly<T>(out S<T> s) { s = default; ref T t = ref s.ReadonlyRefReadonly; } // 6

    static void FromInRef<T>(in S<T> s)                 { ref T t = ref s.Ref; }
    static void FromInRefReadonly<T>(in S<T> s)         { ref T t = ref s.RefReadonly; } // 7
    static void FromInReadonlyRef<T>(in S<T> s)         { ref T t = ref s.ReadonlyRef; }
    static void FromInReadonlyRefReadonly<T>(in S<T> s) { ref T t = ref s.ReadonlyRefReadonly; } // 8
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,73): error CS8329: Cannot use field 'S<T>.RefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromValueRefReadonly<T>(S<T> s)         { ref T t = ref s.RefReadonly; } // 1
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.RefReadonly").WithArguments("field", "S<T>.RefReadonly").WithLocation(12, 73),
                // (14,73): error CS8329: Cannot use field 'S<T>.ReadonlyRefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromValueReadonlyRefReadonly<T>(S<T> s) { ref T t = ref s.ReadonlyRefReadonly; } // 2
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.ReadonlyRefReadonly").WithArguments("field", "S<T>.ReadonlyRefReadonly").WithLocation(14, 73),
                // (17,75): error CS8329: Cannot use field 'S<T>.RefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromRefRefReadonly<T>(ref S<T> s)         { ref T t = ref s.RefReadonly; } // 3
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.RefReadonly").WithArguments("field", "S<T>.RefReadonly").WithLocation(17, 75),
                // (19,75): error CS8329: Cannot use field 'S<T>.ReadonlyRefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromRefReadonlyRefReadonly<T>(ref S<T> s) { ref T t = ref s.ReadonlyRefReadonly; } // 4
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.ReadonlyRefReadonly").WithArguments("field", "S<T>.ReadonlyRefReadonly").WithLocation(19, 75),
                // (22,88): error CS8329: Cannot use field 'S<T>.RefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromOutRefReadonly<T>(out S<T> s)         { s = default; ref T t = ref s.RefReadonly; } // 5
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.RefReadonly").WithArguments("field", "S<T>.RefReadonly").WithLocation(22, 88),
                // (24,88): error CS8329: Cannot use field 'S<T>.ReadonlyRefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromOutReadonlyRefReadonly<T>(out S<T> s) { s = default; ref T t = ref s.ReadonlyRefReadonly; } // 6
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.ReadonlyRefReadonly").WithArguments("field", "S<T>.ReadonlyRefReadonly").WithLocation(24, 88),
                // (27,73): error CS8329: Cannot use field 'S<T>.RefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromInRefReadonly<T>(in S<T> s)         { ref T t = ref s.RefReadonly; } // 7
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.RefReadonly").WithArguments("field", "S<T>.RefReadonly").WithLocation(27, 73),
                // (29,73): error CS8329: Cannot use field 'S<T>.ReadonlyRefReadonly' as a ref or out value because it is a readonly variable
                //     static void FromInReadonlyRefReadonly<T>(in S<T> s) { ref T t = ref s.ReadonlyRefReadonly; } // 8
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.ReadonlyRefReadonly").WithArguments("field", "S<T>.ReadonlyRefReadonly").WithLocation(29, 73));
        }

        [Fact]
        public void AssignRefReadonlyLocalFrom_RefField()
        {
            var source =
@"ref struct S<T>
{
    public ref T Ref;
    public ref readonly T RefReadonly;
    public readonly ref T ReadonlyRef;
    public readonly ref readonly T ReadonlyRefReadonly;
}

class Program
{
    static void FromValueRef<T>(S<T> s)                 { ref readonly T t = ref s.Ref; }
    static void FromValueRefReadonly<T>(S<T> s)         { ref readonly T t = ref s.RefReadonly; }
    static void FromValueReadonlyRef<T>(S<T> s)         { ref readonly T t = ref s.ReadonlyRef; }
    static void FromValueReadonlyRefReadonly<T>(S<T> s) { ref readonly T t = ref s.ReadonlyRefReadonly; }

    static void FromRefRef<T>(ref S<T> s)                 { ref readonly T t = ref s.Ref; }
    static void FromRefRefReadonly<T>(ref S<T> s)         { ref readonly T t = ref s.RefReadonly; }
    static void FromRefReadonlyRef<T>(ref S<T> s)         { ref readonly T t = ref s.ReadonlyRef; }
    static void FromRefReadonlyRefReadonly<T>(ref S<T> s) { ref readonly T t = ref s.ReadonlyRefReadonly; }

    static void FromOutRef<T>(out S<T> s)                 { s = default; ref readonly T t = ref s.Ref; }
    static void FromOutRefReadonly<T>(out S<T> s)         { s = default; ref readonly T t = ref s.RefReadonly; }
    static void FromOutReadonlyRef<T>(out S<T> s)         { s = default; ref readonly T t = ref s.ReadonlyRef; }
    static void FromOutReadonlyRefReadonly<T>(out S<T> s) { s = default; ref readonly T t = ref s.ReadonlyRefReadonly; }

    static void FromInRef<T>(in S<T> s)                 { ref readonly T t = ref s.Ref; }
    static void FromInRefReadonly<T>(in S<T> s)         { ref readonly T t = ref s.RefReadonly; }
    static void FromInReadonlyRef<T>(in S<T> s)         { ref readonly T t = ref s.ReadonlyRef; }
    static void FromInReadonlyRefReadonly<T>(in S<T> s) { ref readonly T t = ref s.ReadonlyRefReadonly; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefReturn()
        {
            var source =
@"class Program
{
    static ref T F1<T>(T t) => ref t; // 1
    static ref T F2<T>(ref T t) => ref t;
    static ref T F3<T>(out T t) { t = default; return ref t; } // 2
    static ref T F4<T>(in T t) => ref t; // 3
    static ref readonly T F5<T>(T t) => ref t; // 4
    static ref readonly T F6<T>(ref T t) => ref t;
    static ref readonly T F7<T>(out T t) { t = default; return ref t; } // 5
    static ref readonly T F8<T>(in T t) => ref t;
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,36): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static ref T F1<T>(T t) => ref t; // 1
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(3, 36),
                // (5,59): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static ref T F3<T>(out T t) { t = default; return ref t; } // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(5, 59),
                // (6,39): error CS8333: Cannot return variable 'in T' by writable reference because it is a readonly variable
                //     static ref T F4<T>(in T t) => ref t; // 3
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "t").WithArguments("variable", "in T").WithLocation(6, 39),
                // (7,45): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static ref readonly T F5<T>(T t) => ref t; // 4
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(7, 45),
                // (9,68): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static ref readonly T F7<T>(out T t) { t = default; return ref t; } // 5
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(9, 68));
        }

        [Fact]
        public void RefReturn_Ref()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    public ref T F1() => ref F;
    public ref readonly T F2() => ref F;
}
class Program
{
    static ref T F3<T>(S<T> s) => ref s.F;
    static ref T F4<T>(ref S<T> s) => ref s.F;
    static ref T F5<T>(out S<T> s) { s = default; return ref s.F; }
    static ref T F6<T>(in S<T> s) => ref s.F;
    static ref readonly T F7<T>(S<T> s) => ref s.F;
    static ref readonly T F8<T>(ref S<T> s) => ref s.F;
    static ref readonly T F9<T>(out S<T> s) { s = default; return ref s.F; }
    static ref readonly T F10<T>(in S<T> s) => ref s.F;
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefReturn_RefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
    public ref T F1() => ref F;
    public ref readonly T F2() => ref F;
}
class Program
{
    static ref T F3<T>(S<T> s) => ref s.F;
    static ref T F4<T>(ref S<T> s) => ref s.F;
    static ref T F5<T>(out S<T> s) { s = default; return ref s.F; }
    static ref T F6<T>(in S<T> s) => ref s.F;
    static ref readonly T F7<T>(S<T> s) => ref s.F;
    static ref readonly T F8<T>(ref S<T> s) => ref s.F;
    static ref readonly T F9<T>(out S<T> s) { s = default; return ref s.F; }
    static ref readonly T F10<T>(in S<T> s) => ref s.F;
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,30): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     public ref T F1() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(4, 30),
                // (9,39): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F3<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(9, 39),
                // (10,43): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F4<T>(ref S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(10, 43),
                // (11,62): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F5<T>(out S<T> s) { s = default; return ref s.F; }
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(11, 62),
                // (12,42): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F6<T>(in S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(12, 42));
        }

        [Fact]
        public void RefReturn_ReadonlyRef()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref T F;
    public ref T F1() => ref F;
    public ref readonly T F2() => ref F;
}
class Program
{
    static ref T F3<T>(S<T> s) => ref s.F;
    static ref T F4<T>(ref S<T> s) => ref s.F;
    static ref T F5<T>(out S<T> s) { s = default; return ref s.F; }
    static ref T F6<T>(in S<T> s) => ref s.F;
    static ref readonly T F7<T>(S<T> s) => ref s.F;
    static ref readonly T F8<T>(ref S<T> s) => ref s.F;
    static ref readonly T F9<T>(out S<T> s) { s = default; return ref s.F; }
    static ref readonly T F10<T>(in S<T> s) => ref s.F;
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefReturn_ReadonlyRefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
    public ref T F1() => ref F;
    public ref readonly T F2() => ref F;
}
class Program
{
    static ref T F3<T>(S<T> s) => ref s.F;
    static ref T F4<T>(ref S<T> s) => ref s.F;
    static ref T F5<T>(out S<T> s) { s = default; return ref s.F; }
    static ref T F6<T>(in S<T> s) => ref s.F;
    static ref readonly T F7<T>(S<T> s) => ref s.F;
    static ref readonly T F8<T>(ref S<T> s) => ref s.F;
    static ref readonly T F9<T>(out S<T> s) { s = default; return ref s.F; }
    static ref readonly T F10<T>(in S<T> s) => ref s.F;
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,30): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     public ref T F1() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(4, 30),
                // (9,39): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F3<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(9, 39),
                // (10,43): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F4<T>(ref S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(10, 43),
                // (11,62): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F5<T>(out S<T> s) { s = default; return ref s.F; }
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(11, 62),
                // (12,42): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     static ref T F6<T>(in S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(12, 42));
        }

        [Fact]
        public void RefParameter_InstanceMethod_Ref()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    public S(ref T t)
    {
        F = ref t;
        M1(F);
        M2(ref F);
        M3(out F);
        M4(F);
        M4(in F);
    }
    object P
    {
        init
        {
            M1(F);
            M2(ref F);
            M3(out F);
            M4(F);
            M4(in F);
        }
    }
    void M()
    {
        M1(F);
        M2(ref F);
        M3(out F);
        M4(F);
        M4(in F);
    }
    static void M1(T t) { }
    static void M2(ref T t) { }
    static void M3(out T t) { t = default; }
    static void M4(in T t) { }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefParameter_InstanceMethod_RefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
    public S(ref T t)
    {
        F = ref t;
        M1(F);
        M2(ref F); // 1
        M3(out F); // 2
        M4(F);
        M4(in F);
    }
    object P
    {
        init
        {
            M1(F);
            M2(ref F); // 3
            M3(out F); // 4
            M4(F);
            M4(in F);
        }
    }
    void M()
    {
        M1(F);
        M2(ref F); // 5
        M3(out F); // 6
        M4(F);
        M4(in F);
    }
    static void M1(T t) { }
    static void M2(ref T t) { }
    static void M3(out T t) { t = default; }
    static void M4(in T t) { }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F); // 1
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(8, 16),
                // (9,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F); // 2
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(9, 16),
                // (18,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M2(ref F); // 3
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(18, 20),
                // (19,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M3(out F); // 4
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(19, 20),
                // (27,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F); // 5
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(27, 16),
                // (28,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F); // 6
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(28, 16));
        }

        [Fact]
        public void RefParameter_InstanceMethod_ReadonlyRef()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref T F;
    public S(ref T t)
    {
        F = ref t;
        M1(F);
        M2(ref F);
        M3(out F);
        M4(F);
        M4(in F);
    }
    object P
    {
        init
        {
            M1(F);
            M2(ref F);
            M3(out F);
            M4(F);
            M4(in F);
        }
    }
    void M()
    {
        M1(F);
        M2(ref F);
        M3(out F);
        M4(F);
        M4(in F);
    }
    static void M1(T t) { }
    static void M2(ref T t) { }
    static void M3(out T t) { t = default; }
    static void M4(in T t) { }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefParameter_InstanceMethod_ReadonlyRefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
    public S(ref T t)
    {
        F = ref t;
        M1(F);
        M2(ref F); // 1
        M3(out F); // 2
        M4(F);
        M4(in F);
    }
    object P
    {
        init
        {
            M1(F);
            M2(ref F); // 3
            M3(out F); // 4
            M4(F);
            M4(in F);
        }
    }
    void M()
    {
        M1(F);
        M2(ref F); // 5
        M3(out F); // 6
        M4(F);
        M4(in F);
    }
    static void M1(T t) { }
    static void M2(ref T t) { }
    static void M3(out T t) { t = default; }
    static void M4(in T t) { }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F); // 1
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(8, 16),
                // (9,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F); // 2
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(9, 16),
                // (18,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M2(ref F); // 3
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(18, 20),
                // (19,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M3(out F); // 4
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(19, 20),
                // (27,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F); // 5
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(27, 16),
                // (28,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F); // 6
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(28, 16));
        }

        [Fact]
        public void RefParameter_Ref()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
}

class Program
{
    static void M1<T>(T t) { }
    static void M2<T>(ref T t) { }
    static void M3<T>(out T t) { t = default; }
    static void M4<T>(in T t) { }

    static void FromValue1<T>(S<T> s)  { M1(s.F); }
    static void FromValue2<T>(S<T> s)  { M2(ref s.F); }
    static void FromValue3<T>(S<T> s)  { M3(out s.F); }
    static void FromValue4A<T>(S<T> s) { M4(in s.F); }
    static void FromValue4B<T>(S<T> s) { M4(s.F); }

    static void FromRef1<T>(ref S<T> s)  { M1(s.F); }
    static void FromRef2<T>(ref S<T> s)  { M2(ref s.F); }
    static void FromRef3<T>(ref S<T> s)  { M3(out s.F); }
    static void FromRef4A<T>(ref S<T> s) { M4(in s.F); }
    static void FromRef4B<T>(ref S<T> s) { M4(s.F); }

    static void FromOut1<T>(out S<T> s)  { s = default; M1(s.F); }
    static void FromOut2<T>(out S<T> s)  { s = default; M2(ref s.F); }
    static void FromOut3<T>(out S<T> s)  { s = default; M3(out s.F); }
    static void FromOut4A<T>(out S<T> s) { s = default; M4(in s.F); }
    static void FromOut4B<T>(out S<T> s) { s = default; M4(s.F); }

    static void FromIn1<T>(in S<T> s)  { M1(s.F); }
    static void FromIn2<T>(in S<T> s)  { M2(ref s.F); }
    static void FromIn3<T>(in S<T> s)  { M3(out s.F); }
    static void FromIn4A<T>(in S<T> s) { M4(in s.F); }
    static void FromIn4B<T>(in S<T> s) { M4(s.F); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefParameter_RefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
}

class Program
{
    static void M1<T>(T t) { }
    static void M2<T>(ref T t) { }
    static void M3<T>(out T t) { t = default; }
    static void M4<T>(in T t) { }

    static void FromValue1<T>(S<T> s)  { M1(s.F); }
    static void FromValue2<T>(S<T> s)  { M2(ref s.F); } // 1
    static void FromValue3<T>(S<T> s)  { M3(out s.F); } // 2
    static void FromValue4A<T>(S<T> s) { M4(in s.F); }
    static void FromValue4B<T>(S<T> s) { M4(s.F); }

    static void FromRef1<T>(ref S<T> s)  { M1(s.F); }
    static void FromRef2<T>(ref S<T> s)  { M2(ref s.F); } // 3
    static void FromRef3<T>(ref S<T> s)  { M3(out s.F); } // 4
    static void FromRef4A<T>(ref S<T> s) { M4(in s.F); }
    static void FromRef4B<T>(ref S<T> s) { M4(s.F); }

    static void FromOut1<T>(out S<T> s)  { s = default; M1(s.F); }
    static void FromOut2<T>(out S<T> s)  { s = default; M2(ref s.F); } // 5
    static void FromOut3<T>(out S<T> s)  { s = default; M3(out s.F); } // 6
    static void FromOut4A<T>(out S<T> s) { s = default; M4(in s.F); }
    static void FromOut4B<T>(out S<T> s) { s = default; M4(s.F); }

    static void FromIn1<T>(in S<T> s)  { M1(s.F); }
    static void FromIn2<T>(in S<T> s)  { M2(ref s.F); } // 7
    static void FromIn3<T>(in S<T> s)  { M3(out s.F); } // 8
    static void FromIn4A<T>(in S<T> s) { M4(in s.F); }
    static void FromIn4B<T>(in S<T> s) { M4(s.F); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromValue2<T>(S<T> s)  { M2(ref s.F); } // 1
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(14, 49),
                // (15,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromValue3<T>(S<T> s)  { M3(out s.F); } // 2
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(15, 49),
                // (20,51): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromRef2<T>(ref S<T> s)  { M2(ref s.F); } // 3
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(20, 51),
                // (21,51): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromRef3<T>(ref S<T> s)  { M3(out s.F); } // 4
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(21, 51),
                // (26,64): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromOut2<T>(out S<T> s)  { s = default; M2(ref s.F); } // 5
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(26, 64),
                // (27,64): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromOut3<T>(out S<T> s)  { s = default; M3(out s.F); } // 6
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(27, 64),
                // (32,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromIn2<T>(in S<T> s)  { M2(ref s.F); } // 7
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(32, 49),
                // (33,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromIn3<T>(in S<T> s)  { M3(out s.F); } // 8
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(33, 49));
        }

        [Fact]
        public void RefParameter_ReadonlyRef()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref T F;
}

class Program
{
    static void M1<T>(T t) { }
    static void M2<T>(ref T t) { }
    static void M3<T>(out T t) { t = default; }
    static void M4<T>(in T t) { }

    static void FromValue1<T>(S<T> s)  { M1(s.F); }
    static void FromValue2<T>(S<T> s)  { M2(ref s.F); }
    static void FromValue3<T>(S<T> s)  { M3(out s.F); }
    static void FromValue4A<T>(S<T> s) { M4(in s.F); }
    static void FromValue4B<T>(S<T> s) { M4(s.F); }

    static void FromRef1<T>(ref S<T> s)  { M1(s.F); }
    static void FromRef2<T>(ref S<T> s)  { M2(ref s.F); }
    static void FromRef3<T>(ref S<T> s)  { M3(out s.F); }
    static void FromRef4A<T>(ref S<T> s) { M4(in s.F); }
    static void FromRef4B<T>(ref S<T> s) { M4(s.F); }

    static void FromOut1<T>(out S<T> s)  { s = default; M1(s.F); }
    static void FromOut2<T>(out S<T> s)  { s = default; M2(ref s.F); }
    static void FromOut3<T>(out S<T> s)  { s = default; M3(out s.F); }
    static void FromOut4A<T>(out S<T> s) { s = default; M4(in s.F); }
    static void FromOut4B<T>(out S<T> s) { s = default; M4(s.F); }

    static void FromIn1<T>(in S<T> s)  { M1(s.F); }
    static void FromIn2<T>(in S<T> s)  { M2(ref s.F); }
    static void FromIn3<T>(in S<T> s)  { M3(out s.F); }
    static void FromIn4A<T>(in S<T> s) { M4(in s.F); }
    static void FromIn4B<T>(in S<T> s) { M4(s.F); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefParameter_ReadonlyRefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
}

class Program
{
    static void M1<T>(T t) { }
    static void M2<T>(ref T t) { }
    static void M3<T>(out T t) { t = default; }
    static void M4<T>(in T t) { }

    static void FromValue1<T>(S<T> s)  { M1(s.F); }
    static void FromValue2<T>(S<T> s)  { M2(ref s.F); } // 1
    static void FromValue3<T>(S<T> s)  { M3(out s.F); } // 2
    static void FromValue4A<T>(S<T> s) { M4(in s.F); }
    static void FromValue4B<T>(S<T> s) { M4(s.F); }

    static void FromRef1<T>(ref S<T> s)  { M1(s.F); }
    static void FromRef2<T>(ref S<T> s)  { M2(ref s.F); } // 3
    static void FromRef3<T>(ref S<T> s)  { M3(out s.F); } // 4
    static void FromRef4A<T>(ref S<T> s) { M4(in s.F); }
    static void FromRef4B<T>(ref S<T> s) { M4(s.F); }

    static void FromOut1<T>(out S<T> s)  { s = default; M1(s.F); }
    static void FromOut2<T>(out S<T> s)  { s = default; M2(ref s.F); } // 5
    static void FromOut3<T>(out S<T> s)  { s = default; M3(out s.F); } // 6
    static void FromOut4A<T>(out S<T> s) { s = default; M4(in s.F); }
    static void FromOut4B<T>(out S<T> s) { s = default; M4(s.F); }

    static void FromIn1<T>(in S<T> s)  { M1(s.F); }
    static void FromIn2<T>(in S<T> s)  { M2(ref s.F); } // 7
    static void FromIn3<T>(in S<T> s)  { M3(out s.F); } // 8
    static void FromIn4A<T>(in S<T> s) { M4(in s.F); }
    static void FromIn4B<T>(in S<T> s) { M4(s.F); }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromValue2<T>(S<T> s)  { M2(ref s.F); } // 1
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(14, 49),
                // (15,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromValue3<T>(S<T> s)  { M3(out s.F); } // 2
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(15, 49),
                // (20,51): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromRef2<T>(ref S<T> s)  { M2(ref s.F); } // 3
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(20, 51),
                // (21,51): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromRef3<T>(ref S<T> s)  { M3(out s.F); } // 4
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(21, 51),
                // (26,64): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromOut2<T>(out S<T> s)  { s = default; M2(ref s.F); } // 5
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(26, 64),
                // (27,64): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromOut3<T>(out S<T> s)  { s = default; M3(out s.F); } // 6
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(27, 64),
                // (32,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromIn2<T>(in S<T> s)  { M2(ref s.F); } // 7
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(32, 49),
                // (33,49): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //     static void FromIn3<T>(in S<T> s)  { M3(out s.F); } // 8
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(33, 49));
        }

        [Fact]
        public void RefParameter_ReadonlyRefReadonly_PEVerifyCompat()
        {
            var source =
@"#pragma warning disable 649
ref struct S<T>
{
    public readonly ref readonly T F;
}

class Program
{
    static void M1<T>(T t) { }
    static void M4<T>(in T t) { }

    static void FromValue1<T>(S<T> s)  { M1(s.F); }
    static void FromValue4A<T>(S<T> s) { M4(in s.F); }
    static void FromValue4B<T>(S<T> s) { M4(s.F); }

    static void FromRef1<T>(ref S<T> s)  { M1(s.F); }
    static void FromRef4A<T>(ref S<T> s) { M4(in s.F); }
    static void FromRef4B<T>(ref S<T> s) { M4(s.F); }

    static void FromOut1<T>(out S<T> s)  { s = default; M1(s.F); }
    static void FromOut4A<T>(out S<T> s) { s = default; M4(in s.F); }
    static void FromOut4B<T>(out S<T> s) { s = default; M4(s.F); }

    static void FromIn1<T>(in S<T> s)  { M1(s.F); }
    static void FromIn4A<T>(in S<T> s) { M4(in s.F); }
    static void FromIn4B<T>(in S<T> s) { M4(s.F); }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithFeature("peverify-compat"));
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void InitobjField()
        {
            var source =
@"using System;
struct S<T>
{
    public T F;
}
ref struct R<T>
{
    public ref S<T> S;
}
class Program
{
    static void Main()
    {
        var s = new S<int>();
        scoped var r = new R<int>();
        r.S = ref s;
        NewField(ref r);
        r.S.F = 42;
        Console.WriteLine(s.F);
        Console.WriteLine(r.S.F);
    }
    static void NewField<T>(ref R<T> r)
    {
        r.S = new S<T>();
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"42
42
"));
            verifier.VerifyIL("Program.NewField<T>",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref S<T> R<T>.S""
  IL_0006:  initobj    ""S<T>""
  IL_000c:  ret
}");
        }

        [Fact]
        public void ReadWriteField()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int x = 1;
        scoped var s = new S<int>();
        s.F = ref x;
        Write(s, 42);
        Console.WriteLine(Read(s));
        Console.WriteLine(x);
    }
    static int Read(S<int> s)
    {
        return s.F;
    }
    static void Write(S<int> s, int value)
    {
        s.F = value;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"42
42
"));
            verifier.VerifyIL("S<T>..ctor(ref T)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""ref T S<T>.F""
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.Read",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref int S<int>.F""
  IL_0006:  ldind.i4
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.Write",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref int S<int>.F""
  IL_0006:  ldarg.1
  IL_0007:  stind.i4
  IL_0008:  ret
}");
        }

        [Fact]
        public void ReadWriteNestedField()
        {
            var source =
@"using System;
ref struct R1<T>
{
    public ref T F;
    public R1(ref T t) { F = ref t; }
}
ref struct R2<T>
{
    public ref R1<T> R1;
    public R2(ref R1<T> r1) { R1 = ref r1; }
}
class Program
{
    static void Main()
    {
        int i = 0;
        var r1 = new R1<int>(ref i);
        var r2 = new R2<int>(ref r1);
        r2.R1.F = 42;
        Console.WriteLine(Read(r2));
        Console.WriteLine(ReadIn(r2));
        Console.WriteLine(i);
    }
    static T Read<T>(R2<T> r2)
    {
        return r2.R1.F;
    }
    static T ReadIn<T>(in R2<T> r2In)
    {
        return r2In.R1.F;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,12): error CS9050: A ref field cannot refer to a ref struct.
                //     public ref R1<T> R1;
                Diagnostic(ErrorCode.ERR_RefFieldCannotReferToRefStruct, "ref R1<T>").WithLocation(9, 12));
        }

        [Fact]
        public void ReadWriteFieldWithTemp()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    private int _other;
    public S(int other) : this() { _other = other; }
}
class Program
{
    static T ReadWrite1<T>(ref T t)
    {
        return new S<T>().F = ref t;
    }
    static T ReadWrite2<T>(ref T t)
    {
        return new S<T>(1).F = ref t;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,16): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         return new S<T>().F = ref t;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new S<T>().F").WithLocation(11, 16),
                // (15,16): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         return new S<T>(1).F = ref t;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new S<T>(1).F").WithLocation(15, 16));
        }

        [WorkItem(62122, "https://github.com/dotnet/roslyn/issues/62122")]
        [Fact]
        public void ReadAndDiscard()
        {
            var source =
@"ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int i = 1;
        ReadAndDiscard1(ref i);
        ReadAndDiscardNoArg<int>();
        ReadAndDiscard2(new S<int>(ref i));
    }
    static void ReadAndDiscard1<T>(ref T t)
    {
        _ = new S<T>(ref t).F;
    }
    static void ReadAndDiscardNoArg<T>()
    {
        _ = new S<T>().F;
    }
    static void ReadAndDiscard2<T>(in S<T> s)
    {
        _ = s.F;
    }
}";
            // https://github.com/dotnet/roslyn/issues/62122: The dereference of a ref field
            // should be emitted to IL, even if the value is ignored, because the behavior
            // may be observable as a NullReferenceException.
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(""));
            verifier.VerifyIL("Program.ReadAndDiscard1<T>",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""S<T>..ctor(ref T)""
  IL_0006:  pop
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.ReadAndDiscardNoArg<T>",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyIL("Program.ReadAndDiscard2<T>",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [Fact]
        public void RefReturn_ByValueArg()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int i = 1;
        var s = new S<int>(ref i);
        RefReturn(s) = 2;
        i = RefReadonlyReturn(s);
        Console.WriteLine(i);
    }
    static ref T RefReturn<T>(S<T> s) => ref s.F;
    static ref readonly T RefReadonlyReturn<T>(S<T> s) => ref s.F;
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("2"));
            var expectedIL =
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""ref T S<T>.F""
  IL_0007:  ret
}";
            verifier.VerifyIL("Program.RefReturn<T>", expectedIL);
            verifier.VerifyIL("Program.RefReadonlyReturn<T>", expectedIL);
        }

        [Fact]
        public void RefReturn_RefArg()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int i = 1;
        var s = new S<int>(ref i);
        RefReturn(ref s) = 2;
        i = RefReadonlyReturn(ref s);
        Console.WriteLine(i);
    }
    static ref T RefReturn<T>(ref S<T> s) => ref s.F;
    static ref readonly T RefReadonlyReturn<T>(ref S<T> s) => ref s.F;
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("2"));
            var expectedIL =
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ret
}";
            verifier.VerifyIL("Program.RefReturn<T>", expectedIL);
            verifier.VerifyIL("Program.RefReadonlyReturn<T>", expectedIL);
        }

        [Fact]
        public void RefReturn_InArg()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int i = 1;
        var s = new S<int>(ref i);
        RefReturn(s) = 2;
        i = RefReadonlyReturn(s);
        Console.WriteLine(i);
    }
    static ref T RefReturn<T>(in S<T> s) => ref s.F;
    static ref readonly T RefReadonlyReturn<T>(in S<T> s) => ref s.F;
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("2"));
            var expectedIL =
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.F""
  IL_0006:  ret
}";
            verifier.VerifyIL("Program.RefReturn<T>", expectedIL);
            verifier.VerifyIL("Program.RefReadonlyReturn<T>", expectedIL);
        }

        [Fact]
        public void RefReturn_OutArg()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int i = 1;
        S<int> s;
        RefReturn(out s, ref i) = 2;
        i = RefReadonlyReturn(out s, ref i);
        Console.WriteLine(i);
    }
    static ref T RefReturn<T>(out S<T> s, ref T t)
    {
        s = new S<T>(ref t);
        return ref s.F;
    }
    static ref readonly T RefReadonlyReturn<T>(out S<T> s, ref T t)
    {
        s = new S<T>(ref t);
        return ref s.F;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("2"));
            var expectedIL =
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  newobj     ""S<T>..ctor(ref T)""
  IL_0007:  stobj      ""S<T>""
  IL_000c:  ldarg.0
  IL_000d:  ldfld      ""ref T S<T>.F""
  IL_0012:  ret
}";
            verifier.VerifyIL("Program.RefReturn<T>", expectedIL);
            verifier.VerifyIL("Program.RefReadonlyReturn<T>", expectedIL);
        }

        [Fact]
        public void CompoundOperations_01()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int x = 42;
        scoped var s = new S<int>(ref x);
        Increment(s);
        Console.WriteLine(s.F);
        Console.WriteLine(x);
        Subtract(s, 10);
        Console.WriteLine(s.F);
        Console.WriteLine(x);
    }
    static void Increment(S<int> s)
    {
        s.F++;
    }
    static void Subtract(S<int> s, int offset)
    {
        s.F -= offset;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"43
43
33
33
"));
            verifier.VerifyIL("Program.Increment",
@"{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""ref int S<int>.F""
  IL_0007:  dup
  IL_0008:  ldind.i4
  IL_0009:  ldc.i4.1
  IL_000a:  add
  IL_000b:  stind.i4
  IL_000c:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""ref int S<int>.F""
  IL_0007:  dup
  IL_0008:  ldind.i4
  IL_0009:  ldarg.1
  IL_000a:  sub
  IL_000b:  stind.i4
  IL_000c:  ret
}");
        }

        [Theory]
        [InlineData("ref")]
        [InlineData("readonly ref")]
        public void CompoundOperations_02(string refKind)
        {
            var source =
$@"using System;
ref struct S
{{
    public {refKind} int F;
    public S(ref int i) {{ F = ref i; }}
    public void Increment()
    {{
        F++;
    }}
    public void Subtract(int offset)
    {{
        F -= offset;
    }}
}}
class Program
{{
    static void Main()
    {{
        int x = 42;
        scoped var s = new S(ref x);
        s.Increment();
        Console.WriteLine(s.F);
        Console.WriteLine(x);
        s.Subtract(10);
        Console.WriteLine(s.F);
        Console.WriteLine(x);
    }}
}}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"43
43
33
33
"));
            verifier.VerifyIL("S.Increment",
@"{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref int S.F""
  IL_0006:  ldarg.0
  IL_0007:  ldfld      ""ref int S.F""
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stind.i4
  IL_0010:  ret
}");
            verifier.VerifyIL("S.Subtract",
@"{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref int S.F""
  IL_0006:  ldarg.0
  IL_0007:  ldfld      ""ref int S.F""
  IL_000c:  ldind.i4
  IL_000d:  ldarg.1
  IL_000e:  sub
  IL_000f:  stind.i4
  IL_0010:  ret
}");
        }

        [Fact]
        public void ConditionalOperator()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int x = 1;
        int y = 2;
        var sx = new S<int>(ref x);
        var sy = new S<int>(ref y);
        Console.WriteLine(ConditionalOperator(true, sx, sy));
        Console.WriteLine(ConditionalOperator(false, sx, sy));
        ConditionalOperatorRef(true, ref sx, ref sy) = 3;
        ConditionalOperatorRef(false, ref sx, ref sy) = 4;
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
    static T ConditionalOperator<T>(bool b, S<T> sx, S<T> sy)
    {
        return b ? sx.F : sy.F;
    }
    static ref T ConditionalOperatorRef<T>(bool b, ref S<T> sx, ref S<T> sy)
    {
        return ref b ? ref sx.F : ref sy.F;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"1
2
3
4
"));
            verifier.VerifyIL("Program.ConditionalOperator<T>",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000f
  IL_0003:  ldarg.2
  IL_0004:  ldfld      ""ref T S<T>.F""
  IL_0009:  ldobj      ""T""
  IL_000e:  ret
  IL_000f:  ldarg.1
  IL_0010:  ldfld      ""ref T S<T>.F""
  IL_0015:  ldobj      ""T""
  IL_001a:  ret
}");
            verifier.VerifyIL("Program.ConditionalOperatorRef<T>",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldarg.2
  IL_0004:  ldfld      ""ref T S<T>.F""
  IL_0009:  ret
  IL_000a:  ldarg.1
  IL_000b:  ldfld      ""ref T S<T>.F""
  IL_0010:  ret
}");
        }

        [Fact]
        public void ConditionalAccess()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        object o = 1;
        int i = 2;
        var s1 = new S<object>(ref o);
        var s2 = new S<int>(ref i);
        Console.WriteLine(ConditionalAccess(s1));
        Console.WriteLine(ConditionalAccess(s2));
    }
    static string ConditionalAccess<T>(S<T> s)
    {
        return s.F?.ToString();
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"1
2
"));
            verifier.VerifyIL("Program.ConditionalAccess<T>",
@"{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""ref T S<T>.F""
  IL_0007:  ldloca.s   V_0
  IL_0009:  initobj    ""T""
  IL_000f:  ldloc.0
  IL_0010:  box        ""T""
  IL_0015:  brtrue.s   IL_002a
  IL_0017:  ldobj      ""T""
  IL_001c:  stloc.0
  IL_001d:  ldloca.s   V_0
  IL_001f:  ldloc.0
  IL_0020:  box        ""T""
  IL_0025:  brtrue.s   IL_002a
  IL_0027:  pop
  IL_0028:  ldnull
  IL_0029:  ret
  IL_002a:  constrained. ""T""
  IL_0030:  callvirt   ""string object.ToString()""
  IL_0035:  ret
}");
        }

        [Fact]
        public void Deconstruct()
        {
            var source =
@"using System;
class Pair<T, U>
{
    public readonly T First;
    public readonly U Second;
    public Pair(T first, U second)
    {
        First = first;
        Second = second;
    }
    public void Deconstruct(out T first, out U second)
    {
        first = First;
        second = Second;
    }
}
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int i = 0;
        string s = null;
        var s1 = new S<int>(ref i);
        var s2 = new S<string>(ref s);
        Deconstruct(new Pair<int, string>(1, ""Hello world""), s1, s2);
        Console.WriteLine((i, s));
    }
    static void Deconstruct<T, U>(Pair<T, U> pair, S<T> s1, S<U> s2)
    {
        (s1.F, s2.F) = pair;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(@"(1, Hello world)"));
            verifier.VerifyIL("Program.Deconstruct<T, U>",
@"{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (T& V_0,
                T V_1,
                U V_2)
  IL_0000:  ldarga.s   V_1
  IL_0002:  ldfld      ""ref T S<T>.F""
  IL_0007:  stloc.0
  IL_0008:  ldarga.s   V_2
  IL_000a:  ldfld      ""ref U S<U>.F""
  IL_000f:  ldarg.0
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldloca.s   V_2
  IL_0014:  callvirt   ""void Pair<T, U>.Deconstruct(out T, out U)""
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  stobj      ""T""
  IL_0020:  ldloc.2
  IL_0021:  stobj      ""U""
  IL_0026:  ret
}");
        }

        [Fact]
        public void InParamReorder()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int x = 1;
        int y = 2;
        var sx = new S<int>(ref x);
        var sy = new S<int>(ref y);
        Reorder(sx, sy);
    }
    static ref S<T> Get<T>(ref S<T> s)
    {
        return ref s;
    }
    static void Reorder<T>(S<T> sx, S<T> sy)
    {
        M(y: in Get(ref sy).F, x: in Get(ref sx).F);
    }
    static void M<T>(in T x, in T y)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"1
2
"));
            verifier.VerifyIL("Program.Reorder<T>",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (T& V_0)
  IL_0000:  ldarga.s   V_1
  IL_0002:  call       ""ref S<T> Program.Get<T>(ref S<T>)""
  IL_0007:  ldfld      ""ref T S<T>.F""
  IL_000c:  stloc.0
  IL_000d:  ldarga.s   V_0
  IL_000f:  call       ""ref S<T> Program.Get<T>(ref S<T>)""
  IL_0014:  ldfld      ""ref T S<T>.F""
  IL_0019:  ldloc.0
  IL_001a:  call       ""void Program.M<T>(in T, in T)""
  IL_001f:  ret
}");
        }

        [Fact]
        public void ReturnRefToByValueParameter_01()
        {
            var source =
@"ref struct S<T>
{
}
class Program
{
    static ref S<T> F1<T>(ref S<T> x1)
    {
        return ref x1;
    }
    static void F2<T>(S<T> x2)
    {
        var y2 = F1(ref x2);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ReturnRefToByValueParameter_02()
        {
            var source =
@"ref struct S<T>
{
}
class Program
{
    static ref S<T> F1<T>(ref S<T> x1, ref S<T> y1)
    {
        return ref x1;
    }
    static void F2<T>(S<T> x2, S<T> y2)
    {
        var z1 = F1(ref x2, ref y2);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(62098, "https://github.com/dotnet/roslyn/issues/62098")]
        [Fact]
        public void RefToContainingType()
        {
            var source =
@"ref struct R<T>
{
    public ref R<T> Next;
}
class Program
{
    static void F<T>(ref R<T> r)
    {
        r.Next = ref r;
    }
}";
            var comp = CreateCompilation(source);
            // https://github.com/dotnet/roslyn/issues/62098: Allow ref field of the containing type.
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS9050: A ref field cannot refer to a ref struct.
                //     public ref R<T> Next;
                Diagnostic(ErrorCode.ERR_RefFieldCannotReferToRefStruct, "ref R<T>").WithLocation(3, 12),
                // (3,21): error CS0523: Struct member 'R<T>.Next' of type 'R<T>' causes a cycle in the struct layout
                //     public ref R<T> Next;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "Next").WithArguments("R<T>.Next", "R<T>").WithLocation(3, 21));
        }

        /// <summary>
        /// Ref auto-properties are not supported.
        /// </summary>
        [Fact]
        public void RefAutoProperty_01()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T P0 { get; }
    public ref T P1 { get; set; }
    public ref T P2 { get; init; }
    public S(ref T t)
    {
        P0 = ref t;
        P1 = ref t;
        P2 = ref t;
    }
}
class Program
{
    static void Main()
    {
        int x = 0;
        var s = new S<int>(ref x);
        s.P0 = 0;
        s.P1 = 1;
        s.P2 = 2;
        s.P0 = ref x;
        s.P1 = ref x;
        s.P2 = ref x;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,18): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref T P0 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P0").WithLocation(4, 18),
                // (5,18): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref T P1 { get; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P1").WithLocation(5, 18),
                // (5,28): error CS8147: Properties which return by reference cannot have set accessors
                //     public ref T P1 { get; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(5, 28),
                // (6,18): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref T P2 { get; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P2").WithLocation(6, 18),
                // (6,28): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                //     public ref T P2 { get; init; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(6, 28),
                // (6,28): error CS8147: Properties which return by reference cannot have set accessors
                //     public ref T P2 { get; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(6, 28),
                // (9,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P0 = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P0").WithLocation(9, 9),
                // (10,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P1 = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P1").WithLocation(10, 9),
                // (11,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P2 = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P2").WithLocation(11, 9),
                // (23,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.P0 = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P0").WithLocation(23, 9),
                // (24,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.P1 = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P1").WithLocation(24, 9),
                // (25,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.P2 = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P2").WithLocation(25, 9));
        }

        /// <summary>
        /// Ref auto-properties are not supported.
        /// </summary>
        [Fact]
        public void RefAutoProperty_02()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref readonly T P0 { get; }
    public ref readonly T P1 { get; set; }
    public ref readonly T P2 { get; init; }
    public S(ref T t)
    {
        P0 = ref t;
        P1 = ref t;
        P2 = ref t;
    }
}
class Program
{
    static void Main()
    {
        int x = 0;
        var s = new S<int>(ref x);
        s.P0 = ref x;
        s.P1 = ref x;
        s.P2 = ref x;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,27): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref readonly T P0 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P0").WithLocation(4, 27),
                // (5,27): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref readonly T P1 { get; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P1").WithLocation(5, 27),
                // (5,37): error CS8147: Properties which return by reference cannot have set accessors
                //     public ref readonly T P1 { get; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(5, 37),
                // (6,27): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref readonly T P2 { get; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P2").WithLocation(6, 27),
                // (6,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                //     public ref readonly T P2 { get; init; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "init").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(6, 37),
                // (6,37): error CS8147: Properties which return by reference cannot have set accessors
                //     public ref readonly T P2 { get; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(6, 37),
                // (9,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P0 = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P0").WithLocation(9, 9),
                // (10,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P1 = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P1").WithLocation(10, 9),
                // (11,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P2 = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P2").WithLocation(11, 9),
                // (20,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.P0 = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P0").WithLocation(20, 9),
                // (21,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.P1 = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P1").WithLocation(21, 9),
                // (22,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.P2 = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P2").WithLocation(22, 9));
        }

        [Fact]
        public void RefAccessor_Value()
        {
            var source =
@"using System;
ref struct S<T>
{
    internal T t;
    internal ref T F() => ref t;
}
class Program
{
    static void Main()
    {
        var s = new S<int>();
        s.t = 1;
        s.F() = 2;
        Console.WriteLine(s.F());
        Console.WriteLine(s.t);
        s.t = 3;
        Console.WriteLine(s.F());
        Console.WriteLine(s.t);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,31): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     internal ref T F() => ref t;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "t").WithLocation(5, 31));
        }

        [Fact]
        public void RefAccessor_Ref()
        {
            var source =
@"using System;
ref struct S<T>
{
    internal ref T t;
    internal ref T F() => ref t;
    internal S(ref T t) { this.t = ref t; }
}
class Program
{
    static void Main()
    {
        int i = 0;
        var s = new S<int>(ref i);
        s.t = 1;
        s.F() = 2;
        Console.WriteLine(s.F());
        Console.WriteLine(s.t);
        s.t = 3;
        Console.WriteLine(s.F());
        Console.WriteLine(s.t);
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"2
2
3
3
"));
            verifier.VerifyIL("S<T>.F",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref T S<T>.t""
  IL_0006:  ret
}");
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Span(LanguageVersion languageVersion)
        {
            var source =
@"using System;
class Program
{
    static ref int F1()
    {
        Span<int> s1 = stackalloc int[10];
        return ref s1[1]; // 1
    }
    static ref int F2()
    {
        Span<int> s2 = new int[10];
        return ref s2[1];
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (7,20): error CS8352: Cannot use variable 's1' in this context because it may expose referenced variables outside of their declaration scope
                //         return ref s1[1]; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s1").WithArguments("s1").WithLocation(7, 20));
        }

        // Breaking change in C#11: Cannot return an 'out' parameter by reference.
        [Fact]
        public void BreakingChange_ReturnOutByRef()
        {
            var source =
@"class Program
{
    static ref T ReturnOutParamByRef<T>(out T t)
    {
        t = default;
        return ref t;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,20): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //         return ref t;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(6, 20));
        }

        // Breaking change in C#11: The rvalue from a method invocation that
        // returns a ref struct is safe-to-escape from ... the ref-safe-to-escape of all ref arguments.
        [Fact]
        public void BreakingChange_RefStructReturnFromRefArguments()
        {
            var source =
@"ref struct R { }
class Program
{
    static R MayCaptureArg(ref int i) => new R();
    static R MayCaptureDefaultArg(in int i = 0) => new R();
    static R Create()
    {
        int i = 0;
        return MayCaptureArg(ref i);
    }
    static R CreateDefault()
    {
        return MayCaptureDefaultArg();
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,16): error CS8347: Cannot use a result of 'Program.MayCaptureArg(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return MayCaptureArg(ref i);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayCaptureArg(ref i)").WithArguments("Program.MayCaptureArg(ref int)", "i").WithLocation(9, 16),
                // (9,34): error CS8168: Cannot return local 'i' by reference because it is not a ref local
                //         return MayCaptureArg(ref i);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(9, 34),
                // (13,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return MayCaptureDefaultArg();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "MayCaptureDefaultArg()").WithLocation(13, 16),
                // (13,16): error CS8347: Cannot use a result of 'Program.MayCaptureDefaultArg(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return MayCaptureDefaultArg();
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayCaptureDefaultArg()").WithArguments("Program.MayCaptureDefaultArg(in int)", "i").WithLocation(13, 16));
        }

        [Theory]
        [CombinatorialData]
        public void ParameterScope_01(bool useCompilationReference)
        {
            var sourceA =
@"public ref struct R
{
    public R(ref int i) { }
}
public static class A
{
    public static void F1(R x1, scoped R y1) { }
    public static void F2(ref R x2, scoped ref R y2) { }
    public static void F3(in R x3, scoped in R y3) { }
    public static void F4(out R x4, scoped out R y4) { x4 = default; y4 = default; }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (7,33): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static void F1(R x1, scoped R y1) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(7, 33),
                // (8,37): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static void F2(ref R x2, scoped ref R y2) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(8, 37),
                // (9,36): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static void F3(in R x3, scoped in R y3) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(9, 36),
                // (10,37): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static void F4(out R x4, scoped out R y4) { x4 = default; y4 = default; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(10, 37));

            verify(comp);

            comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"static class B
{
    static void F(ref R x)
    {
        int i = 0;
        R y = new R(ref i);
        A.F2(ref x, ref y);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8350: This combination of arguments to 'A.F2(ref R, ref R)' is disallowed because it may expose variables referenced by parameter 'y2' outside of their declaration scope
                //         A.F2(ref x, ref y);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "A.F2(ref x, ref y)").WithArguments("A.F2(ref R, ref R)", "y2").WithLocation(7, 9),
                // (7,25): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         A.F2(ref x, ref y);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(7, 25));

            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var parameters = comp.GetMember<MethodSymbol>("A.F1").Parameters;
                VerifyParameterSymbol(parameters[0], "R x1", RefKind.None, DeclarationScope.Unscoped);
                VerifyParameterSymbol(parameters[1], "scoped R y1", RefKind.None, DeclarationScope.ValueScoped);

                parameters = comp.GetMember<MethodSymbol>("A.F2").Parameters;
                VerifyParameterSymbol(parameters[0], "ref R x2", RefKind.Ref, DeclarationScope.Unscoped);
                VerifyParameterSymbol(parameters[1], "scoped ref R y2", RefKind.Ref, DeclarationScope.RefScoped);

                parameters = comp.GetMember<MethodSymbol>("A.F3").Parameters;
                VerifyParameterSymbol(parameters[0], "in R x3", RefKind.In, DeclarationScope.Unscoped);
                VerifyParameterSymbol(parameters[1], "scoped in R y3", RefKind.In, DeclarationScope.RefScoped);

                parameters = comp.GetMember<MethodSymbol>("A.F4").Parameters;
                VerifyParameterSymbol(parameters[0], "out R x4", RefKind.Out, DeclarationScope.RefScoped);
                VerifyParameterSymbol(parameters[1], "out R y4", RefKind.Out, DeclarationScope.RefScoped);
            }
        }

        [Fact]
        public void ParameterScope_02()
        {
            var source =
@"ref struct A<T>
{
    A(scoped ref T t) { }
    T this[scoped in object o] => default;
}
struct B<T>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (3,7): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     A(scoped ref T t) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(3, 7),
                // (4,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     T this[scoped in object o] => default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(4, 12));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                VerifyParameterSymbol(comp.GetMember<NamedTypeSymbol>("A").Constructors.Single(c => !c.IsImplicitlyDeclared).Parameters[0], "scoped ref T t", RefKind.Ref, DeclarationScope.RefScoped);
                VerifyParameterSymbol(comp.GetMember<PropertySymbol>("A.this[]").GetMethod.Parameters[0], "scoped in System.Object o", RefKind.In, DeclarationScope.RefScoped);
            }
        }

        [Fact]
        public void ParameterScope_03()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
#pragma warning disable 8321
        static void L1(scoped R x1) { }
        static void L2(scoped ref int x2) { }
        static void L3(scoped in int x3) { }
        static void L4(scoped out int x4) { x4 = 0; }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (7,24): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         static void L1(scoped R x1) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(7, 24),
                // (8,24): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         static void L2(scoped ref int x2) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(8, 24),
                // (9,24): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         static void L3(scoped in int x3) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(9, 24),
                // (10,24): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         static void L4(scoped out int x4) { x4 = 0; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(10, 24));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var decls = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().ToArray();
                var localFunctions = decls.Select(d => model.GetDeclaredSymbol(d).GetSymbol<LocalFunctionSymbol>()).ToArray();

                VerifyParameterSymbol(localFunctions[0].Parameters[0], "scoped R x1", RefKind.None, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(localFunctions[1].Parameters[0], "scoped ref System.Int32 x2", RefKind.Ref, DeclarationScope.RefScoped);
                VerifyParameterSymbol(localFunctions[2].Parameters[0], "scoped in System.Int32 x3", RefKind.In, DeclarationScope.RefScoped);
                VerifyParameterSymbol(localFunctions[3].Parameters[0], "out System.Int32 x4", RefKind.Out, DeclarationScope.RefScoped);
            }
        }

        [Fact]
        public void ParameterScope_04()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
        var f1 = (scoped R x1) => { };
        var f2 = (scoped ref int x2) => { };
        var f3 = (scoped in int x3) => { };
        var f4 = (scoped out int x4) => { x4 = 0; };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (6,19): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         var f1 = (scoped R x1) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(6, 19),
                // (7,19): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         var f2 = (scoped ref int x2) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(7, 19),
                // (8,19): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         var f3 = (scoped in int x3) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(8, 19),
                // (9,19): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         var f4 = (scoped out int x4) => { x4 = 0; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(9, 19));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var delegateTypesAndLambdas = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => getDelegateTypeAndLambda(model, d)).ToArray();

                verifyParameter(delegateTypesAndLambdas[0], 0, "scoped R", "x1", RefKind.None, DeclarationScope.ValueScoped);
                verifyParameter(delegateTypesAndLambdas[1], 0, "scoped ref System.Int32", "x2", RefKind.Ref, DeclarationScope.RefScoped);
                verifyParameter(delegateTypesAndLambdas[2], 0, "scoped in System.Int32", "x3", RefKind.In, DeclarationScope.RefScoped);
                verifyParameter(delegateTypesAndLambdas[3], 0, "out System.Int32", "x4", RefKind.Out, DeclarationScope.RefScoped);
            }

            static void verifyParameter((NamedTypeSymbol, LambdaSymbol) delegateTypeAndLambda, int parameterIndex, string expectedDisplayType, string expectedDisplayName, RefKind expectedRefKind, DeclarationScope expectedScope)
            {
                var (delegateType, lambda) = delegateTypeAndLambda;
                VerifyParameterSymbol(delegateType.DelegateInvokeMethod.Parameters[parameterIndex], expectedDisplayType, expectedRefKind, expectedScope);
                VerifyParameterSymbol(lambda.Parameters[parameterIndex], $"{expectedDisplayType} {expectedDisplayName}", expectedRefKind, expectedScope);
            }

            static (NamedTypeSymbol, LambdaSymbol) getDelegateTypeAndLambda(SemanticModel model, VariableDeclaratorSyntax decl)
            {
                var delegateType = (NamedTypeSymbol)model.GetDeclaredSymbol(decl).GetSymbol<LocalSymbol>().Type;
                var value = decl.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();
                var lambda = model.GetSymbolInfo(value).Symbol.GetSymbol<LambdaSymbol>();
                return (delegateType, lambda);
            }
        }

        [Fact]
        public void ParameterScope_05()
        {
            var source =
@"ref struct R { }
delegate void D1(scoped R r1);
delegate void D2(scoped ref R r2);
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (2,18): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // delegate void D1(scoped R r1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(2, 18),
                // (3,18): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // delegate void D2(scoped ref R r2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(3, 18));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                VerifyParameterSymbol(comp.GetMember<NamedTypeSymbol>("D1").DelegateInvokeMethod.Parameters[0], "scoped R r1", RefKind.None, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(comp.GetMember<NamedTypeSymbol>("D2").DelegateInvokeMethod.Parameters[0], "scoped ref R r2", RefKind.Ref, DeclarationScope.RefScoped);
            }
        }

        [Fact]
        public void ParameterScope_06()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F1(scoped R r1) { }
    static void F2(ref scoped R x, scoped ref int y) { }
    static unsafe void Main()
    {
        delegate*<scoped R, void> f1 = &F1;
        delegate*<ref scoped R, scoped ref int, void> f2 = &F2;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (4,20): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static void F1(scoped R r1) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(4, 20),
                // (5,24): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static void F2(ref scoped R x, scoped ref int y) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(5, 24),
                // (5,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F2(ref scoped R x, scoped ref int y) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(5, 24),
                // (5,36): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static void F2(ref scoped R x, scoped ref int y) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(5, 36),
                // (8,19): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<scoped R, void> f1 = &F1;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(8, 19),
                // (8,40): error CS8986: The 'scoped' modifier of parameter 'r1' doesn't match target 'delegate*<R, void>'.
                //         delegate*<scoped R, void> f1 = &F1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "&F1").WithArguments("r1", "delegate*<R, void>").WithLocation(8, 40),
                // (9,23): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<ref scoped R, scoped ref int, void> f2 = &F2;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(9, 23),
                // (9,33): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<ref scoped R, scoped ref int, void> f2 = &F2;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(9, 33),
                // (9,60): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'delegate*<ref R, ref int, void>'.
                //         delegate*<ref scoped R, scoped ref int, void> f2 = &F2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "&F2").WithArguments("y", "delegate*<ref R, ref int, void>").WithLocation(9, 60));
            verify(comp);

            comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F2(ref scoped R x, scoped ref int y) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(5, 24),
                // (8,19): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<scoped R, void> f1 = &F1;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(8, 19),
                // (8,40): error CS8986: The 'scoped' modifier of parameter 'r1' doesn't match target 'delegate*<R, void>'.
                //         delegate*<scoped R, void> f1 = &F1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "&F1").WithArguments("r1", "delegate*<R, void>").WithLocation(8, 40),
                // (9,23): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<ref scoped R, scoped ref int, void> f2 = &F2;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(9, 23),
                // (9,33): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<ref scoped R, scoped ref int, void> f2 = &F2;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(9, 33),
                // (9,60): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'delegate*<ref R, ref int, void>'.
                //         delegate*<ref scoped R, scoped ref int, void> f2 = &F2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "&F2").WithArguments("y", "delegate*<ref R, ref int, void>").WithLocation(9, 60));
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
                var methods = decls.Select(d => ((FunctionPointerTypeSymbol)model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>().Type).Signature).ToArray();

                VerifyParameterSymbol(methods[0].Parameters[0], "R", RefKind.None, DeclarationScope.Unscoped);
                VerifyParameterSymbol(methods[1].Parameters[0], "ref R", RefKind.Ref, DeclarationScope.Unscoped);
                VerifyParameterSymbol(methods[1].Parameters[1], "ref System.Int32", RefKind.Ref, DeclarationScope.Unscoped);
            }
        }

        [Fact]
        public void ParameterScope_07()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F0(scoped scoped R r) { }
    static void F1(ref scoped scoped R r) { }
    static void F2(scoped ref scoped R r) { }
    static void F3(scoped scoped ref R r) { }
    static void F4(in scoped scoped R r) { }
    static void F5(scoped in scoped R r) { }
    static void F6(scoped scoped in R r) { }
    static void F7(out scoped scoped R r) { r = default; }
    static void F8(scoped out scoped R r) { r = default; }
    static void F9(scoped scoped out R r) { r = default; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,27): error CS1107: A parameter can only have one 'scoped' modifier
                //     static void F0(scoped scoped R r) { }
                Diagnostic(ErrorCode.ERR_DupParamMod, "scoped").WithArguments("scoped").WithLocation(4, 27),
                // (5,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F1(ref scoped scoped R r) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(5, 24),
                // (5,31): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F1(ref scoped scoped R r) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(5, 31),
                // (6,31): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F2(scoped ref scoped R r) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(6, 31),
                // (7,27): error CS1107: A parameter can only have one 'scoped' modifier
                //     static void F3(scoped scoped ref R r) { }
                Diagnostic(ErrorCode.ERR_DupParamMod, "scoped").WithArguments("scoped").WithLocation(7, 27),
                // (8,23): error CS8339:  The parameter modifier 'scoped' cannot follow 'in'
                //     static void F4(in scoped scoped R r) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "in").WithLocation(8, 23),
                // (8,30): error CS8339:  The parameter modifier 'scoped' cannot follow 'in'
                //     static void F4(in scoped scoped R r) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "in").WithLocation(8, 30),
                // (9,30): error CS8339:  The parameter modifier 'scoped' cannot follow 'in'
                //     static void F5(scoped in scoped R r) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "in").WithLocation(9, 30),
                // (10,27): error CS1107: A parameter can only have one 'scoped' modifier
                //     static void F6(scoped scoped in R r) { }
                Diagnostic(ErrorCode.ERR_DupParamMod, "scoped").WithArguments("scoped").WithLocation(10, 27),
                // (11,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'out'
                //     static void F7(out scoped scoped R r) { r = default; }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "out").WithLocation(11, 24),
                // (11,31): error CS8339:  The parameter modifier 'scoped' cannot follow 'out'
                //     static void F7(out scoped scoped R r) { r = default; }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "out").WithLocation(11, 31),
                // (12,31): error CS8339:  The parameter modifier 'scoped' cannot follow 'out'
                //     static void F8(scoped out scoped R r) { r = default; }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "out").WithLocation(12, 31),
                // (13,27): error CS1107: A parameter can only have one 'scoped' modifier
                //     static void F9(scoped scoped out R r) { r = default; }
                Diagnostic(ErrorCode.ERR_DupParamMod, "scoped").WithArguments("scoped").WithLocation(13, 27));

            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F0").Parameters[0], "scoped R r", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F1").Parameters[0], "ref R r", RefKind.Ref, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F2").Parameters[0], "scoped ref R r", RefKind.Ref, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F3").Parameters[0], "scoped ref R r", RefKind.Ref, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F4").Parameters[0], "in R r", RefKind.In, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F5").Parameters[0], "scoped in R r", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F6").Parameters[0], "scoped in R r", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F7").Parameters[0], "out R r", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F8").Parameters[0], "out R r", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F9").Parameters[0], "out R r", RefKind.Out, DeclarationScope.RefScoped);
        }

        [Fact]
        public void ParameterScope_08()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
        var f1 = (scoped scoped R r) => { };
        var f2 = (ref scoped scoped R r) => { };
        var f3 = (scoped scoped ref R r) => { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,26): error CS1107: A parameter can only have one 'scoped' modifier
                //         var f1 = (scoped scoped R r) => { };
                Diagnostic(ErrorCode.ERR_DupParamMod, "scoped").WithArguments("scoped").WithLocation(6, 26),
                // (7,23): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //         var f2 = (ref scoped scoped R r) => { };
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(7, 23),
                // (7,30): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //         var f2 = (ref scoped scoped R r) => { };
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(7, 30),
                // (8,26): error CS1107: A parameter can only have one 'scoped' modifier
                //         var f3 = (scoped scoped ref R r) => { };
                Diagnostic(ErrorCode.ERR_DupParamMod, "scoped").WithArguments("scoped").WithLocation(8, 26));
        }

        [Fact]
        public void ParameterScope_09()
        {
            var source =
@"ref struct scoped { }
class Program
{
    static void F0(scoped s) { }
    static void F1(scoped scoped s) { }
    static void F2(ref scoped s) { }
    static void F3(ref scoped scoped s) { }
    static void F4(scoped ref scoped s) { }
    static void F5(in scoped s) { }
    static void F6(in scoped scoped s) { }
    static void F7(scoped in scoped s) { }
    static void F8(out scoped s) { s = default; }
    static void F9(out scoped scoped s) { s = default; }
    static void FA(scoped out scoped s) { s = default; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,12): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // ref struct scoped { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 12),
                // (7,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F3(ref scoped scoped s) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(7, 24),
                // (10,23): error CS8339:  The parameter modifier 'scoped' cannot follow 'in'
                //     static void F6(in scoped scoped s) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "in").WithLocation(10, 23),
                // (13,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'out'
                //     static void F9(out scoped scoped s) { s = default; }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "out").WithLocation(13, 24));

            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F0").Parameters[0], "scoped s", RefKind.None, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F1").Parameters[0], "scoped scoped s", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F2").Parameters[0], "ref scoped s", RefKind.Ref, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F3").Parameters[0], "ref scoped s", RefKind.Ref, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F4").Parameters[0], "scoped ref scoped s", RefKind.Ref, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F5").Parameters[0], "in scoped s", RefKind.In, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F6").Parameters[0], "in scoped s", RefKind.In, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F7").Parameters[0], "scoped in scoped s", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F8").Parameters[0], "out scoped s", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F9").Parameters[0], "out scoped s", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.FA").Parameters[0], "out scoped s", RefKind.Out, DeclarationScope.RefScoped);
        }

        [Fact]
        public void ParameterScope_10()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.LifetimeAnnotationAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor(bool isRefScoped, bool isValueScoped) cil managed { ret }
}
.class public sealed R extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public int32& modreq(int32) F
}
.class public A
{
  .method public static void F(valuetype R r)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor(bool, bool) = ( 01 00 00 00 00 00 ) // LifetimeAnnotationAttribute(isRefScoped: false, isValueScoped: false)
    ret
  }
}
";
            var ref0 = CompileIL(source0);

            var source1 =
@"class Program
{
    static void Main()
    {
        var r = new R();
        A.F(r);
    }
}";
            var comp = CreateCompilation(source1, references: new[] { ref0 });
            comp.VerifyDiagnostics();

            var method = comp.GetMember<PEMethodSymbol>("A.F");
            VerifyParameterSymbol(method.Parameters[0], "R r", RefKind.None, DeclarationScope.Unscoped);
        }

        [WorkItem(62080, "https://github.com/dotnet/roslyn/issues/62080")]
        [Fact]
        public void ParameterScope_11()
        {
            var source =
@"ref struct R { }
delegate R D1(R r);
delegate R D2(scoped R r);
class Program
{
    static void Main()
    {
        D1 d1 = r1 => r1;
        D2 d2 = r2 => r2;
    }
}";
            var comp = CreateCompilation(source);
            // https://github.com/dotnet/roslyn/issues/62080: Lambda parameter r2 should be inferred as 'scoped R' rather than 'R'.
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS8986: The 'scoped' modifier of parameter 'r2' doesn't match target 'D2'.
                //         D2 d2 = r2 => r2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "r2 => r2").WithArguments("r2", "D2").WithLocation(9, 17));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var lambdas = tree.GetRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Select(e => model.GetSymbolInfo(e).Symbol.GetSymbol<LambdaSymbol>()).ToArray();

            VerifyParameterSymbol(lambdas[0].Parameters[0], "R r1", RefKind.None, DeclarationScope.Unscoped);
            VerifyParameterSymbol(lambdas[1].Parameters[0], "R r2", RefKind.None, DeclarationScope.Unscoped);
        }

        [Fact]
        public void ParameterScope_12()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.LifetimeAnnotationAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor(bool isRefScoped, bool isValueScoped) cil managed { ret }
}
.class public A
{
  .method public static void F1([out] int32& i)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor(bool, bool) = ( 01 00 01 00 00 00 ) // LifetimeAnnotationAttribute(isRefScoped: true, isValueScoped: false)
    ldnull
    throw
  }
  .method public static void F2([out] int32& i)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor(bool, bool) = ( 01 00 00 00 00 00 ) // LifetimeAnnotationAttribute(isRefScoped: false, isValueScoped: false)
    ldnull
    throw
  }
}
";
            var ref0 = CompileIL(source0);

            var source1 =
@"class Program
{
    static void Main()
    {
        int i;
        A.F1(out i);
        A.F2(out i);
    }
}";
            var comp = CreateCompilation(source1, references: new[] { ref0 });
            comp.VerifyDiagnostics();

            VerifyParameterSymbol(comp.GetMember<PEMethodSymbol>("A.F1").Parameters[0], "out System.Int32 i", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<PEMethodSymbol>("A.F2").Parameters[0], "out System.Int32 i", RefKind.Out, DeclarationScope.Unscoped);
        }

        [Fact]
        public void ThisScope()
        {
            var source =
@"class C
{
    public C() { }
    void F1() { }
}
struct S1
{
    public S1() { }
    void F1() { }
    readonly void F2() { }
}
ref struct R1
{
    public R1() { }
    void F1() { }
    readonly void F2() { }
}
readonly struct S2
{
    public S2() { }
    void F1() { }
    readonly void F2() { }
}
readonly ref struct R2
{
    public R2() { }
    void F1() { }
    readonly void F2() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("C..ctor").ThisParameter, "C this", RefKind.None, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("C.F1").ThisParameter, "C this", RefKind.None, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("S1..ctor").ThisParameter, "out S1 this", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("S1.F1").ThisParameter, "scoped ref S1 this", RefKind.Ref, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("S1.F2").ThisParameter, "scoped in S1 this", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("R1..ctor").ThisParameter, "out R1 this", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("R1.F1").ThisParameter, "scoped ref R1 this", RefKind.Ref, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("R1.F2").ThisParameter, "scoped in R1 this", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("S2..ctor").ThisParameter, "out S2 this", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("S2.F1").ThisParameter, "scoped in S2 this", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("S2.F2").ThisParameter, "scoped in S2 this", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("R2..ctor").ThisParameter, "out R2 this", RefKind.Out, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("R2.F1").ThisParameter, "scoped in R2 this", RefKind.In, DeclarationScope.RefScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("R2.F2").ThisParameter, "scoped in R2 this", RefKind.In, DeclarationScope.RefScoped);
        }

        [Fact]
        public void ExtensionThisScope()
        {
            var source =
@"ref struct R<T> { }
static class Extensions
{
    static void F0(this R<object> r) { }
    static void F1(this scoped R<object> r) { }
    static void F2<T>(scoped this R<T> r) { }
    static void F3<T>(this scoped ref T t) where T : struct { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Extensions.F0").Parameters[0], "R<System.Object> r", RefKind.None, DeclarationScope.Unscoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Extensions.F1").Parameters[0], "scoped R<System.Object> r", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Extensions.F2").Parameters[0], "scoped R<T> r", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Extensions.F3").Parameters[0], "scoped ref T t", RefKind.Ref, DeclarationScope.RefScoped);
        }

        [Fact]
        public void ParamsScope()
        {
            var source =
@"class Program
{
    static void F1(scoped params object[] args) { }
    static void F2(params scoped object[] args) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F1(scoped params object[] args) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped params object[] args").WithLocation(3, 20),
                // (4,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F2(params scoped object[] args) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "params scoped object[] args").WithLocation(4, 20));

            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F1").Parameters[0], "scoped params System.Object[] args", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(comp.GetMember<MethodSymbol>("Program.F2").Parameters[0], "scoped params System.Object[] args", RefKind.None, DeclarationScope.ValueScoped);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReturnTypeScope(LanguageVersion langVersion)
        {
            var source =
@"ref struct R { }
class Program
{
    static scoped R F1<T>() => throw null;
    static scoped ref R F2<T>() => throw null;
    static void Main()
    {
#pragma warning disable 8321
        static scoped R L1<T>() => throw null;
        static scoped ref readonly R L2<T>() => throw null;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            comp.VerifyDiagnostics(
                // (4,21): error CS0106: The modifier 'scoped' is not valid for this item
                //     static scoped R F1<T>() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F1").WithArguments("scoped").WithLocation(4, 21),
                // (5,25): error CS0106: The modifier 'scoped' is not valid for this item
                //     static scoped ref R F2<T>() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F2").WithArguments("scoped").WithLocation(5, 25),
                // (9,16): error CS0106: The modifier 'scoped' is not valid for this item
                //         static scoped R L1<T>() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "scoped").WithArguments("scoped").WithLocation(9, 16),
                // (10,16): error CS0106: The modifier 'scoped' is not valid for this item
                //         static scoped ref readonly R L2<T>() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "scoped").WithArguments("scoped").WithLocation(10, 16));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DelegateReturnTypeScope(LanguageVersion langVersion)
        {
            var source =
@"ref struct R { }
delegate ref scoped R D();
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            comp.VerifyEmitDiagnostics(
                // (2,14): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(2, 14),
                // (2,21): error CS0101: The namespace '<global namespace>' already contains a definition for 'R'
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "R").WithArguments("R", "<global namespace>").WithLocation(2, 21),
                // (2,23): error CS1003: Syntax error, '(' expected
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_SyntaxError, "D").WithArguments("(").WithLocation(2, 23),
                // (2,23): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D").WithLocation(2, 23),
                // (2,24): error CS1001: Identifier expected
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(2, 24),
                // (2,24): error CS1003: Syntax error, ',' expected
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(2, 24),
                // (2,25): error CS8124: Tuple must contain at least two elements.
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(2, 25),
                // (2,26): error CS1001: Identifier expected
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 26),
                // (2,26): error CS1026: ) expected
                // delegate ref scoped R D();
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(2, 26));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void TypeScopeModifier_01(LanguageVersion langVersion)
        {
            var source =
@"scoped struct A { }
scoped ref struct B { }
scoped readonly ref struct C { }
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            comp.VerifyDiagnostics(
                // (1,15): error CS0106: The modifier 'scoped' is not valid for this item
                // scoped struct A { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "A").WithArguments("scoped").WithLocation(1, 15),
                // (2,19): error CS0106: The modifier 'scoped' is not valid for this item
                // scoped ref struct B { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "B").WithArguments("scoped").WithLocation(2, 19),
                // (3,28): error CS0106: The modifier 'scoped' is not valid for this item
                // scoped readonly ref struct C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("scoped").WithLocation(3, 28));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void TypeScopeModifier_02(LanguageVersion langVersion)
        {
            var source =
@"scoped record A { }
scoped readonly record struct B;
readonly scoped record struct C();
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            comp.VerifyDiagnostics(
                // (1,15): error CS0106: The modifier 'scoped' is not valid for this item
                // scoped record A { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "A").WithArguments("scoped").WithLocation(1, 15),
                // (2,31): error CS0106: The modifier 'scoped' is not valid for this item
                // scoped readonly record struct B;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "B").WithArguments("scoped").WithLocation(2, 31),
                // (3,31): error CS0106: The modifier 'scoped' is not valid for this item
                // readonly scoped record struct C();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("scoped").WithLocation(3, 31));
        }

        [Fact]
        public void FieldTypeScope()
        {
            var source =
@"#pragma warning disable 169
ref struct R1 { }
ref struct R2
{
    scoped R1 F1;
    scoped ref int F3;
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,15): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped R1 F1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F1").WithArguments("scoped").WithLocation(5, 15),
                // (6,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     scoped ref int F3;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref int").WithArguments("ref fields", "11.0").WithLocation(6, 12),
                // (6,20): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped ref int F3;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F3").WithArguments("scoped").WithLocation(6, 20));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,15): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped R1 F1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F1").WithArguments("scoped").WithLocation(5, 15),
                // (6,20): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped ref int F3;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F3").WithArguments("scoped").WithLocation(6, 20));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void PropertyTypeScope(LanguageVersion langVersion)
        {
            var source =
@"ref struct R1 { }
ref struct R2
{
    scoped R1 P1 { get; }
    scoped R1 P2 { get; init; }
    scoped R1 P3 { set { } }
    scoped ref int P5 => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            comp.VerifyDiagnostics(
                // (4,15): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped R1 P1 { get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P1").WithArguments("scoped").WithLocation(4, 15),
                // (5,15): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped R1 P2 { get; init; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P2").WithArguments("scoped").WithLocation(5, 15),
                // (6,15): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped R1 P3 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P3").WithArguments("scoped").WithLocation(6, 15),
                // (7,20): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped ref int P5 => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P5").WithArguments("scoped").WithLocation(7, 20));
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                verifyValueParameter(comp.GetMember<PropertySymbol>("R2.P2"), "R1 value", RefKind.None, DeclarationScope.Unscoped);
                verifyValueParameter(comp.GetMember<PropertySymbol>("R2.P3"), "R1 value", RefKind.None, DeclarationScope.Unscoped);
            }

            static void verifyValueParameter(PropertySymbol property, string expectedDisplayString, RefKind expectedRefKind, DeclarationScope expectedScope)
            {
                Assert.Equal(expectedRefKind, property.RefKind);
                VerifyParameterSymbol(property.SetMethod.Parameters[0], expectedDisplayString, expectedRefKind, expectedScope);
            }
        }

        [Fact]
        public void SubstitutedParameter()
        {
            var source =
@"ref struct R<T> { }
class A<T>
{
    public static void F(scoped R<T> x, scoped in T y) { }
}
class B : A<int>
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            var method = (MethodSymbol)comp.GetMember<NamedTypeSymbol>("B").BaseTypeNoUseSiteDiagnostics.GetMember("F");
            VerifyParameterSymbol(method.Parameters[0], "scoped R<System.Int32> x", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(method.Parameters[1], "scoped in System.Int32 y", RefKind.In, DeclarationScope.RefScoped);
        }

        [Fact]
        public void RetargetingParameter()
        {
            var sourceA =
@"public ref struct R { }
public class A
{
    public static void F(scoped R x, scoped in int y) { }
}
";
            var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Mscorlib40);
            var refA = comp.ToMetadataReference();

            var sourceB =
@"class B
{
    static void Main()
    {
        A.F(default, 0);
    }
}";
            comp = CreateCompilation(sourceB, new[] { refA }, targetFramework: TargetFramework.Mscorlib45);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            var method = model.GetSymbolInfo(expr).Symbol.GetSymbol<RetargetingMethodSymbol>();

            VerifyParameterSymbol(method.Parameters[0], "scoped R x", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(method.Parameters[1], "scoped in System.Int32 y", RefKind.In, DeclarationScope.RefScoped);
        }

        private static readonly SymbolDisplayFormat displayFormatWithScoped = SymbolDisplayFormat.TestFormat.
            WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeScoped).
            AddLocalOptions(SymbolDisplayLocalOptions.IncludeRef);

        private static void VerifyParameterSymbol(ParameterSymbol parameter, string expectedDisplayString, RefKind expectedRefKind, DeclarationScope expectedScope)
        {
            Assert.Equal(expectedRefKind, parameter.RefKind);
            Assert.Equal(expectedScope, parameter.Scope);
            Assert.Equal(expectedDisplayString, parameter.ToDisplayString(displayFormatWithScoped));

            var attribute = parameter.GetAttributes().FirstOrDefault(a => a.GetTargetAttributeSignatureIndex(parameter, AttributeDescription.LifetimeAnnotationAttribute) != -1);
            Assert.Null(attribute);

            VerifyParameterSymbol(parameter.GetPublicSymbol(), expectedDisplayString, expectedRefKind, expectedScope);
        }

        private static void VerifyParameterSymbol(IParameterSymbol parameter, string expectedDisplayString, RefKind expectedRefKind, DeclarationScope expectedScope)
        {
            Assert.Equal(expectedRefKind, parameter.RefKind);
            // https://github.com/dotnet/roslyn/issues/61647: Use public API.
            //Assert.Equal(expectedScope == DeclarationScope.RefScoped, parameter.IsRefScoped);
            //Assert.Equal(expectedScope == DeclarationScope.ValueScoped, parameter.IsValueScoped);
            Assert.Equal(expectedDisplayString, parameter.ToDisplayString(displayFormatWithScoped));
        }

        [Fact]
        public void LocalScope_01()
        {
            var source =
@"#pragma warning disable 219
ref struct R { }
class Program
{
    static void F(ref R r)
    {
        scoped R r1 = default;
        scoped ref R r2 = ref r;
        scoped ref readonly R r5 = ref r;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         scoped R r1 = default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(7, 9),
                // (8,9): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         scoped ref R r2 = ref r;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(8, 9),
                // (9,9): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         scoped ref readonly R r5 = ref r;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(9, 9));
            verify(comp, useUpdatedEscapeRules: false);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp, useUpdatedEscapeRules: true);

            static void verify(CSharpCompilation comp, bool useUpdatedEscapeRules)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
                var locals = decls.Select(d => model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>()).ToArray();

                VerifyLocalSymbol(locals[0], "scoped R r1", RefKind.None, DeclarationScope.ValueScoped);
                VerifyLocalSymbol(locals[1], "scoped ref R r2", RefKind.Ref, DeclarationScope.RefScoped);
                VerifyLocalSymbol(locals[2], "scoped ref readonly R r5", RefKind.RefReadOnly, DeclarationScope.RefScoped);
            }
        }

        [Fact]
        public void LocalScope_02()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
        scoped scoped R x = default;
        scoped scoped ref R z = ref x;
    }
}";
            var comp = CreateCompilation(source);
            // Duplicate scoped modifiers result are parse errors rather than binding errors.
            comp.VerifyDiagnostics(
                // (6,16): error CS1031: Type expected
                //         scoped scoped R x = default;
                Diagnostic(ErrorCode.ERR_TypeExpected, "scoped").WithLocation(6, 16),
                // (7,9): error CS0118: 'scoped' is a variable but is used like a type
                //         scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "scoped").WithArguments("scoped", "variable", "type").WithLocation(7, 9),
                // (7,16): warning CS0168: The variable 'scoped' is declared but never used
                //         scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "scoped").WithArguments("scoped").WithLocation(7, 16),
                // (7,23): error CS1002: ; expected
                //         scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(7, 23));
        }

        [Fact]
        public void LocalScope_03()
        {
            var source =
@"scoped scoped R x = default;
scoped scoped ref R z = ref x;
ref struct R { }
";
            var comp = CreateCompilation(source);
            // Duplicate scoped modifiers result are parse errors rather than binding errors.
            comp.VerifyDiagnostics(
                // (1,8): error CS1031: Type expected
                // scoped scoped R x = default;
                Diagnostic(ErrorCode.ERR_TypeExpected, "scoped").WithLocation(1, 8),
                // (1,17): warning CS0219: The variable 'x' is assigned but its value is never used
                // scoped scoped R x = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(1, 17),
                // (2,1): error CS0118: 'scoped' is a variable but is used like a type
                // scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "scoped").WithArguments("scoped", "variable", "type").WithLocation(2, 1),
                // (2,8): warning CS0168: The variable 'scoped' is declared but never used
                // scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "scoped").WithArguments("scoped").WithLocation(2, 8),
                // (2,15): error CS1003: Syntax error, ',' expected
                // scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 15));
        }

        [Fact]
        public void LocalScope_04()
        {
            var source =
@"scoped s1 = default;
ref scoped s2 = ref s1;
scoped scoped s3 = default;
scoped ref scoped s4 = ref s1;
ref struct scoped { }
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (3,1): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // scoped scoped s3 = default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(3, 1),
                // (3,15): warning CS0219: The variable 's3' is assigned but its value is never used
                // scoped scoped s3 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s3").WithArguments("s3").WithLocation(3, 15),
                // (4,1): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // scoped ref scoped s4 = ref s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(4, 1),
                // (5,12): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // ref struct scoped { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(5, 12));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,15): warning CS0219: The variable 's3' is assigned but its value is never used
                // scoped scoped s3 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s3").WithArguments("s3").WithLocation(3, 15),
                // (5,12): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // ref struct scoped { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(5, 12));
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
                var locals = decls.Select(d => model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>()).ToArray();

                VerifyLocalSymbol(locals[0], "scoped s1", RefKind.None, DeclarationScope.Unscoped);
                VerifyLocalSymbol(locals[1], "ref scoped s2", RefKind.Ref, DeclarationScope.Unscoped);
                VerifyLocalSymbol(locals[2], "scoped scoped s3", RefKind.None, DeclarationScope.ValueScoped);
                VerifyLocalSymbol(locals[3], "scoped ref scoped s4", RefKind.Ref, DeclarationScope.RefScoped);
            }
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void LocalScope_05(LanguageVersion langVersion)
        {
            var source =
@"bool scoped;
scoped = true;
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            comp.VerifyDiagnostics(
                // (1,6): warning CS0219: The variable 'scoped' is assigned but its value is never used
                // bool scoped;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "scoped").WithArguments("scoped").WithLocation(1, 6));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var locals = decls.Select(d => model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>()).ToArray();

            VerifyLocalSymbol(locals[0], "System.Boolean scoped", RefKind.None, DeclarationScope.Unscoped);
        }

        [Fact]
        public void LocalScope_06()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static void M(R<int> r0)
    {
        scoped var r1 = new R<int>();
        scoped ref var r3 = ref r0;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,20): warning CS0219: The variable 'r1' is assigned but its value is never used
                //         scoped var r1 = new R<int>();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "r1").WithArguments("r1").WithLocation(6, 20)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var locals = decls.Select(d => model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>()).ToArray();

            VerifyLocalSymbol(locals[0], "scoped R<System.Int32> r1", RefKind.None, DeclarationScope.ValueScoped);
            VerifyLocalSymbol(locals[1], "scoped ref R<System.Int32> r3", RefKind.Ref, DeclarationScope.RefScoped);
        }

        [Fact]
        public void LocalScopeAndInitializer_01()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Values(R r1, scoped R r2)
    {
        R r11 = r1;
        R r12 = r2;
        scoped R r21 = r1;
        scoped R r22 = r2;
    }
    static void Refs(ref R r1, scoped ref R r2)
    {
        ref R r31 = ref r1;
        ref R r32 = ref r2;
        scoped ref R r41 = ref r1;
        scoped ref R r42 = ref r2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var locals = decls.Select(d => model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>()).ToArray();

            VerifyLocalSymbol(locals[0], "R r11", RefKind.None, DeclarationScope.Unscoped);
            VerifyLocalSymbol(locals[1], "R r12", RefKind.None, DeclarationScope.Unscoped);
            VerifyLocalSymbol(locals[2], "scoped R r21", RefKind.None, DeclarationScope.ValueScoped);
            VerifyLocalSymbol(locals[3], "scoped R r22", RefKind.None, DeclarationScope.ValueScoped);

            VerifyLocalSymbol(locals[4], "ref R r31", RefKind.Ref, DeclarationScope.Unscoped);
            VerifyLocalSymbol(locals[5], "ref R r32", RefKind.Ref, DeclarationScope.Unscoped);
            VerifyLocalSymbol(locals[6], "scoped ref R r41", RefKind.Ref, DeclarationScope.RefScoped);
            VerifyLocalSymbol(locals[7], "scoped ref R r42", RefKind.Ref, DeclarationScope.RefScoped);
        }

        private static void VerifyLocalSymbol(LocalSymbol local, string expectedDisplayString, RefKind expectedRefKind, DeclarationScope expectedScope)
        {
            Assert.Equal(expectedRefKind, local.RefKind);
            Assert.Equal(expectedScope, local.Scope);
            Assert.Equal(expectedDisplayString, local.ToDisplayString(displayFormatWithScoped));

            VerifyLocalSymbol(local.GetPublicSymbol(), expectedDisplayString, expectedRefKind, expectedScope);
        }

        private static void VerifyLocalSymbol(ILocalSymbol local, string expectedDisplayString, RefKind expectedRefKind, DeclarationScope expectedScope)
        {
            Assert.Equal(expectedRefKind, local.RefKind);
            // https://github.com/dotnet/roslyn/issues/61647: Use public API.
            //Assert.Equal(expectedScope == DeclarationScope.RefScoped, local.IsRefScoped);
            //Assert.Equal(expectedScope == DeclarationScope.ValueScoped, local.IsValueScoped);
            Assert.Equal(expectedDisplayString, local.ToDisplayString(displayFormatWithScoped));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void ParameterScope_EmbeddedMethod()
        {
            var sourceA =
@"using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""DB204C34-AE89-49C6-9174-09F72E7F7F10"")]
[ComImport()]
[Guid(""933FEEE7-2728-4F87-A802-953F3CF1B1E9"")]
public interface I
{
    void M(scoped ref int i);
}
";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference(embedInteropTypes: true);

            var sourceB =
@"class C : I
{
    public void M(scoped ref int i) { }
}
class Program
{
    static void Main()
    {
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA },
                symbolValidator: module =>
                {
                    var method = module.GlobalNamespace.GetMember<PEMethodSymbol>("I.M");
                    // Attribute is not included for the parameter from the embedded method.
                    VerifyParameterSymbol(method.Parameters[0], "ref System.Int32 i", RefKind.Ref, DeclarationScope.Unscoped);
                });
        }

        [Fact]
        public void Conversions_01()
        {
            var source =
@"ref struct R { }
class Program
{
    static R Implicit1(scoped R r) => r;
    static R Implicit2(ref R r) => r;
    static R Implicit4(scoped ref R r) => r;
    static R Explicit1(scoped R r) => (R)r;
    static R Explicit2(ref R r) => (R)r;
    static R Explicit4(scoped ref R r) => (R)r;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,39): error CS8352: Cannot use variable 'R' in this context because it may expose referenced variables outside of their declaration scope
                //     static R Implicit1(scoped R r) => r;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r").WithArguments("R").WithLocation(4, 39),
                // (7,39): error CS8352: Cannot use variable 'R' in this context because it may expose referenced variables outside of their declaration scope
                //     static R Explicit1(scoped R r) => (R)r;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "(R)r").WithArguments("R").WithLocation(7, 39));
        }

        [Fact]
        public void DelegateConversions_01()
        {
            var source =
@"ref struct R { }
delegate R D1(R x, R y);
delegate R D2(R x, scoped R y);
delegate ref R D5(ref R x, scoped ref R y);
class Program
{
    static void Implicit()
    {
        D1 d1 = (R x, scoped R y) => x;
        D2 d2 = (R x, R y) => x;
        D5 d5 = (ref R x, ref R y) => ref x;
    }
    static void Explicit()
    {
        var d1 = (D1)((R x, scoped R y) => x);
        var d2 = (D2)((R x, R y) => x);
        var d5 = (D5)((ref R x, ref R y) => ref x);
    }
    static void New()
    {
        var d1 = new D1((R x, scoped R y) => x);
        var d2 = new D2((R x, R y) => x);
        var d5 = new D5((ref R x, ref R y) => ref x);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         D1 d1 = (R x, scoped R y) => x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(R x, scoped R y) => x").WithArguments("y", "D1").WithLocation(9, 17),
                // (10,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         D2 d2 = (R x, R y) => x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(R x, R y) => x").WithArguments("y", "D2").WithLocation(10, 17),
                // (11,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         D5 d5 = (ref R x, ref R y) => ref x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(ref R x, ref R y) => ref x").WithArguments("y", "D5").WithLocation(11, 17),
                // (15,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         var d1 = (D1)((R x, scoped R y) => x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D1)((R x, scoped R y) => x)").WithArguments("y", "D1").WithLocation(15, 18),
                // (16,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         var d2 = (D2)((R x, R y) => x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D2)((R x, R y) => x)").WithArguments("y", "D2").WithLocation(16, 18),
                // (17,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         var d5 = (D5)((ref R x, ref R y) => ref x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D5)((ref R x, ref R y) => ref x)").WithArguments("y", "D5").WithLocation(17, 18),
                // (21,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         var d1 = new D1((R x, scoped R y) => x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(R x, scoped R y) => x").WithArguments("y", "D1").WithLocation(21, 25),
                // (22,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         var d2 = new D2((R x, R y) => x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(R x, R y) => x").WithArguments("y", "D2").WithLocation(22, 25),
                // (23,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         var d5 = new D5((ref R x, ref R y) => ref x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(ref R x, ref R y) => ref x").WithArguments("y", "D5").WithLocation(23, 25));
        }

        [Fact]
        public void DelegateConversions_02()
        {
            var source =
@"ref struct R { }
delegate R D1(R x, R y);
delegate R D2(R x, scoped R y);
delegate ref R D5(ref R x, scoped ref R y);
class Program
{
    static R M1(R x, R y) => x;
    static R M2(R x, scoped R y) => x;
    static ref R M3(ref R x, ref R y) => ref x;
    static ref R M5(ref R x, scoped ref R y) => ref x;
    static void Implicit()
    {
        D1 dA = M2;
        D2 d2 = M1;
        D5 d5 = M3;
    }
    static void Explicit()
    {
        var d1 = (D1)M2;
        var d2 = (D2)M1;
        var d5 = (D5)M3;
    }
    static void New()
    {
        var d1 = new D1(M2);
        var d2 = new D2(M1);
        var d5 = new D5(M3);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         D1 dA = M2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M2").WithArguments("y", "D1").WithLocation(13, 17),
                // (14,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         D2 d2 = M1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M1").WithArguments("y", "D2").WithLocation(14, 17),
                // (15,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         D5 d5 = M3;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M3").WithArguments("y", "D5").WithLocation(15, 17),
                // (19,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         var d1 = (D1)M2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D1)M2").WithArguments("y", "D1").WithLocation(19, 18),
                // (20,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         var d2 = (D2)M1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D2)M1").WithArguments("y", "D2").WithLocation(20, 18),
                // (21,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         var d5 = (D5)M3;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D5)M3").WithArguments("y", "D5").WithLocation(21, 18),
                // (25,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         var d1 = new D1(M2);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M2").WithArguments("y", "D1").WithLocation(25, 25),
                // (26,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         var d2 = new D2(M1);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M1").WithArguments("y", "D2").WithLocation(26, 25),
                // (27,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         var d5 = new D5(M3);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M3").WithArguments("y", "D5").WithLocation(27, 25));
        }

        [Fact]
        public void DelegateConversions_03()
        {
            var source =
@"delegate ref int D1(ref int x, ref int y);
delegate ref int D2(scoped ref int x, ref int y);
delegate D1 D1R();
delegate D2 D2R();
class Program
{
    static void Implicit()
    {
        D1R d1 = () => (scoped ref int x, ref int y) => ref y;
        D2R d2 = () => (ref int x, ref int y) => ref x;
    }
    static void Explicit()
    {
        var d1 = (D1R)(() => (scoped ref int x, ref int y) => ref y);
        var d2 = (D2R)(() => (ref int x, ref int y) => ref x);
    }
    static void New()
    {
        var d1 = new D1R(() => (scoped ref int x, ref int y) => ref y);
        var d2 = new D2R(() => (ref int x, ref int y) => ref x);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,24): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D1'.
                //         D1R d1 = () => (scoped ref int x, ref int y) => ref y;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(scoped ref int x, ref int y) => ref y").WithArguments("x", "D1").WithLocation(9, 24),
                // (10,24): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D2'.
                //         D2R d2 = () => (ref int x, ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(ref int x, ref int y) => ref x").WithArguments("x", "D2").WithLocation(10, 24),
                // (14,30): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D1'.
                //         var d1 = (D1R)(() => (scoped ref int x, ref int y) => ref y);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(scoped ref int x, ref int y) => ref y").WithArguments("x", "D1").WithLocation(14, 30),
                // (15,30): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D2'.
                //         var d2 = (D2R)(() => (ref int x, ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(ref int x, ref int y) => ref x").WithArguments("x", "D2").WithLocation(15, 30),
                // (19,32): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D1'.
                //         var d1 = new D1R(() => (scoped ref int x, ref int y) => ref y);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(scoped ref int x, ref int y) => ref y").WithArguments("x", "D1").WithLocation(19, 32),
                // (20,32): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D2'.
                //         var d2 = new D2R(() => (ref int x, ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(ref int x, ref int y) => ref x").WithArguments("x", "D2").WithLocation(20, 32));
        }

        [Fact]
        public void DelegateConversions_04()
        {
            var source =
@"delegate ref int D1(ref int x, ref int y);
delegate ref int D2(scoped ref int x, ref int y);
delegate D1 D1R();
delegate D2 D2R();
class Program
{
    static ref int M1(ref int x, ref int y) => ref x;
    static ref int M2(scoped ref int x, ref int y) => ref y;
    static void Implicit()
    {
        D1R d1 = () => M2;
        D2R d2 = () => M1;
    }
    static void Explicit()
    {
        var d1 = (D1R)M2;
        var d2 = (D2R)M1;
    }
    static void New()
    {
        var d1 = new D1R(M2);
        var d2 = new D2R(M1);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,24): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D1'.
                //         D1R d1 = () => M2;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M2").WithArguments("x", "D1").WithLocation(11, 24),
                // (12,24): error CS8986: The 'scoped' modifier of parameter 'x' doesn't match target 'D2'.
                //         D2R d2 = () => M1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "M1").WithArguments("x", "D2").WithLocation(12, 24),
                // (16,18): error CS0123: No overload for 'M2' matches delegate 'D1R'
                //         var d1 = (D1R)M2;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "(D1R)M2").WithArguments("M2", "D1R").WithLocation(16, 18),
                // (17,18): error CS0123: No overload for 'M1' matches delegate 'D2R'
                //         var d2 = (D2R)M1;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "(D2R)M1").WithArguments("M1", "D2R").WithLocation(17, 18),
                // (21,18): error CS0123: No overload for 'M2' matches delegate 'D1R'
                //         var d1 = new D1R(M2);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new D1R(M2)").WithArguments("M2", "D1R").WithLocation(21, 18),
                // (22,18): error CS0123: No overload for 'M1' matches delegate 'D2R'
                //         var d2 = new D2R(M1);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new D2R(M1)").WithArguments("M1", "D2R").WithLocation(22, 18));
        }

        [Fact]
        public void DelegateConversions_05()
        {
            var source =
@"ref struct R { }
delegate R D1(R x, R y);
delegate R D2(R x, scoped R y);
delegate ref R D5(ref R x, scoped ref R y);
class Program
{
    static void Implicit()
    {
        D1 d1 = delegate(R x, scoped R y) { return x; };
        D2 d2 = delegate(R x, R y) { return x; };
        D5 d5 = delegate(ref R x, ref R y) { return ref x; };
    }
    static void Explicit()
    {
        var d1 = (D1)(delegate(R x, scoped R y) { return x; });
        var d2 = (D2)(delegate(R x, R y) { return x; });
        var d5 = (D5)(delegate(ref R x, ref R y) { return ref x; });
    }
    static void New()
    {
        var d1 = new D1(delegate(R x, scoped R y) { return x; });
        var d2 = new D2(delegate(R x, R y) { return x; });
        var d5 = new D5(delegate(ref R x, ref R y) { return ref x; });
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         D1 d1 = delegate(R x, scoped R y) { return x; };
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "delegate(R x, scoped R y) { return x; }").WithArguments("y", "D1").WithLocation(9, 17),
                // (10,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         D2 d2 = delegate(R x, R y) { return x; };
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "delegate(R x, R y) { return x; }").WithArguments("y", "D2").WithLocation(10, 17),
                // (11,17): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         D5 d5 = delegate(ref R x, ref R y) { return ref x; };
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "delegate(ref R x, ref R y) { return ref x; }").WithArguments("y", "D5").WithLocation(11, 17),
                // (15,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         var d1 = (D1)(delegate(R x, scoped R y) { return x; });
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D1)(delegate(R x, scoped R y) { return x; })").WithArguments("y", "D1").WithLocation(15, 18),
                // (16,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         var d2 = (D2)(delegate(R x, R y) { return x; });
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D2)(delegate(R x, R y) { return x; })").WithArguments("y", "D2").WithLocation(16, 18),
                // (17,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         var d5 = (D5)(delegate(ref R x, ref R y) { return ref x; });
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(D5)(delegate(ref R x, ref R y) { return ref x; })").WithArguments("y", "D5").WithLocation(17, 18),
                // (21,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D1'.
                //         var d1 = new D1(delegate(R x, scoped R y) { return x; });
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "delegate(R x, scoped R y) { return x; }").WithArguments("y", "D1").WithLocation(21, 25),
                // (22,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D2'.
                //         var d2 = new D2(delegate(R x, R y) { return x; });
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "delegate(R x, R y) { return x; }").WithArguments("y", "D2").WithLocation(22, 25),
                // (23,25): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'D5'.
                //         var d5 = new D5(delegate(ref R x, ref R y) { return ref x; });
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "delegate(ref R x, ref R y) { return ref x; }").WithArguments("y", "D5").WithLocation(23, 25));
        }

        [Fact]
        public void DelegateConversions_06()
        {
            var source =
@"using System.Linq.Expressions;
ref struct R { }
delegate R D1(R x, R y);
delegate R D2(R x, scoped R y);
delegate ref int D3(ref int x, ref int y);
delegate ref int D4(ref int x, scoped ref int y);
class Program
{
    static void Implicit()
    {
        Expression<D1> e1 = (R x, scoped R y) => x;
        Expression<D2> e2 = (R x, R y) => x;
        Expression<D3> e3 = (ref int x, scoped ref int y) => ref x;
        Expression<D4> e4 = (ref int x, ref int y) => ref x;
    }
    static void Explicit()
    {
        var e1 = (Expression<D1>)((R x, scoped R y) => x);
        var e2 = (Expression<D2>)((R x, R y) => x);
        var e3 = (Expression<D3>)((ref int x, scoped ref int y) => ref x);
        var e4 = (Expression<D4>)((ref int x, ref int y) => ref x);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,29): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D1>'.
                //         Expression<D1> e1 = (R x, scoped R y) => x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(R x, scoped R y) => x").WithArguments("y", "System.Linq.Expressions.Expression<D1>").WithLocation(11, 29),
                // (11,32): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         Expression<D1> e1 = (R x, scoped R y) => x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(11, 32),
                // (11,44): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         Expression<D1> e1 = (R x, scoped R y) => x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "y").WithArguments("R").WithLocation(11, 44),
                // (11,50): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         Expression<D1> e1 = (R x, scoped R y) => x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(11, 50),
                // (12,29): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D2>'.
                //         Expression<D2> e2 = (R x, R y) => x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(R x, R y) => x").WithArguments("y", "System.Linq.Expressions.Expression<D2>").WithLocation(12, 29),
                // (12,32): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         Expression<D2> e2 = (R x, R y) => x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(12, 32),
                // (12,37): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         Expression<D2> e2 = (R x, R y) => x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "y").WithArguments("R").WithLocation(12, 37),
                // (12,43): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         Expression<D2> e2 = (R x, R y) => x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(12, 43),
                // (13,29): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D3>'.
                //         Expression<D3> e3 = (ref int x, scoped ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(ref int x, scoped ref int y) => ref x").WithArguments("y", "System.Linq.Expressions.Expression<D3>").WithLocation(13, 29),
                // (13,29): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<D3> e3 = (ref int x, scoped ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(ref int x, scoped ref int y) => ref x").WithLocation(13, 29),
                // (13,38): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         Expression<D3> e3 = (ref int x, scoped ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "x").WithLocation(13, 38),
                // (13,56): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         Expression<D3> e3 = (ref int x, scoped ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "y").WithLocation(13, 56),
                // (14,29): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D4>'.
                //         Expression<D4> e4 = (ref int x, ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(ref int x, ref int y) => ref x").WithArguments("y", "System.Linq.Expressions.Expression<D4>").WithLocation(14, 29),
                // (14,29): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<D4> e4 = (ref int x, ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(ref int x, ref int y) => ref x").WithLocation(14, 29),
                // (14,38): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         Expression<D4> e4 = (ref int x, ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "x").WithLocation(14, 38),
                // (14,49): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         Expression<D4> e4 = (ref int x, ref int y) => ref x;
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "y").WithLocation(14, 49),
                // (18,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D1>'.
                //         var e1 = (Expression<D1>)((R x, scoped R y) => x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(Expression<D1>)((R x, scoped R y) => x)").WithArguments("y", "System.Linq.Expressions.Expression<D1>").WithLocation(18, 18),
                // (18,38): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         var e1 = (Expression<D1>)((R x, scoped R y) => x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(18, 38),
                // (18,50): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         var e1 = (Expression<D1>)((R x, scoped R y) => x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "y").WithArguments("R").WithLocation(18, 50),
                // (18,56): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         var e1 = (Expression<D1>)((R x, scoped R y) => x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(18, 56),
                // (19,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D2>'.
                //         var e2 = (Expression<D2>)((R x, R y) => x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(Expression<D2>)((R x, R y) => x)").WithArguments("y", "System.Linq.Expressions.Expression<D2>").WithLocation(19, 18),
                // (19,38): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         var e2 = (Expression<D2>)((R x, R y) => x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(19, 38),
                // (19,43): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         var e2 = (Expression<D2>)((R x, R y) => x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "y").WithArguments("R").WithLocation(19, 43),
                // (19,49): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'R'.
                //         var e2 = (Expression<D2>)((R x, R y) => x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("R").WithLocation(19, 49),
                // (20,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D3>'.
                //         var e3 = (Expression<D3>)((ref int x, scoped ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(Expression<D3>)((ref int x, scoped ref int y) => ref x)").WithArguments("y", "System.Linq.Expressions.Expression<D3>").WithLocation(20, 18),
                // (20,35): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         var e3 = (Expression<D3>)((ref int x, scoped ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(ref int x, scoped ref int y) => ref x").WithLocation(20, 35),
                // (20,44): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         var e3 = (Expression<D3>)((ref int x, scoped ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "x").WithLocation(20, 44),
                // (20,62): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         var e3 = (Expression<D3>)((ref int x, scoped ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "y").WithLocation(20, 62),
                // (21,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'Expression<D4>'.
                //         var e4 = (Expression<D4>)((ref int x, ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(Expression<D4>)((ref int x, ref int y) => ref x)").WithArguments("y", "System.Linq.Expressions.Expression<D4>").WithLocation(21, 18),
                // (21,35): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         var e4 = (Expression<D4>)((ref int x, ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(ref int x, ref int y) => ref x").WithLocation(21, 35),
                // (21,44): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         var e4 = (Expression<D4>)((ref int x, ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "x").WithLocation(21, 44),
                // (21,55): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         var e4 = (Expression<D4>)((ref int x, ref int y) => ref x);
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "y").WithLocation(21, 55));
        }

        [Fact]
        public void DelegateConversions_Out()
        {
            var source =
@"ref struct R { }
delegate void D1(out int x, scoped out int y);
class Program
{
    static void Implicit()
    {
        D1 d1 = (scoped out int x, out int y) => { x = 0; y = 0; };
    }
    static void Explicit()
    {
        var d1 = (D1)((scoped out int x, out int y) => { x = 0; y = 0; });
    }
    static void New()
    {
        var d1 = new D1((scoped out int x, out int y) => { x = 0; y = 0; });
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void FunctionPointerConversions()
        {
            var source =
@"ref struct R { }
unsafe class Program
{
    static R F1(R x, scoped R y) => x;
    static ref readonly int F3(in int x, scoped in int y) => ref x;
    static void Implicit()
    {
        delegate*<R, R, R> d1 = &F1;
        delegate*<in int, in int, ref readonly int> d3 = &F3;
    }
    static void Explicit()
    {
        var d1 = (delegate*<R, R, R>)&F1;
        var d3 = (delegate*<in int, in int, ref readonly int>)&F3;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (8,33): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'delegate*<R, R, R>'.
                //         delegate*<R, R, R> d1 = &F1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "&F1").WithArguments("y", "delegate*<R, R, R>").WithLocation(8, 33),
                // (9,58): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'delegate*<in int, in int, ref readonly int>'.
                //         delegate*<in int, in int, ref readonly int> d3 = &F3;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "&F3").WithArguments("y", "delegate*<in int, in int, ref readonly int>").WithLocation(9, 58),
                // (13,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'delegate*<R, R, R>'.
                //         var d1 = (delegate*<R, R, R>)&F1;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(delegate*<R, R, R>)&F1").WithArguments("y", "delegate*<R, R, R>").WithLocation(13, 18),
                // (14,18): error CS8986: The 'scoped' modifier of parameter 'y' doesn't match target 'delegate*<in int, in int, ref readonly int>'.
                //         var d3 = (delegate*<in int, in int, ref readonly int>)&F3;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(delegate*<in int, in int, ref readonly int>)&F3").WithArguments("y", "delegate*<in int, in int, ref readonly int>").WithLocation(14, 18));
        }

        [Fact]
        public void FunctionPointerConversions_Out()
        {
            var source =
@"unsafe class Program
{
    static void F(out int x, scoped out int y) { x = 0; y = 0; }
    static void Implicit()
    {
        delegate*<out int, out int, void> d = &F;
    }
    static void Explicit()
    {
        var d = (delegate*<out int, out int, void>)&F;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DuplicateMethodSignatures()
        {
            var source =
@"ref struct R<T> { }
class C<T>
{
    static void M1(R<T> r) { }
    void M2(scoped R<T> r) { }
    object this[R<T> r] => null;
    static void M1(scoped R<T> r) { } // 1
    void M2(R<T> r) { } // 2 
    object this[scoped R<T> r] => null; // 3
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,17): error CS0111: Type 'C<T>' already defines a member called 'M1' with the same parameter types
                //     static void M1(scoped R<T> r) { } // 1
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M1").WithArguments("M1", "C<T>").WithLocation(7, 17),
                // (8,10): error CS0111: Type 'C<T>' already defines a member called 'M2' with the same parameter types
                //     void M2(R<T> r) { } // 2 
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "C<T>").WithLocation(8, 10),
                // (9,12): error CS0111: Type 'C<T>' already defines a member called 'this' with the same parameter types
                //     object this[scoped R<T> r] => null; // 3
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C<T>").WithLocation(9, 12));
        }

        [Fact]
        public void Overloads()
        {
            var source =
@"ref struct R<T> { }
class C
{
    static void M1(R<int> r) { }
    static void M2(scoped R<int> r) { }
    static void M3(ref R<int> r) { }
    static void M4(ref scoped R<int> r) { } // 1
    static void M5(scoped ref R<int> r) { }
    static void M1(scoped R<int> r) { } // 2
    static void M2(R<int> r) { } // 3
    static void M3(ref scoped R<int> r) { } // 4
    static void M4(scoped ref R<int> r) { } // 5
    static void M5(ref R<int> r) { } // 6
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void M4(ref scoped R<int> r) { } // 1
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(7, 24),
                // (9,17): error CS0111: Type 'C' already defines a member called 'M1' with the same parameter types
                //     static void M1(scoped R<int> r) { } // 2
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M1").WithArguments("M1", "C").WithLocation(9, 17),
                // (10,17): error CS0111: Type 'C' already defines a member called 'M2' with the same parameter types
                //     static void M2(R<int> r) { } // 3
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "C").WithLocation(10, 17),
                // (11,17): error CS0111: Type 'C' already defines a member called 'M3' with the same parameter types
                //     static void M3(ref scoped R<int> r) { } // 4
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M3").WithArguments("M3", "C").WithLocation(11, 17),
                // (11,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void M3(ref scoped R<int> r) { } // 4
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(11, 24),
                // (12,17): error CS0111: Type 'C' already defines a member called 'M4' with the same parameter types
                //     static void M4(scoped ref R<int> r) { } // 5
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M4").WithArguments("M4", "C").WithLocation(12, 17),
                // (13,17): error CS0111: Type 'C' already defines a member called 'M5' with the same parameter types
                //     static void M5(ref R<int> r) { } // 6
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M5").WithArguments("M5", "C").WithLocation(13, 17));
        }

        [Fact]
        public void PartialMethods()
        {
            var sourceA =
@"ref struct R<T> { }
partial class C
{
    static partial void M1(R<int> r);
    static partial void M2(scoped R<int> r);
    static partial void M3(ref R<int> r);
    static partial void M5(scoped ref R<int> r);
}";
            var sourceB1 =
@"partial class C
{
    static partial void M1(R<int> r) { }
    static partial void M2(scoped R<int> r) { }
    static partial void M3(ref R<int> r) { }
    static partial void M5(scoped ref R<int> r) { }
}";
            var sourceB2 =
@"partial class C
{
    static partial void M1(scoped R<int> r) { } // 1
    static partial void M2(R<int> r) { } // 2
    static partial void M5(ref R<int> r) { } // 3
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB1 });
            comp.VerifyEmitDiagnostics();

            var expectedDiagnostics = new[]
            {
                // (3,25): error CS8988: The 'scoped' modifier of parameter 'r' doesn't match partial method declaration.
                //     static partial void M1(scoped R<int> r) { } // 1
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "M1").WithArguments("r").WithLocation(3, 25),
                // (4,25): error CS8988: The 'scoped' modifier of parameter 'r' doesn't match partial method declaration.
                //     static partial void M2(R<int> r) { } // 2
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "M2").WithArguments("r").WithLocation(4, 25),
                // (5,25): error CS8988: The 'scoped' modifier of parameter 'r' doesn't match partial method declaration.
                //     static partial void M5(ref R<int> r) { } // 3
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "M5").WithArguments("r").WithLocation(5, 25)
            };

            comp = CreateCompilation(new[] { sourceA, sourceB2 });
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { sourceB2, sourceA });
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void PartialMethods_Out()
        {
            var source =
@"ref struct R { }
partial class C
{
    private partial void F1(out int i);
    private partial void F2(scoped out int i);
    private partial void F1(scoped out int i) { i = 0; }
    private partial void F2(out int i) { i = 0; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [CombinatorialData]
        [Theory]
        public void Hiding(bool useCompilationReference)
        {
            var sourceA =
@"public ref struct R<T> { }
public class A<T>
{
    public void M1(R<T> r) { }
    public void M2(scoped R<T> r) { }
    public object this[R<T> r] { get { return null; } set { } }
    public object this[int x, scoped R<T> y] => null;
}";
            var comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class B1 : A<int>
{
    public new void M1(scoped R<int> r) { }
    public new void M2(R<int> r) { }
    public new object this[scoped R<int> r] { get { return null; } set { } }
    public new object this[int x, R<int> y] => null;
}
class B2 : A<string>
{
    public void M1(scoped R<string> r) { } // 1
    public void M2(R<string> r) { } // 2
    public object this[scoped R<string> r] { get { return null; } set { } } // 3
    public object this[int x, R<string> y] => null; // 4
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (10,17): warning CS0108: 'B2.M1(R<string>)' hides inherited member 'A<string>.M1(R<string>)'. Use the new keyword if hiding was intended.
                //     public void M1(scoped R<string> r) { } // 1
                Diagnostic(ErrorCode.WRN_NewRequired, "M1").WithArguments("B2.M1(R<string>)", "A<string>.M1(R<string>)").WithLocation(10, 17),
                // (11,17): warning CS0108: 'B2.M2(R<string>)' hides inherited member 'A<string>.M2(R<string>)'. Use the new keyword if hiding was intended.
                //     public void M2(R<string> r) { } // 2
                Diagnostic(ErrorCode.WRN_NewRequired, "M2").WithArguments("B2.M2(R<string>)", "A<string>.M2(R<string>)").WithLocation(11, 17),
                // (12,19): warning CS0108: 'B2.this[R<string>]' hides inherited member 'A<string>.this[R<string>]'. Use the new keyword if hiding was intended.
                //     public object this[scoped R<string> r] { get { return null; } set { } } // 3
                Diagnostic(ErrorCode.WRN_NewRequired, "this").WithArguments("B2.this[R<string>]", "A<string>.this[R<string>]").WithLocation(12, 19),
                // (13,19): warning CS0108: 'B2.this[int, R<string>]' hides inherited member 'A<string>.this[int, R<string>]'. Use the new keyword if hiding was intended.
                //     public object this[int x, R<string> y] => null; // 4
                Diagnostic(ErrorCode.WRN_NewRequired, "this").WithArguments("B2.this[int, R<string>]", "A<string>.this[int, R<string>]").WithLocation(13, 19));
        }

        [CombinatorialData]
        [Theory]
        public void Overrides(bool useCompilationReference)
        {
            var sourceA =
@"public ref struct R<T> { }
public abstract class A<T>
{
    public abstract R<T> F1(R<T> r);
    public abstract R<T> F2(scoped R<T> r);
    public abstract R<T> F4(scoped ref R<T> r);
    public abstract object this[R<T> r] { get; set; }
    public abstract object this[int x, scoped R<T> y] { get; }
}";
            var comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class B1 : A<int>
{
    public override R<int> F1(R<int> r) => default;
    public override R<int> F2(scoped R<int> r) => default;
    public override R<int> F4(scoped ref R<int> r) => default;
    public override object this[R<int> r] { get { return null; } set { } }
    public override object this[int x, scoped R<int> y] => null;
}
class B2 : A<string>
{
    public override R<string> F1(scoped R<string> r) => default; // 1
    public override R<string> F2(R<string> r) => default; // 2
    public override R<string> F4(ref scoped R<string> r) => default; // 3
    public override object this[scoped R<string> r] { get { return null; } set { } } // 4
    public override object this[int x, R<string> y] => null; // 5
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (11,31): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public override R<string> F1(scoped R<string> r) => default; // 1
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F1").WithArguments("r").WithLocation(11, 31),
                // (12,31): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public override R<string> F2(R<string> r) => default; // 2
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F2").WithArguments("r").WithLocation(12, 31),
                // (13,31): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public override R<string> F4(ref scoped R<string> r) => default; // 3
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F4").WithArguments("r").WithLocation(13, 31),
                // (13,38): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     public override R<string> F4(ref scoped R<string> r) => default; // 3
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(13, 38),
                // (14,76): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public override object this[scoped R<string> r] { get { return null; } set { } } // 4
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "set").WithArguments("r").WithLocation(14, 76),
                // (15,56): error CS8987: The 'scoped' modifier of parameter 'y' doesn't match overridden or implemented member.
                //     public override object this[int x, R<string> y] => null; // 5
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "null").WithArguments("y").WithLocation(15, 56));
        }

        [CombinatorialData]
        [Theory]
        public void InterfaceImplementations(bool useCompilationReference)
        {
            var sourceA =
@"public ref struct R<T> { }
public interface I<T>
{
    R<T> F1(R<T> r);
    R<T> F2(scoped R<T> r);
    R<T> F4(scoped ref R<T> r);
    object this[R<T> r] { get; set; }
    object this[int x, scoped R<T> y] { get; }
}";
            var comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB1 =
@"class C1 : I<int>
{
    public R<int> F1(R<int> r) => default;
    public R<int> F2(scoped R<int> r) => default;
    public R<int> F4(scoped ref R<int> r) => default;
    public object this[R<int> r] { get { return null; } set { } }
    public object this[int x, scoped R<int> y] => null;
}
class C2 : I<string>
{
    public R<string> F1(scoped R<string> r) => default; // 1
    public R<string> F2(R<string> r) => default; // 2
    public R<string> F4(scoped ref R<string> r) => default;
    public object this[scoped R<string> r] { get { return null; } set { } } // 3
    public object this[int x, R<string> y] => null; // 4
}";
            comp = CreateCompilation(sourceB1, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (11,22): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public R<string> F1(scoped R<string> r) => default; // 1
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F1").WithArguments("r").WithLocation(11, 22),
                // (12,22): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public R<string> F2(R<string> r) => default; // 2
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F2").WithArguments("r").WithLocation(12, 22),
                // (14,67): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public object this[scoped R<string> r] { get { return null; } set { } } // 3
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "set").WithArguments("r").WithLocation(14, 67),
                // (15,47): error CS8987: The 'scoped' modifier of parameter 'y' doesn't match overridden or implemented member.
                //     public object this[int x, R<string> y] => null; // 4
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "null").WithArguments("y").WithLocation(15, 47));

            var sourceB2 =
@"class C3 : I<int>
{
    R<int> I<int>.F1(R<int> r) => default;
    R<int> I<int>.F2(scoped R<int> r) => default;
    R<int> I<int>.F4(scoped ref R<int> r) => default;
    object I<int>.this[R<int> r] { get { return null; } set { } }
    object I<int>.this[int x, scoped R<int> y] => null;
}
class C4 : I<string>
{
    R<string> I<string>.F1(scoped R<string> r) => default; // 1
    R<string> I<string>.F2(R<string> r) => default; // 2
    R<string> I<string>.F4(scoped ref R<string> r) => default;
    object I<string>.this[scoped R<string> r] { get { return null; } set { } } // 3
    object I<string>.this[int x, R<string> y] => null; // 4
}";
            comp = CreateCompilation(sourceB2, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (11,25): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     R<string> I<string>.F1(scoped R<string> r) => default; // 1
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F1").WithArguments("r").WithLocation(11, 25),
                // (12,25): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     R<string> I<string>.F2(R<string> r) => default; // 2
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F2").WithArguments("r").WithLocation(12, 25),
                // (14,70): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     object I<string>.this[scoped R<string> r] { get { return null; } set { } } // 3
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "set").WithArguments("r").WithLocation(14, 70),
                // (15,50): error CS8987: The 'scoped' modifier of parameter 'y' doesn't match overridden or implemented member.
                //     object I<string>.this[int x, R<string> y] => null; // 4
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "null").WithArguments("y").WithLocation(15, 50));
        }

        [CombinatorialData]
        [Theory]
        public void OverridesAndInterfaceImplementations_Out_01(bool useCompilationReference)
        {
            var sourceA =
@"public abstract class A<T>
{
    public abstract void F1(out T t);
}
public interface I<T>
{
    void F2(out T t);
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class B1 : A<int>, I<int>
{
    public override void F1(scoped out int i) { i = 0; }
    public void F2(scoped out int i) { i = 0; }
}
class B2 : I<string>
{
    void I<string>.F2(scoped out string s) { s = null; }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void OverridesAndInterfaceImplementations_Out_02()
        {
            var source =
@"ref struct R { }
abstract class A
{
    public abstract void F1(out int i);
    public abstract void F2(scoped out int i);
}
class B : A
{
    public override void F1(scoped out int i) { i = 0; }
    public override void F2(out int i) { i = 0; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void OverridesAndInterfaceImplementations_Out_03()
        {
            var source =
@"ref struct R { }
interface I
{
    void F1(out int i);
    void F2(scoped out int i);
}
class C1 : I
{
    public void F1(out int i) { i = 0; }
    public void F2(scoped out int i) { i = 0; }
}
class C2 : I
{
    void I.F1(out int i) { i = 0; }
    void I.F2(scoped out int i) { i = 0; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Overrides_Example()
        {
            var source =
@"ref struct R
{
    public ref int F;
    public R(ref int i) { F = ref i; }
}
abstract class A
{
    public abstract R F1(scoped R r);
    public abstract R F2(R r);
}
class B : A
{
    public override R F1(R r) => r;
    public override R F2(scoped R r) => default;
}
class Program
{
    static R F1(A a)
    {
        int i = 0;
        return a.F1(new R(ref i));
    }
    static R F2(B b)
    {
        int i = 0;
        return b.F2(new R(ref i)); // unsafe
    }
    static void Main()
    {
        R r1 = F1(new B()); // unsafe
        R r2 = F2(new B());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,23): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public override R F1(R r) => r;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F1").WithArguments("r").WithLocation(13, 23),
                // (14,23): error CS8987: The 'scoped' modifier of parameter 'r' doesn't match overridden or implemented member.
                //     public override R F2(scoped R r) => default;
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation, "F2").WithArguments("r").WithLocation(14, 23));
        }

        [Fact]
        public void Delegates_Example()
        {
            var source =
@"ref struct R
{
    public ref int F;
    public R(ref int i) { F = ref i; }
}
delegate R D1(scoped R x);
delegate R D2(R x);
class Program
{
    static R F1(D1 d1)
    {
        int i = 0;
        return d1(new R(ref i));
    }
    static R F2(D2 d2)
    {
        int i = 0;
        return d2(new R(ref i));
    }
    static void Main()
    {
        R r1 = F1((R x) => x); // unsafe
        R r2 = F2((scoped R x) => default);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (18,16): error CS8347: Cannot use a result of 'D2.Invoke(R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return d2(new R(ref i));
                Diagnostic(ErrorCode.ERR_EscapeCall, "d2(new R(ref i))").WithArguments("D2.Invoke(R)", "x").WithLocation(18, 16),
                // (18,19): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return d2(new R(ref i));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref i)").WithArguments("R.R(ref int)", "i").WithLocation(18, 19),
                // (18,29): error CS8168: Cannot return local 'i' by reference because it is not a ref local
                //         return d2(new R(ref i));
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(18, 29),
                // (22,19): error CS8989: The 'scoped' modifier of parameter 'x' doesn't match target 'D1'.
                //         R r1 = F1((R x) => x); // unsafe
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(R x) => x").WithArguments("x", "D1").WithLocation(22, 19),
                // (23,19): error CS8989: The 'scoped' modifier of parameter 'x' doesn't match target 'D2'.
                //         R r2 = F2((scoped R x) => default);
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(scoped R x) => default").WithArguments("x", "D2").WithLocation(23, 19));
        }

        [Fact]
        public void BestCommonType_01()
        {
            var source =
@"ref struct R { }
class Program
{
    static R F1(bool b, R x, scoped R y) => b ? x : y;
    static R F2(bool b, R x, scoped R y) => b ? y : x;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,53): error CS8352: Cannot use variable 'R' in this context because it may expose referenced variables outside of their declaration scope
                //     static R F1(bool b, R x, scoped R y) => b ? x : y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("R").WithLocation(4, 53),
                // (5,49): error CS8352: Cannot use variable 'R' in this context because it may expose referenced variables outside of their declaration scope
                //     static R F2(bool b, R x, scoped R y) => b ? y : x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("R").WithLocation(5, 49));
        }

        [Fact]
        public void BestCommonType_03()
        {
            var source =
@"class Program
{
    static ref int F1(bool b, scoped ref int x, ref int y) => ref b ? ref x : ref y;
    static ref int F2(bool b, scoped ref int x, ref int y) => ref b ? ref y : ref x;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,75): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     static ref int F1(bool b, scoped ref int x, ref int y) => ref b ? ref x : ref y;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(3, 75),
                // (4,83): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     static ref int F2(bool b, scoped ref int x, ref int y) => ref b ? ref y : ref x;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(4, 83));
        }

        [Fact]
        public void BestCommonType_04()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
        var f1 = new[] { (R r) => { }, (scoped R r) => { } }[0]; // 1
        var f2 = new[] { (scoped R r) => { }, (scoped R r) => { } }[0];
        var f3 = new[] { (ref R r) => { }, (scoped ref R r) => { } }[0]; // 2 
        var f4 = new[] { (scoped ref R r) => { }, (scoped ref R r) => { } }[0];
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,18): error CS0826: No best type found for implicitly-typed array
                //         var f1 = new[] { (R r) => { }, (scoped R r) => { } }[0]; // 1
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (R r) => { }, (scoped R r) => { } }").WithLocation(6, 18),
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var f3 = new[] { (ref R r) => { }, (scoped ref R r) => { } }[0]; // 2 
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (ref R r) => { }, (scoped ref R r) => { } }").WithLocation(8, 18));
        }

        [Fact]
        public void InferredDelegateTypes_01()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F1(R x1, scoped R y1)
    {
        var f = (R x, scoped R y) => x;
        R z;
        z = f(x1, y1);
        z = f(y1, x1); // 1
    }
    static void F3(ref int x3, scoped ref int y3)
    {
        var f = (ref int x, scoped ref int y) => ref x;
        int z;
        z = f(ref x3, ref y3);
        z = f(ref y3, ref x3);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,13): error CS8347: Cannot use a result of '<anonymous delegate>.Invoke(R, R)' in this context because it may expose variables referenced by parameter '0' outside of their declaration scope
                //         z = f(y1, x1); // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "f(y1, x1)").WithArguments("<anonymous delegate>.Invoke(R, R)", "0").WithLocation(9, 13),
                // (9,15): error CS8352: Cannot use variable 'R' in this context because it may expose referenced variables outside of their declaration scope
                //         z = f(y1, x1); // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y1").WithArguments("R").WithLocation(9, 15));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.Text == "f").ToArray();
            var delegateInvokeMethods = decls.Select(d => ((ILocalSymbol)model.GetDeclaredSymbol(d)).Type.GetSymbol<NamedTypeSymbol>().DelegateInvokeMethod).ToArray();

            VerifyParameterSymbol(delegateInvokeMethods[0].Parameters[1], "scoped R", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(delegateInvokeMethods[1].Parameters[1], "scoped ref System.Int32", RefKind.Ref, DeclarationScope.RefScoped);
        }

        [Fact]
        public void InferredDelegateTypes_02()
        {
            var source =
@"ref struct R { }
static class E1
{
    public static void F1(this object o, R r) { }
    public static void F2(this object o, ref R r) { }
}
static class E2
{
    public static void F1(this object o, scoped R r) { }
    public static void F2(this object o, scoped ref R r) { }
}
class Program
{
    static void Main()
    {
        object o = new object();
        var d1 = o.F1;
        var d2 = o.F2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (17,18): error CS0121: The call is ambiguous between the following methods or properties: 'E1.F1(object, R)' and 'E2.F1(object, R)'
                //         var d1 = o.F1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "o.F1").WithArguments("E1.F1(object, R)", "E2.F1(object, R)").WithLocation(17, 18),
                // (18,18): error CS0121: The call is ambiguous between the following methods or properties: 'E1.F2(object, ref R)' and 'E2.F2(object, ref R)'
                //         var d2 = o.F2;
                Diagnostic(ErrorCode.ERR_AmbigCall, "o.F2").WithArguments("E1.F2(object, ref R)", "E2.F2(object, ref R)").WithLocation(18, 18));
        }

        [Fact]
        public void ScopedRefAndRefStructOnly_01()
        {
            var source =
@"struct S { }
class Program
{
    static void F1(scoped S s) { }
    static void F2(ref scoped S s) { }
    static void F3(scoped ref S s) { }
    static void F4(scoped ref scoped S s) { }
    static void F5(ref scoped int i) { }
    static void F6(in scoped  int i) { }
    static void F7(out scoped int i) { i = 0; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,20): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F1(scoped S s) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped S s").WithLocation(4, 20),
                // (5,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F2(ref scoped S s) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(5, 24),
                // (7,31): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F4(scoped ref scoped S s) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(7, 31),
                // (8,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     static void F5(ref scoped int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(8, 24),
                // (9,23): error CS8339:  The parameter modifier 'scoped' cannot follow 'in'
                //     static void F6(in scoped  int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "in").WithLocation(9, 23),
                // (10,24): error CS8339:  The parameter modifier 'scoped' cannot follow 'out'
                //     static void F7(out scoped int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "out").WithLocation(10, 24));
        }

        [Fact]
        public void ScopedRefAndRefStructOnly_02()
        {
            var source =
@"struct S { }
interface I
{
    void F1<T>(scoped T t);
    void F2<T>(scoped T t) where T : class;
    void F3<T>(scoped T t) where T : struct;
    void F4<T>(scoped T t) where T : unmanaged;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,16): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //     void F1<T>(scoped T t);
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped T t").WithLocation(4, 16),
                // (5,16): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //     void F2<T>(scoped T t) where T : class;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped T t").WithLocation(5, 16),
                // (6,16): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //     void F3<T>(scoped T t) where T : struct;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped T t").WithLocation(6, 16),
                // (7,16): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //     void F4<T>(scoped T t) where T : unmanaged;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped T t").WithLocation(7, 16));
        }

        [Fact]
        public void ScopedRefAndRefStructOnly_03()
        {
            var source =
@"enum E { }
class Program
{
    static void Main()
    {
        var f = (scoped ref E x, scoped E y) => { };
#pragma warning disable 8321
        static void L(scoped ref E x, scoped E y) { }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,43): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //         var f = (scoped ref E x, scoped E y) => { };
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "y").WithLocation(6, 43),
                // (8,39): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //         static void L(scoped ref E x, scoped E y) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped E y").WithLocation(8, 39));
        }

        [Fact]
        public void ScopedRefAndRefStructOnly_04()
        {
            var source =
@"delegate void D(scoped C c);
class C
{
    static unsafe void Main()
    {
        delegate*<scoped C, int> d = default;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (1,17): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                // delegate void D(scoped C c);
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped C c").WithLocation(1, 17),
                // (6,19): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<scoped C, int> d = default;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(6, 19));
        }

        [Theory]
        [InlineData("ref         ")]
        [InlineData("ref readonly")]
        public void ScopedRefAndRefStructOnly_05(string refModifier)
        {
            var source =
$@"struct S {{ }}
class Program
{{
    static void F(S s)
    {{
        scoped {refModifier} S s2 = ref s;
    }}
}}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("ref         ")]
        [InlineData("ref readonly")]
        public void ScopedRefAndRefStructOnly_05_RefScoped(string refModifier)
        {
            var source =
$@"struct S {{ }}
class Program
{{
    static void F(S s)
    {{
        {refModifier} scoped S s1 = ref s;
        scoped {refModifier} scoped S s3 = ref s;
    }}
}}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,22): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                //         ref readonly scoped S s1 = ref s;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(6, 22),
                // (6,29): error CS8174: A declaration of a by-reference variable must have an initializer
                //         ref readonly scoped S s1 = ref s;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "S").WithLocation(6, 29),
                // (6,29): warning CS0168: The variable 'S' is declared but never used
                //         ref readonly scoped S s1 = ref s;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "S").WithArguments("S").WithLocation(6, 29),
                // (6,31): error CS1002: ; expected
                //         ref readonly scoped S s1 = ref s;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "s1").WithLocation(6, 31),
                // (6,31): error CS0103: The name 's1' does not exist in the current context
                //         ref readonly scoped S s1 = ref s;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "s1").WithArguments("s1").WithLocation(6, 31),
                // (7,29): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                //         scoped ref readonly scoped S s3 = ref s;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(7, 29),
                // (7,36): error CS0128: A local variable or function named 'S' is already defined in this scope
                //         scoped ref readonly scoped S s3 = ref s;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "S").WithArguments("S").WithLocation(7, 36),
                // (7,36): error CS8174: A declaration of a by-reference variable must have an initializer
                //         scoped ref readonly scoped S s3 = ref s;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "S").WithLocation(7, 36),
                // (7,36): warning CS0168: The variable 'S' is declared but never used
                //         scoped ref readonly scoped S s3 = ref s;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "S").WithArguments("S").WithLocation(7, 36),
                // (7,38): error CS1002: ; expected
                //         scoped ref readonly scoped S s3 = ref s;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "s3").WithLocation(7, 38),
                // (7,38): error CS0103: The name 's3' does not exist in the current context
                //         scoped ref readonly scoped S s3 = ref s;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "s3").WithArguments("s3").WithLocation(7, 38));
        }

        [Fact]
        public void ScopedRefAndRefStructOnly_06()
        {
            var source =
@"ref struct R<T> { }
struct S<T> { }
class Program
{
    static void Main()
    {
        scoped var x1 = new R<int>();
        scoped ref var x3 = ref x1; // 1
        scoped var y1 = new S<int>(); // 2
        scoped ref var y3 = ref y1;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,33): error CS8352: Cannot use variable 'x1' in this context because it may expose referenced variables outside of their declaration scope
                //         scoped ref var x3 = ref x1; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x1").WithArguments("x1").WithLocation(8, 33),
                // (9,16): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //         scoped var y1 = new S<int>(); // 2
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "var").WithLocation(9, 16));
        }

        [Fact]
        public void ScopedRefAndRefStructOnly_07()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static void F1(scoped Unknown x, scoped R<Unknown> y)
    {
        var f = (ref scoped Unknown u) => { };
        scoped R<Unknown> z = y;
        scoped var v = F2();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,27): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F1(scoped Unknown x, scoped R<Unknown> y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(4, 27),
                // (4,47): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //     static void F1(scoped Unknown x, scoped R<Unknown> y)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(4, 47),
                // (6,22): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //         var f = (ref scoped Unknown u) => { };
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(6, 22),
                // (6,29): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //         var f = (ref scoped Unknown u) => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(6, 29),
                // (7,18): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //         scoped R<Unknown> z = y;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(7, 18),
                // (8,24): error CS0103: The name 'F2' does not exist in the current context
                //         scoped var v = F2();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "F2").WithArguments("F2").WithLocation(8, 24));
        }

        [Fact]
        public void Local_SequencePoints()
        {
            var source =
@"using System;
ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static void Main()
    {
        int x = 1;
        scoped R<int> y = new R<int>(ref x);
        ref R<int> z = ref y;
        z.F = 2;
        Console.WriteLine(x);
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugExe, verify: Verification.Skipped);
            verifier.VerifyIL("Program.Main",
                source: source,
                sequencePoints: "Program.Main",
                expectedIL:
@"{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (int V_0, //x
                R<int> V_1, //y
                R<int>& V_2) //z
  // sequence point: {
  IL_0000:  nop
  // sequence point: int x = 1;
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  // sequence point: scoped R<int> y = new R<int>(ref x);
  IL_0003:  ldloca.s   V_0
  IL_0005:  newobj     ""R<int>..ctor(ref int)""
  IL_000a:  stloc.1
  // sequence point: ref R<int> z = ref y;
  IL_000b:  ldloca.s   V_1
  IL_000d:  stloc.2
  // sequence point: z.F = 2;
  IL_000e:  ldloc.2
  IL_000f:  ldfld      ""ref int R<int>.F""
  IL_0014:  ldc.i4.2
  IL_0015:  stind.i4
  // sequence point: Console.WriteLine(x);
  IL_0016:  ldloc.0
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  nop
  // sequence point: }
  IL_001d:  ret
}");
        }

        [Fact]
        public void SafeToEscape_01()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static R<int> F0(R<int> r0) => r0;
    static R<int> F1(scoped R<int> r1) => r1; // 1
    static R<int> F2(ref R<int> r2) => r2;
    static R<int> F3(scoped ref R<int> r3) => r3;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,43): error CS8352: Cannot use variable 'R<int>' in this context because it may expose referenced variables outside of their declaration scope
                //     static R<int> F1(scoped R<int> r1) => r1; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("R<int>").WithLocation(5, 43));
        }

        [Fact]
        public void RefSafeToEscape_01()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static ref R<int> F0(R<int> r0) => ref r0; // 1
    static ref R<int> F1(scoped R<int> r1) => ref r1; // 2
    static ref R<int> F2(ref R<int> r2) => ref r2;
    static ref R<int> F3(scoped ref R<int> r3) => ref r3; // 3
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,44): error CS8166: Cannot return a parameter by reference 'r0' because it is not a ref parameter
                //     static ref R<int> F0(R<int> r0) => ref r0; // 1
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "r0").WithArguments("r0").WithLocation(4, 44),
                // (5,51): error CS8166: Cannot return a parameter by reference 'r1' because it is not a ref parameter
                //     static ref R<int> F1(scoped R<int> r1) => ref r1; // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "r1").WithArguments("r1").WithLocation(5, 51),
                // (7,55): error CS8166: Cannot return a parameter by reference 'r3' because it is not a ref parameter
                //     static ref R<int> F3(scoped ref R<int> r3) => ref r3; // 3
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "r3").WithArguments("r3").WithLocation(7, 55));
        }

        [Fact]
        public void SafeToEscape_02()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static R<int> F0(R<int> r0)
    {
        R<int> l0 = r0;
        return l0;
    }
    static R<int> F1(scoped R<int> r1)
    {
        R<int> l1 = r1;
        return l1; // 1
    }
    static R<int> F2(ref R<int> r2)
    {
        R<int> l2 = r2;
        return l2;
    }
    static R<int> F3(scoped ref R<int> r3)
    {
        R<int> l3 = r3;
        return l3;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,16): error CS8352: Cannot use variable 'l1' in this context because it may expose referenced variables outside of their declaration scope
                //         return l1; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "l1").WithArguments("l1").WithLocation(12, 16));
        }

        [Fact]
        public void RefSafeToEscape_02()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static ref R<int> F0(R<int> r0)
    {
        R<int> l0 = r0;
        return ref l0; // 1
    }
    static ref R<int> F1(scoped R<int> r1)
    {
        R<int> l1 = r1;
        return ref l1; // 2
    }
    static ref R<int> F2(ref R<int> r2)
    {
        R<int> l2 = r2;
        return ref l2; // 3
    }
    static ref R<int> F3(scoped ref R<int> r3)
    {
        R<int> l3 = r3;
        return ref l3; // 4
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,20): error CS8168: Cannot return local 'l0' by reference because it is not a ref local
                //         return ref l0; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l0").WithArguments("l0").WithLocation(7, 20),
                // (12,20): error CS8168: Cannot return local 'l1' by reference because it is not a ref local
                //         return ref l1; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l1").WithArguments("l1").WithLocation(12, 20),
                // (17,20): error CS8168: Cannot return local 'l2' by reference because it is not a ref local
                //         return ref l2; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l2").WithArguments("l2").WithLocation(17, 20),
                // (22,20): error CS8168: Cannot return local 'l3' by reference because it is not a ref local
                //         return ref l3; // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l3").WithArguments("l3").WithLocation(22, 20));
        }

        [Fact]
        public void SafeToEscape_03()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static R<int> F0(R<int> r0)
    {
        scoped R<int> l0 = r0;
        return l0; // 1
    }
    static R<int> F1(scoped R<int> r1)
    {
        scoped R<int> l1 = r1;
        return l1; // 2
    }
    static R<int> F2(ref R<int> r2)
    {
        scoped R<int> l2 = r2;
        return l2; // 3
    }
    static R<int> F3(scoped ref R<int> r3)
    {
        scoped R<int> l3 = r3;
        return l3; // 4
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,16): error CS8352: Cannot use variable 'l0' in this context because it may expose referenced variables outside of their declaration scope
                //         return l0; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "l0").WithArguments("l0").WithLocation(7, 16),
                // (12,16): error CS8352: Cannot use variable 'l1' in this context because it may expose referenced variables outside of their declaration scope
                //         return l1; // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "l1").WithArguments("l1").WithLocation(12, 16),
                // (17,16): error CS8352: Cannot use variable 'l2' in this context because it may expose referenced variables outside of their declaration scope
                //         return l2; // 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "l2").WithArguments("l2").WithLocation(17, 16),
                // (22,16): error CS8352: Cannot use variable 'l3' in this context because it may expose referenced variables outside of their declaration scope
                //         return l3; // 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "l3").WithArguments("l3").WithLocation(22, 16));
        }

        [Fact]
        public void RefSafeToEscape_03()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static ref R<int> F0(R<int> r0)
    {
        scoped R<int> l0 = r0;
        return ref l0; // 1
    }
    static ref R<int> F1(scoped R<int> r1)
    {
        scoped R<int> l1 = r1;
        return ref l1; // 2
    }
    static ref R<int> F2(ref R<int> r2)
    {
        scoped R<int> l2 = r2;
        return ref l2; // 3
    }
    static ref R<int> F3(scoped ref R<int> r3)
    {
        scoped R<int> l3 = r3;
        return ref l3; // 4
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,20): error CS8168: Cannot return local 'l0' by reference because it is not a ref local
                //         return ref l0; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l0").WithArguments("l0").WithLocation(7, 20),
                // (12,20): error CS8168: Cannot return local 'l1' by reference because it is not a ref local
                //         return ref l1; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l1").WithArguments("l1").WithLocation(12, 20),
                // (17,20): error CS8168: Cannot return local 'l2' by reference because it is not a ref local
                //         return ref l2; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l2").WithArguments("l2").WithLocation(17, 20),
                // (22,20): error CS8168: Cannot return local 'l3' by reference because it is not a ref local
                //         return ref l3; // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "l3").WithArguments("l3").WithLocation(22, 20));
        }

        [Fact]
        public void SafeToEscape_04()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static R<int> F0(R<int> r0)
    {
        ref R<int> l0 = ref r0;
        return l0;
    }
    static R<int> F1(scoped R<int> r1)
    {
        ref R<int> l1 = ref r1;
        return l1; // 1
    }
    static R<int> F2(ref R<int> r2)
    {
        ref R<int> l2 = ref r2;
        return l2;
    }
    static R<int> F3(scoped ref R<int> r3)
    {
        ref R<int> l3 = ref r3;
        return l3;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,16): error CS8352: Cannot use variable 'l1' in this context because it may expose referenced variables outside of their declaration scope
                //         return l1; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "l1").WithArguments("l1").WithLocation(12, 16));
        }

        [Fact]
        public void RefSafeToEscape_04()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static ref R<int> F0(R<int> r0)
    {
        ref R<int> l0 = ref r0;
        return ref l0; // 1
    }
    static ref R<int> F1(scoped R<int> r1)
    {
        ref R<int> l1 = ref r1;
        return ref l1; // 2
    }
    static ref R<int> F2(ref R<int> r2)
    {
        ref R<int> l2 = ref r2;
        return ref l2;
    }
    static ref R<int> F3(scoped ref R<int> r3)
    {
        ref R<int> l3 = ref r3;
        return ref l3; // 3
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,20): error CS8157: Cannot return 'l0' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref l0; // 1
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "l0").WithArguments("l0").WithLocation(7, 20),
                // (12,20): error CS8157: Cannot return 'l1' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref l1; // 2
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "l1").WithArguments("l1").WithLocation(12, 20),
                // (22,20): error CS8157: Cannot return 'l3' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref l3; // 3
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "l3").WithArguments("l3").WithLocation(22, 20));
        }

        [Fact]
        public void SafeToEscape_05()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static R<int> F0(R<int> r0)
    {
        scoped ref R<int> l0 = ref r0;
        return l0;
    }
    static R<int> F1(scoped R<int> r1)
    {
        scoped ref R<int> l1 = ref r1; // 1
        return l1;
    }
    static R<int> F2(ref R<int> r2)
    {
        scoped ref R<int> l2 = ref r2;
        return l2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,36): error CS8352: Cannot use variable 'R<int>' in this context because it may expose referenced variables outside of their declaration scope
                //         scoped ref R<int> l1 = ref r1; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("R<int>").WithLocation(11, 36));
        }

        [Fact]
        public void RefSafeToEscape_05()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static ref R<int> F0(R<int> r0)
    {
        scoped ref R<int> l0 = ref r0;
        return ref l0; // 1
    }
    static ref R<int> F1(scoped R<int> r1)
    {
        scoped ref R<int> l1 = ref r1; // 2
        return ref l1; // 3
    }
    static ref R<int> F2(ref R<int> r2)
    {
        scoped ref R<int> l2 = ref r2;
        return ref l2; // 4
    }
    static ref R<int> F3(scoped ref R<int> r3)
    {
        scoped ref R<int> l3 = ref r3;
        return ref l3; // 5
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,20): error CS8157: Cannot return 'l0' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref l0; // 1
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "l0").WithArguments("l0").WithLocation(7, 20),
                // (11,36): error CS8352: Cannot use variable 'R<int>' in this context because it may expose referenced variables outside of their declaration scope
                //         scoped ref R<int> l1 = ref r1; // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("R<int>").WithLocation(11, 36),
                // (12,20): error CS8157: Cannot return 'l1' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref l1; // 3
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "l1").WithArguments("l1").WithLocation(12, 20),
                // (17,20): error CS8157: Cannot return 'l2' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref l2; // 4
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "l2").WithArguments("l2").WithLocation(17, 20),
                // (22,20): error CS8157: Cannot return 'l3' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref l3; // 5
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "l3").WithArguments("l3").WithLocation(22, 20));
        }

        [Fact]
        public void ReturnValueField_01()
        {
            var source =
@"ref struct R<T>
{
    public T F;
    public T GetValue() => F;
    public ref T GetRef() => ref F; // 1
    public ref readonly T GetRefReadonly() => ref F; // 2
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,34): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref T GetRef() => ref F; // 1
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(5, 34),
                // (6,51): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref readonly T GetRefReadonly() => ref F; // 2
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 51));
        }

        [Fact]
        public void ReturnValueField_02()
        {
            var source =
@"struct S<T>
{
    public T F;
    public T GetValue() => F;
    public ref T GetRef() => ref F; // 1
    public ref readonly T GetRefReadonly() => ref F; // 2
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,34): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref T GetRef() => ref F; // 1
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(5, 34),
                // (6,51): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref readonly T GetRefReadonly() => ref F; // 2
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 51));
        }

        [Fact]
        public void ReturnRefField()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
    public T GetValue() => F;
    public ref T GetRef() => ref F;
    public ref readonly T GetRefReadonly() => ref F;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReturnRefReadonlyField()
        {
            var source =
@"ref struct R<T>
{
    public ref readonly T F;
    public T GetValue() => F;
    public ref T GetRef() => ref F; // 1
    public ref readonly T GetRefReadonly() => ref F;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,34): error CS8333: Cannot return field 'R<T>.F' by writable reference because it is a readonly variable
                //     public ref T GetRef() => ref F; // 1
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "F").WithArguments("field", "R<T>.F").WithLocation(5, 34));
        }

        [Fact]
        public void ReturnValueFieldByValue()
        {
            var source =
@"#pragma warning disable 649
ref struct R<T>
{
    public T F;
}
class Program
{
    static T F0<T>(R<T> r0) => r0.F;
    static T F1<T>(scoped R<T> r1) => r1.F;
    static T F2<T>(ref R<T> r2) => r2.F;
    static T F3<T>(scoped ref R<T> r3) => r3.F;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReturnValueFieldByRef()
        {
            var source =
@"ref struct R<T>
{
    public T F;
}
class Program
{
    static ref T F0<T>(R<T> r0) => ref r0.F; // 1
    static ref T F1<T>(scoped R<T> r1) => ref r1.F; // 2
    static ref T F2<T>(ref R<T> r2) => ref r2.F;
    static ref T F3<T>(scoped ref R<T> r3) => ref r3.F; // 3
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,40): error CS8167: Cannot return by reference a member of parameter 'r0' because it is not a ref or out parameter
                //     static ref T F0<T>(R<T> r0) => ref r0.F; // 1
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r0").WithArguments("r0").WithLocation(7, 40),
                // (8,47): error CS8167: Cannot return by reference a member of parameter 'r1' because it is not a ref or out parameter
                //     static ref T F1<T>(scoped R<T> r1) => ref r1.F; // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r1").WithArguments("r1").WithLocation(8, 47),
                // (10,51): error CS8167: Cannot return by reference a member of parameter 'r3' because it is not a ref or out parameter
                //     static ref T F3<T>(scoped ref R<T> r3) => ref r3.F; // 3
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r3").WithArguments("r3").WithLocation(10, 51));
        }

        [Theory]
        [InlineData("ref         ")]
        [InlineData("ref readonly")]
        public void ReturnRefFieldByValue(string refOrRefReadonly)
        {
            var source =
$@"ref struct R<T>
{{
    public {refOrRefReadonly} T F;
    public R(ref T t) {{ F = ref t; }}
}}
class Program
{{
    static T F0<T>(R<T> r) => r.F;
    static T F1<T>(ref R<T> r) => r.F;
    static T F2<T>(out R<T> r) {{ r = default; return r.F; }}
    static T F3<T>(in R<T> r) => r.F;
    static T F4<T>(scoped R<T> r) => r.F;
    static T F5<T>(scoped ref R<T> r) => r.F;
    static T F6<T>(scoped out R<T> r) {{ r = default; return r.F; }}
    static T F7<T>(scoped in R<T> r) => r.F;
}}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("ref         ")]
        [InlineData("ref readonly")]
        public void ReturnRefFieldByRef_01(string refOrRefReadonly)
        {
            var source =
$@"ref struct R<T>
{{
    public {refOrRefReadonly} T F;
    public R(ref T t) {{ F = ref t; }}
}}
class Program
{{
    static {refOrRefReadonly} T F0<T>(R<T> r) => ref r.F;
    static {refOrRefReadonly} T F1<T>(ref R<T> r) => ref r.F;
    static {refOrRefReadonly} T F2<T>(out R<T> r) {{ r = default; return ref r.F; }}
    static {refOrRefReadonly} T F3<T>(in R<T> r) => ref r.F;
    static {refOrRefReadonly} T F4<T>(scoped R<T> r) => ref r.F; // 1
    static {refOrRefReadonly} T F5<T>(scoped ref R<T> r) => ref r.F;
    static {refOrRefReadonly} T F6<T>(scoped out R<T> r) {{ r = default; return ref r.F; }}
    static {refOrRefReadonly} T F7<T>(scoped in R<T> r) => ref r.F;
}}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,55): error CS8352: Cannot use variable 'R<T>' in this context because it may expose referenced variables outside of their declaration scope
                //     static ref          T F4<T>(scoped R<T> r) => ref r.F; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r.F").WithArguments("R<T>").WithLocation(12, 55));
        }

        [Fact]
        public void ReturnRefFieldByRef_02()
        {
            var source =
@"ref struct R<T>
{
    public ref readonly T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static ref readonly T F1<T>(scoped ref T t)
    {
        R<T> r1 = new R<T>(ref t);
        return ref r1.F; // 1
    }
    static ref readonly T F2<T>(scoped ref T t)
    {
        R<T> r2;
        r2 = new R<T>(ref t); // 2
        return ref r2.F;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,20): error CS8352: Cannot use variable 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         return ref r1.F; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1.F").WithArguments("r1").WithLocation(11, 20),
                // (16,14): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         r2 = new R<T>(ref t); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t)").WithArguments("R<T>.R(ref T)", "t").WithLocation(16, 14),
                // (16,27): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //         r2 = new R<T>(ref t); // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(16, 27));
        }

        [Fact]
        public void ReturnRefFieldByRef_03()
        {
            var source =
@"ref struct R<T>
{
    public ref readonly T F;
    public R(in T t) { F = ref t; }
}
class Program
{
    static ref readonly T F1<T>(scoped in T t)
    {
        R<T> r1 = new R<T>(t);
        return ref r1.F; // 1
    }
    static ref readonly T F2<T>(scoped in T t)
    {
        R<T> r2 = new R<T>(in t);
        return ref r2.F; // 2
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,20): error CS8352: Cannot use variable 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         return ref r1.F; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1.F").WithArguments("r1").WithLocation(11, 20),
                // (16,20): error CS8352: Cannot use variable 'r2' in this context because it may expose referenced variables outside of their declaration scope
                //         return ref r2.F; // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r2.F").WithArguments("r2").WithLocation(16, 20));
        }

        [Fact]
        public void ReturnRefStructFieldByValue_01()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static R<T> F1<T>(ref T t) => new R<T>(ref t);
    static R<T> F2<T>(out T t, T tValue) { t = tValue; return new R<T>(ref t); } // 1
    static R<T> F3<T>(scoped ref T t) => new R<T>(ref t); // 2
    static R<T> F4<T>(scoped out T t, T tValue) { t = tValue; return new R<T>(ref t); } // 3
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,63): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F2<T>(out T t, T tValue) { t = tValue; return new R<T>(ref t); } // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t)").WithArguments("R<T>.R(ref T)", "t").WithLocation(9, 63),
                // (9,76): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static R<T> F2<T>(out T t, T tValue) { t = tValue; return new R<T>(ref t); } // 1
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(9, 76),
                // (10,42): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F3<T>(scoped ref T t) => new R<T>(ref t); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t)").WithArguments("R<T>.R(ref T)", "t").WithLocation(10, 42),
                // (10,55): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static R<T> F3<T>(scoped ref T t) => new R<T>(ref t); // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(10, 55),
                // (11,70): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //     static R<T> F4<T>(scoped out T t, T tValue) { t = tValue; return new R<T>(ref t); } // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t)").WithArguments("R<T>.R(ref T)", "t").WithLocation(11, 70),
                // (11,83): error CS8166: Cannot return a parameter by reference 't' because it is not a ref parameter
                //     static R<T> F4<T>(scoped out T t, T tValue) { t = tValue; return new R<T>(ref t); } // 3
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t").WithArguments("t").WithLocation(11, 83));
        }

        [Fact]
        public void ReturnRefStructFieldByValue_02()
        {
            var source =
@"ref struct R0<T>
{
    public ref T F0;
    public R0(ref T t) { F0 = ref t; }
}
ref struct R1<T>
{
    public R0<T> F1;
    public R1(ref T t) { F1 = new R0<T>(ref t); }
}
class Program
{
    static R0<T> F0<T>(R1<T> r0) => r0.F1;
    static R0<T> F1<T>(scoped R1<T> r1) => r1.F1; // 1
    static R0<T> F2<T>(ref R1<T> r2) => r2.F1;
    static R0<T> F3<T>(scoped ref R1<T> r3) => r3.F1;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,44): error CS8352: Cannot use variable 'R1<T>' in this context because it may expose referenced variables outside of their declaration scope
                //     static R0<T> F1<T>(scoped R1<T> r1) => r1.F1; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1.F1").WithArguments("R1<T>").WithLocation(14, 44));
        }

        [Fact]
        public void ReturnRefStructFieldByRef()
        {
            var source =
@"ref struct R0<T>
{
    public ref T F0;
    public R0(ref T t) { F0 = ref t; }
}
ref struct R1<T>
{
    public R0<T> F1;
    public R1(ref T t) { F1 = new R0<T>(ref t); }
}
class Program
{
    static ref R0<T> F0<T>(R1<T> r0) => ref r0.F1; // 1
    static ref R0<T> F1<T>(scoped R1<T> r1) => ref r1.F1; // 2
    static ref R0<T> F2<T>(ref R1<T> r2) => ref r2.F1;
    static ref R0<T> F3<T>(scoped ref R1<T> r3) => ref r3.F1; // 3
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,45): error CS8167: Cannot return by reference a member of parameter 'r0' because it is not a ref or out parameter
                //     static ref R0<T> F0<T>(R1<T> r0) => ref r0.F1; // 1
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r0").WithArguments("r0").WithLocation(13, 45),
                // (14,52): error CS8167: Cannot return by reference a member of parameter 'r1' because it is not a ref or out parameter
                //     static ref R0<T> F1<T>(scoped R1<T> r1) => ref r1.F1; // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r1").WithArguments("r1").WithLocation(14, 52),
                // (16,56): error CS8167: Cannot return by reference a member of parameter 'r3' because it is not a ref or out parameter
                //     static ref R0<T> F3<T>(scoped ref R1<T> r3) => ref r3.F1; // 3
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "r3").WithArguments("r3").WithLocation(16, 56));
        }

        [Fact]
        public void ReturnRefFieldFromCaller()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static ref T F0<T>(R<T> r0)
    {
        return ref r0.F;
    }
    static ref T F1<T>()
    {
        return ref F0(new R<T>()); // ok, returns null
    }
    static ref T F2<T>()
    {
        T t = default;
        return ref F0(new R<T>(ref t)); // error
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (19,20): error CS8347: Cannot use a result of 'Program.F0<T>(R<T>)' in this context because it may expose variables referenced by parameter 'r0' outside of their declaration scope
                //         return ref F0(new R<T>(ref t)); // error
                Diagnostic(ErrorCode.ERR_EscapeCall, "F0(new R<T>(ref t))").WithArguments("Program.F0<T>(R<T>)", "r0").WithLocation(19, 20),
                // (19,23): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         return ref F0(new R<T>(ref t)); // error
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t)").WithArguments("R<T>.R(ref T)", "t").WithLocation(19, 23),
                // (19,36): error CS8168: Cannot return local 't' by reference because it is not a ref local
                //         return ref F0(new R<T>(ref t)); // error
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t").WithArguments("t").WithLocation(19, 36));
        }

        [Fact]
        public void PropertyReturnValue_01()
        {
            var source =
@"ref struct R
{
    private ref int _i;
    public R(ref int i) { _i = ref i; }
}
class C
{
    R this[R x, R y] => x;
    R F1(R x1, R y1)
    {
        return this[x1, y1];
    }
    R F2(R x2)
    {
        int i2 = 0;
        return this[x2, new R(ref i2)]; // 1
    }
    static R F3(C c, R x3, R y3)
    {
        return c[x3, y3];
    }
    static R F4(C c, R y4)
    {
        int i4 = 0;
        return c[new R(ref i4), y4]; // 2
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,16): error CS8347: Cannot use a result of 'C.this[R, R]' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return this[x2, new R(ref i2)]; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "this[x2, new R(ref i2)]").WithArguments("C.this[R, R]", "y").WithLocation(16, 16),
                // (16,25): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return this[x2, new R(ref i2)]; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref i2)").WithArguments("R.R(ref int)", "i").WithLocation(16, 25),
                // (16,35): error CS8168: Cannot return local 'i2' by reference because it is not a ref local
                //         return this[x2, new R(ref i2)]; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i2").WithArguments("i2").WithLocation(16, 35),
                // (25,16): error CS8347: Cannot use a result of 'C.this[R, R]' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return c[new R(ref i4), y4]; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "c[new R(ref i4), y4]").WithArguments("C.this[R, R]", "x").WithLocation(25, 16),
                // (25,18): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return c[new R(ref i4), y4]; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref i4)").WithArguments("R.R(ref int)", "i").WithLocation(25, 18),
                // (25,28): error CS8168: Cannot return local 'i4' by reference because it is not a ref local
                //         return c[new R(ref i4), y4]; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i4").WithArguments("i4").WithLocation(25, 28));
        }

        [Fact]
        public void PropertyReturnValue_02()
        {
            var source =
@"ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class C
{
    R this[in int x, in int y] => new R(x);
    R F1(in int x1, in int y1)
    {
        return this[x1, y1];
    }
    R F2(in int x2)
    {
        int y2 = 0;
        return this[x2, y2]; // 1
    }
    static R F3(C c, in int x3, in int y3)
    {
        return c[x3, y3];
    }
    static R F4(C c, in int y4)
    {
        int x4 = 0;
        return c[x4, y4]; // 2
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,16): error CS8347: Cannot use a result of 'C.this[in int, in int]' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return this[x2, y2]; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "this[x2, y2]").WithArguments("C.this[in int, in int]", "y").WithLocation(16, 16),
                // (16,25): error CS8168: Cannot return local 'y2' by reference because it is not a ref local
                //         return this[x2, y2]; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "y2").WithArguments("y2").WithLocation(16, 25),
                // (25,16): error CS8347: Cannot use a result of 'C.this[in int, in int]' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return c[x4, y4]; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "c[x4, y4]").WithArguments("C.this[in int, in int]", "x").WithLocation(25, 16),
                // (25,18): error CS8168: Cannot return local 'x4' by reference because it is not a ref local
                //         return c[x4, y4]; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "x4").WithArguments("x4").WithLocation(25, 18));
        }

        [Fact]
        public void PropertyReturnValue_03()
        {
            var source =
@"ref struct R
{
    public ref int _i;
    public R(ref int i) { _i = ref i; }
}
class C
{
    ref int this[R x, R y] => ref x._i;
    ref int F1(R x1, R y1)
    {
        return ref this[x1, y1];
    }
    ref int F2(R x2)
    {
        int i2 = 0;
        return ref this[x2, new R(ref i2)]; // 1
    }
    static ref int F3(C c, R x3, R y3)
    {
        return ref c[x3, y3];
    }
    static ref int F4(C c, R y4)
    {
        int i4 = 0;
        return ref c[new R(ref i4), y4]; // 2
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,20): error CS8347: Cannot use a result of 'C.this[R, R]' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return ref this[x2, new R(ref i2)]; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "this[x2, new R(ref i2)]").WithArguments("C.this[R, R]", "y").WithLocation(16, 20),
                // (16,29): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return ref this[x2, new R(ref i2)]; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref i2)").WithArguments("R.R(ref int)", "i").WithLocation(16, 29),
                // (16,39): error CS8168: Cannot return local 'i2' by reference because it is not a ref local
                //         return ref this[x2, new R(ref i2)]; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i2").WithArguments("i2").WithLocation(16, 39),
                // (25,20): error CS8347: Cannot use a result of 'C.this[R, R]' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref c[new R(ref i4), y4]; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "c[new R(ref i4), y4]").WithArguments("C.this[R, R]", "x").WithLocation(25, 20),
                // (25,22): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return ref c[new R(ref i4), y4]; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref i4)").WithArguments("R.R(ref int)", "i").WithLocation(25, 22),
                // (25,32): error CS8168: Cannot return local 'i4' by reference because it is not a ref local
                //         return ref c[new R(ref i4), y4]; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i4").WithArguments("i4").WithLocation(25, 32));
        }

        [Fact]
        public void PropertyReturnValue_04()
        {
            var source =
@"class C
{
    ref readonly int this[in int x, in int y] => ref x;
    ref readonly int F1(in int x1, in int y1)
    {
        return ref this[x1, y1];
    }
    ref readonly int F2(in int x2)
    {
        int y2 = 0;
        return ref this[x2, y2]; // 1
    }
    static ref readonly int F3(C c, in int x3, in int y3)
    {
        return ref c[x3, y3];
    }
    static ref readonly int F4(C c, in int y4)
    {
        int x4 = 0;
        return ref c[x4, y4]; // 2
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,20): error CS8347: Cannot use a result of 'C.this[in int, in int]' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return ref this[x2, y2]; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "this[x2, y2]").WithArguments("C.this[in int, in int]", "y").WithLocation(11, 20),
                // (11,29): error CS8168: Cannot return local 'y2' by reference because it is not a ref local
                //         return ref this[x2, y2]; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "y2").WithArguments("y2").WithLocation(11, 29),
                // (20,20): error CS8347: Cannot use a result of 'C.this[in int, in int]' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref c[x4, y4]; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "c[x4, y4]").WithArguments("C.this[in int, in int]", "x").WithLocation(20, 20),
                // (20,22): error CS8168: Cannot return local 'x4' by reference because it is not a ref local
                //         return ref c[x4, y4]; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "x4").WithArguments("x4").WithLocation(20, 22));
        }

        [Fact]
        public void RefStructLocal_FromLocal_01()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static R<T> Create<T>() => new R<T>();
    static R<T> Create<T>(ref T t) => new R<T>(ref t);
    static void F<T>(T t) { }
    static T F1<T>()
    {
        T t = default;
        R<T> r1 = new R<T>(ref t);
        F(r1.F);
        return r1.F;
    }
    static T F2<T>()
    {
        T t = default;
        scoped R<T> r2 = new R<T>(ref t);
        F(r2.F);
        return r2.F;
    }
    static T F3<T>()
    {
        T t = default;
        R<T> r3 = new R<T>();
        r3.F = ref t;
        F(r3.F);
        return r3.F;
    }
    static T F4<T>()
    {
        T t = default;
        scoped R<T> r4 = new R<T>();
        r4.F = ref t;
        F(r4.F);
        return r4.F;
    }
    static T F5<T>()
    {
        T t = default;
        R<T> r5 = Create(ref t);
        F(r5.F);
        return r5.F;
    }
    static T F6<T>()
    {
        T t = default;
        scoped R<T> r6 = Create(ref t);
        F(r6.F);
        return r6.F;
    }
    static T F7<T>()
    {
        T t = default;
        R<T> r7 = Create<T>();
        r7.F = ref t;
        F(r7.F);
        return r7.F;
    }
    static T F8<T>()
    {
        T t = default;
        scoped R<T> r8 = Create<T>();
        r8.F = ref t;
        F(r8.F);
        return r8.F;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (29,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r3.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r3.F = ref t").WithArguments("F", "t").WithLocation(29, 9),
                // (59,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r7.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r7.F = ref t").WithArguments("F", "t").WithLocation(59, 9));
        }

        [Fact]
        public void RefStructLocal_FromParameter_01()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static R<T> Create<T>() => new R<T>();
    static R<T> Create<T>(ref T t) => new R<T>(ref t);
    static void F<T>(T t) { }
    static T F1<T>(T t)
    {
        R<T> r1 = new R<T>(ref t);
        F(r1.F);
        return r1.F;
    }
    static T F2<T>(T t)
    {
        scoped R<T> r2 = new R<T>(ref t);
        F(r2.F);
        return r2.F;
    }
    static T F3<T>(T t)
    {
        R<T> r3 = new R<T>();
        r3.F = ref t;
        F(r3.F);
        return r3.F;
    }
    static T F4<T>(T t)
    {
        scoped R<T> r4 = new R<T>();
        r4.F = ref t;
        F(r4.F);
        return r4.F;
    }
    static T F5<T>(T t)
    {
        R<T> r5 = Create(ref t);
        F(r5.F);
        return r5.F;
    }
    static T F6<T>(T t)
    {
        scoped R<T> r6 = Create(ref t);
        F(r6.F);
        return r6.F;
    }
    static T F7<T>(T t)
    {
        R<T> r7 = Create<T>();
        r7.F = ref t;
        F(r7.F);
        return r7.F;
    }
    static T F8<T>(T t)
    {
        scoped R<T> r8 = Create<T>();
        r8.F = ref t;
        F(r8.F);
        return r8.F;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (26,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r3.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r3.F = ref t").WithArguments("F", "t").WithLocation(26, 9),
                // (52,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r7.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r7.F = ref t").WithArguments("F", "t").WithLocation(52, 9));
        }

        [Fact]
        public void RefStructLocal_FromLocal_02()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static R<T> Create<T>() => new R<T>();
    static R<T> Create<T>(ref T t) => new R<T>(ref t);
    static void F<T>(R<T> r) { }
    static R<T> F1<T>()
    {
        T t = default;
        R<T> r1 = new R<T>(ref t);
        F(r1);
        return r1;
    }
    static R<T> F2<T>()
    {
        T t = default;
        scoped R<T> r2 = new R<T>(ref t);
        F(r2);
        return r2;
    }
    static R<T> F3<T>()
    {
        T t = default;
        R<T> r3 = new R<T>();
        r3.F = ref t;
        F(r3);
        return r3;
    }
    static R<T> F4<T>()
    {
        T t = default;
        scoped R<T> r4 = new R<T>();
        r4.F = ref t;
        F(r4);
        return r4;
    }
    static R<T> F5<T>()
    {
        T t = default;
        R<T> r5 = Create(ref t);
        F(r5);
        return r5;
    }
    static R<T> F6<T>()
    {
        T t = default;
        scoped R<T> r6 = Create(ref t);
        F(r6);
        return r6;
    }
    static R<T> F7<T>()
    {
        T t = default;
        R<T> r7 = Create<T>();
        r7.F = ref t;
        F(r7);
        return r7;
    }
    static R<T> F8<T>()
    {
        T t = default;
        scoped R<T> r8 = Create<T>();
        r8.F = ref t;
        F(r8);
        return r8;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         return r1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("r1").WithLocation(16, 16),
                // (23,16): error CS8352: Cannot use variable 'r2' in this context because it may expose referenced variables outside of their declaration scope
                //         return r2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r2").WithArguments("r2").WithLocation(23, 16),
                // (29,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r3.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r3.F = ref t").WithArguments("F", "t").WithLocation(29, 9),
                // (39,16): error CS8352: Cannot use variable 'r4' in this context because it may expose referenced variables outside of their declaration scope
                //         return r4;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r4").WithArguments("r4").WithLocation(39, 16),
                // (46,16): error CS8352: Cannot use variable 'r5' in this context because it may expose referenced variables outside of their declaration scope
                //         return r5;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r5").WithArguments("r5").WithLocation(46, 16),
                // (53,16): error CS8352: Cannot use variable 'r6' in this context because it may expose referenced variables outside of their declaration scope
                //         return r6;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r6").WithArguments("r6").WithLocation(53, 16),
                // (59,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r7.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r7.F = ref t").WithArguments("F", "t").WithLocation(59, 9),
                // (69,16): error CS8352: Cannot use variable 'r8' in this context because it may expose referenced variables outside of their declaration scope
                //         return r8;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r8").WithArguments("r8").WithLocation(69, 16));
        }

        [Fact]
        public void RefStructLocal_FromParameter_02()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
    public R(ref T t) { F = ref t; }
}
class Program
{
    static R<T> Create<T>() => new R<T>();
    static R<T> Create<T>(ref T t) => new R<T>(ref t);
    static void F<T>(R<T> r) { }
    static R<T> F1<T>(T t)
    {
        R<T> r1 = new R<T>(ref t);
        F(r1);
        return r1;
    }
    static R<T> F2<T>(T t)
    {
        scoped R<T> r2 = new R<T>(ref t);
        F(r2);
        return r2;
    }
    static R<T> F3<T>(T t)
    {
        R<T> r3 = new R<T>();
        r3.F = ref t;
        F(r3);
        return r3;
    }
    static R<T> F4<T>(T t)
    {
        scoped R<T> r4 = new R<T>();
        r4.F = ref t;
        F(r4);
        return r4;
    }
    static R<T> F5<T>(T t)
    {
        R<T> r5 = Create(ref t);
        F(r5);
        return r5;
    }
    static R<T> F6<T>(T t)
    {
        scoped R<T> r6 = Create(ref t);
        F(r6);
        return r6;
    }
    static R<T> F7<T>(T t)
    {
        R<T> r7 = Create<T>();
        r7.F = ref t;
        F(r7);
        return r7;
    }
    static R<T> F8<T>(T t)
    {
        scoped R<T> r8 = Create<T>();
        r8.F = ref t;
        F(r8);
        return r8;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (15,16): error CS8352: Cannot use variable 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         return r1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("r1").WithLocation(15, 16),
                // (21,16): error CS8352: Cannot use variable 'r2' in this context because it may expose referenced variables outside of their declaration scope
                //         return r2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r2").WithArguments("r2").WithLocation(21, 16),
                // (26,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r3.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r3.F = ref t").WithArguments("F", "t").WithLocation(26, 9),
                // (35,16): error CS8352: Cannot use variable 'r4' in this context because it may expose referenced variables outside of their declaration scope
                //         return r4;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r4").WithArguments("r4").WithLocation(35, 16),
                // (41,16): error CS8352: Cannot use variable 'r5' in this context because it may expose referenced variables outside of their declaration scope
                //         return r5;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r5").WithArguments("r5").WithLocation(41, 16),
                // (47,16): error CS8352: Cannot use variable 'r6' in this context because it may expose referenced variables outside of their declaration scope
                //         return r6;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r6").WithArguments("r6").WithLocation(47, 16),
                // (52,9): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //         r7.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r7.F = ref t").WithArguments("F", "t").WithLocation(52, 9),
                // (61,16): error CS8352: Cannot use variable 'r8' in this context because it may expose referenced variables outside of their declaration scope
                //         return r8;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r8").WithArguments("r8").WithLocation(61, 16));
        }

        [Fact]
        public void LocalFromRvalueInvocation()
        {
            var source =
@"ref struct R<T> { }
class Program
{
    static R<T> Create<T>(scoped ref T t)
    {
        return default;
    }
    static R<T> CreateReadonly<T>(scoped in T t)
    {
        return default;
    }
    static void F0(string s0)
    {
        R<string> r0;
        r0 = Create(ref s0);
    }
    static void F1(ref string s1)
    {
        R<string> r1;
        r1 = Create(ref s1);
    }
    static void F2(out string s2)
    {
        s2 = null;
        R<string> r2;
        r2 = Create(ref s2);
    }
    static void F3(in string s3)
    {
        R<string> r3;
        r3 = CreateReadonly(in s3);
    }
    static void F4(scoped ref string s4)
    {
        R<string> r4;
        r4 = Create(ref s4);
    }
    static void F5(scoped out string s5)
    {
        s5 = null;
        R<string> r5;
        r5 = Create(ref s5);
    }
    static void F6(scoped in string s6)
    {
        R<string> r6;
        r6 = CreateReadonly(in s6);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void This_FromLocal()
        {
            var source =
@"ref struct R<T>
{
    static R<T> Create() => new R<T>();
    static R<T> Create(ref T t) => new R<T>(ref t);
    static void M(R<T> r) { }
    private ref T F;
    public R(ref T t) { F = ref t; }
    public R(sbyte unused)
    {
        T t1 = default;
        this = new R<T>(ref t1);
        M(this);
    }
    public R(short unused)
    {
        T t2 = default;
        this = new R<T>();
        this.F = ref t2;
        M(this);
    }
    public R(int unused)
    {
        T t3 = default;
        this = Create(ref t3);
        M(this);
    }
    public R(long unused)
    {
        T t4 = default;
        this = Create();
        this.F = ref t4;
        M(this);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (11,16): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         this = new R<T>(ref t1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t1)").WithArguments("R<T>.R(ref T)", "t").WithLocation(11, 16),
                // (11,29): error CS8168: Cannot return local 't1' by reference because it is not a ref local
                //         this = new R<T>(ref t1);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t1").WithArguments("t1").WithLocation(11, 29),
                // (18,9): error CS8374: Cannot ref-assign 't2' to 'F' because 't2' has a narrower escape scope than 'F'.
                //         this.F = ref t2;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "this.F = ref t2").WithArguments("F", "t2").WithLocation(18, 9),
                // (24,16): error CS8347: Cannot use a result of 'R<T>.Create(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         this = Create(ref t3);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Create(ref t3)").WithArguments("R<T>.Create(ref T)", "t").WithLocation(24, 16),
                // (24,27): error CS8168: Cannot return local 't3' by reference because it is not a ref local
                //         this = Create(ref t3);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "t3").WithArguments("t3").WithLocation(24, 27),
                // (31,9): error CS8374: Cannot ref-assign 't4' to 'F' because 't4' has a narrower escape scope than 'F'.
                //         this.F = ref t4;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "this.F = ref t4").WithArguments("F", "t4").WithLocation(31, 9));
        }

        [Fact]
        public void This_FromParameter()
        {
            var source =
@"ref struct R<T>
{
    static R<T> Create() => new R<T>();
    static R<T> Create(ref T t) => new R<T>(ref t);
    static void M(R<T> r) { }
    private ref T F;
    R(ref T t) { F = ref t; }
    R(sbyte unused, T t1)
    {
        this = new R<T>(ref t1);
        M(this);
    }
    R(short unused, T t2)
    {
        this = new R<T>();
        this.F = ref t2;
        M(this);
    }
    R(int unused, T t3)
    {
        this = Create(ref t3);
        M(this);
    }
    R(long unused, T t4)
    {
        this = Create();
        this.F = ref t4;
        M(this);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,16): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         this = new R<T>(ref t1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t1)").WithArguments("R<T>.R(ref T)", "t").WithLocation(10, 16),
                // (10,29): error CS8166: Cannot return a parameter by reference 't1' because it is not a ref parameter
                //         this = new R<T>(ref t1);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t1").WithArguments("t1").WithLocation(10, 29),
                // (16,9): error CS8374: Cannot ref-assign 't2' to 'F' because 't2' has a narrower escape scope than 'F'.
                //         this.F = ref t2;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "this.F = ref t2").WithArguments("F", "t2").WithLocation(16, 9),
                // (21,16): error CS8347: Cannot use a result of 'R<T>.Create(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         this = Create(ref t3);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Create(ref t3)").WithArguments("R<T>.Create(ref T)", "t").WithLocation(21, 16),
                // (21,27): error CS8166: Cannot return a parameter by reference 't3' because it is not a ref parameter
                //         this = Create(ref t3);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t3").WithArguments("t3").WithLocation(21, 27),
                // (27,9): error CS8374: Cannot ref-assign 't4' to 'F' because 't4' has a narrower escape scope than 'F'.
                //         this.F = ref t4;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "this.F = ref t4").WithArguments("F", "t4").WithLocation(27, 9));
        }

        [Fact]
        public void This_FromRefParameter()
        {
            var source =
@"ref struct R<T>
{
    static void M(R<T> r) { }
    private ref T F;
    R(ref T t) { F = ref t; }
    R(sbyte unused, ref T t1)
    {
        this = new R<T>(ref t1);
        M(this);
    }
    R(short unused, scoped ref T t2)
    {
        this = new R<T>(ref t2);
        M(this);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,16): error CS8347: Cannot use a result of 'R<T>.R(ref T)' in this context because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         this = new R<T>(ref t2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R<T>(ref t2)").WithArguments("R<T>.R(ref T)", "t").WithLocation(13, 16),
                // (13,29): error CS8166: Cannot return a parameter by reference 't2' because it is not a ref parameter
                //         this = new R<T>(ref t2);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "t2").WithArguments("t2").WithLocation(13, 29));
        }

        [Fact]
        public void This_FromRefStructParameter()
        {
            var source =
@"ref struct R<T>
{
    static void M(R<T> r) { }
    private ref T F;
    R(sbyte unused, ref R<T> r1)
    {
        this = r1;
        M(this);
    }
    R(short unused, scoped ref R<T> r2)
    {
        this = r2;
        M(this);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,19): warning CS0169: The field 'R<T>.F' is never used
                //     private ref T F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("R<T>.F").WithLocation(4, 19));
        }

        [Fact]
        public void NestedScope()
        {
            var source =
@"ref struct R<T>
{
    public ref T F;
}
class Program
{
    static T F<T>()
    {
        scoped R<T> r;
        {
            T t = default;
            r.F = ref t;
        }
        return r.F;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,13): error CS8374: Cannot ref-assign 't' to 'F' because 't' has a narrower escape scope than 'F'.
                //             r.F = ref t;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r.F = ref t").WithArguments("F", "t").WithLocation(12, 13));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void InstanceMethodWithOutVar_01(LanguageVersion languageVersion)
        {
            var source =
@"using System;
ref struct R
{
    public R(Span<int> s) { }
    public void F(out R r) { r = default; }
}
class Program
{
    static void F(out R r)
    { 
        Span<int> s1 = stackalloc int[10];
        R r1 = new R(s1);
        r1.F(out r);
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (13,9): error CS8352: Cannot use variable 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         r1.F(out r);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("r1").WithLocation(13, 9));
        }

        [Fact]
        public void InstanceMethodWithOutVar_02()
        {
            var source =
@"ref struct R
{
    public ref int _i;
    public R(ref int i) { _i = ref i; }
    public void F(out R r) { r = new R(ref _i); }
}
class Program
{
    static void F(out R r)
    { 
        int i = 0;
        R r1 = new R(ref i);
        r1.F(out r);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,9): error CS8352: Cannot use variable 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         r1.F(out r);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("r1").WithLocation(13, 9));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void InstanceMethodWithOutVar_03(LanguageVersion languageVersion)
        {
            var source =
@"using System;
ref struct R
{
    public void F(out Span<int> s) { s = default; }
}
class Program
{
    static void Main()
    {
        Span<int> s = stackalloc int[10];
        R r = new R();
        r.F(out s);
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (12,9): error CS8350: This combination of arguments to 'R.F(out Span<int>)' is disallowed because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         r.F(out s);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "r.F(out s)").WithArguments("R.F(out System.Span<int>)", "s").WithLocation(12, 9),
                // (12,17): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         r.F(out s);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(12, 17));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReturnThis_01(LanguageVersion languageVersion)
        {
            var source =
@"ref struct R
{
    R F1() => this;
    ref R F2() => ref this;
    ref readonly R F3() => ref this;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (4,23): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     ref R F2() => ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(4, 23),
                // (5,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     ref readonly R F3() => ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(5, 32));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReturnThis_02(LanguageVersion languageVersion)
        {
            var source =
@"readonly ref struct R
{
    R F1() => this;
    ref R F2() => ref this;
    ref readonly R F3() => ref this;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (4,23): error CS8354: Cannot return 'this' by reference.
                //     ref R F2() => ref this;
                Diagnostic(ErrorCode.ERR_RefReturnThis, "this").WithLocation(4, 23),
                // (5,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     ref readonly R F3() => ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(5, 32));
        }

        [Fact]
        public void RefInitializer_LangVer()
        {
            var source = @"
int x = 42;
var r = new R() { field = ref x };

ref struct R
{
    public ref int field;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref int field;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref int").WithArguments("ref fields", "11.0").WithLocation(7, 12)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefInitializer_LangVer_FromMetadata()
        {
            var lib_cs = @"
public ref struct R
{
    public ref int field;
}
";
            var source = @"
int x = 42;
var r1 = new R() { field = ref x }; // 1
var r2 = new R() { field = x }; // 2

R r3 = default;
_ = r3 with { field = ref x }; // 3
";
            var lib = CreateCompilation(lib_cs, parseOptions: TestOptions.Regular11);

            var comp = CreateCompilation(source, references: new[] { lib.EmitToImageReference() }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,20): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // var r1 = new R() { field = ref x }; // 1
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "field").WithArguments("ref fields", "11.0").WithLocation(3, 20),
                // (4,20): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // var r2 = new R() { field = x }; // 2
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "field").WithArguments("ref fields", "11.0").WithLocation(4, 20),
                // (7,15): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // _ = r3 with { field = ref x }; // 3
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "field").WithArguments("ref fields", "11.0").WithLocation(7, 15)
                );
        }

        [Fact]
        public void RefInitializer()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        int x = 42;
        var r = new R() { field = ref x };
        System.Console.Write(r.ToString());
    }
}

ref struct R
{
    public ref int field;
    public override string ToString()
    {
        return field.ToString();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("42"));
            verifier.VerifyIL("C.Main",
"""
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int V_0, //x
                R V_1, //r
                R V_2)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_2
  IL_0005:  initobj    "R"
  IL_000b:  ldloc.2
  IL_000c:  ldfld      "ref int R.field"
  IL_0011:  ldloc.0
  IL_0012:  stind.i4
  IL_0013:  ldloc.2
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  constrained. "R"
  IL_001d:  callvirt   "string object.ToString()"
  IL_0022:  call       "void System.Console.Write(string)"
  IL_0027:  ret
}
""");
        }

        [Fact]
        public void RefInitializer_RHSMustBeDefinitelyAssigned()
        {
            // The right operand must be definitely assigned at the point of the ref assignment.
            var source = @"
int x;
var r = new R() { field = ref x };

ref struct R
{
    public ref int field;
    public override string ToString()
    {
        return field.ToString();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,31): error CS0165: Use of unassigned local variable 'x'
                // var r = new R() { field = ref x };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(3, 31)
                );
        }

        [Fact]
        public void RefInitializer_RHSTypeMustMatch()
        {
            // The right operand must be an expression that yields an lvalue designating a value of the same type as the left operand.
            var source = @"
object x = null;
var r = new R() { field = ref x };

ref struct R
{
    public ref int field;
    public override string ToString()
    {
        return field.ToString();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,31): error CS8173: The expression must be of type 'int' because it is being assigned by reference
                // var r = new R() { field = ref x };
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "x").WithArguments("int").WithLocation(3, 31)
                );
        }

        [Fact]
        public void RefInitializer_RHSTypeMustMatch_ImplicitConversionExists()
        {
            // The right operand must be an expression that yields an lvalue designating a value of the same type as the left operand.
            var source = @"
S1 x = default;
var r = new R() { field = ref x };

struct S1 { }
struct S2
{
    public static implicit operator S2(S1 s1) => throw null;
}
ref struct R
{
    public ref S2 field;
    public override string ToString()
    {
        return field.ToString();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,31): error CS8173: The expression must be of type 'S2' because it is being assigned by reference
                // var r = new R() { field = ref x };
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "x").WithArguments("S2").WithLocation(3, 31)
                );
        }

        [Fact]
        public void RefInitializer_StaticRefField()
        {
            var source = @"
int x = 0;
var r = new R() { field = ref x };

ref struct R
{
    public static ref int field;
    public override string ToString()
    {
        return field.ToString();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,19): error CS1914: Static field or property 'R.field' cannot be assigned in an object initializer
                // var r = new R() { field = ref x };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "field").WithArguments("R.field").WithLocation(3, 19),
                // (7,27): error CS0106: The modifier 'static' is not valid for this item
                //     public static ref int field;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "field").WithArguments("static").WithLocation(7, 27)
                );
        }

        [Fact]
        public void RefInitializer_VarInvocationReserved()
        {
            var source = @"
var r = new R() { field = ref var() };

ref struct R
{
    public ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,31): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                // var r = new R() { field = ref var() };
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var()").WithLocation(2, 31),
                // (2,31): error CS0103: The name 'var' does not exist in the current context
                // var r = new R() { field = ref var() };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(2, 31)
                );
        }

        [Fact]
        public void RefInitializer_ReadonlyRefField()
        {
            var source = @"
int x = 42;
var r = new R() { field = ref x };

ref struct R
{
    public readonly ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,19): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                // var r = new R() { field = ref x };
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(3, 19)
                );
        }

        [Fact]
        public void RefInitializer_RefReadonlyField()
        {
            var source = @"
int x = 42;
var r = new R() { field = ref x };

ref struct R
{
    public ref readonly int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefInitializer_RefReadonlyValue_Field()
        {
            // If the left operand is a writeable ref (i.e. it designates anything other than a `ref readonly` local or  `in` parameter), then the right operand must be a writeable lvalue.
            var source = @"
class C
{
    ref readonly int Value() => throw null;

    void M()
    {
        var r = new R() { field = ref Value() };
    }
}

ref struct R
{
    public ref int field;
}
";
            // Confusing error message
            // Tracked by https://github.com/dotnet/roslyn/issues/62756
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,39): error CS8331: Cannot assign to method 'C.Value()' because it is a readonly variable
                //         var r = new R() { field = ref Value() };
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "Value()").WithArguments("method", "C.Value()").WithLocation(8, 39)
                );
        }

        [Fact]
        public void RefInitializer_AssignInParameter()
        {
            var source = @"
class C
{
    static void F(in int i)
    {
        _ = new R1 { _f = ref i };
        _ = new R2 { _f = ref i }; // 1
    }
}

ref struct R1
{
    public ref readonly int _f;
}

ref struct R2
{
    public ref int _f;
}
";
            // Diagnostic is missing parameter name
            // Tracked by https://github.com/dotnet/roslyn/issues/62096
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,31): error CS8331: Cannot assign to variable 'in int' because it is a readonly variable
                //         _ = new R2 { _f = ref i }; // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "i").WithArguments("variable", "in int").WithLocation(7, 31)
                );
        }

        [Fact]
        public void RefInitializer_RefReadonlyValue_GetOnlyProperty()
        {
            var source = @"
class C
{
    ref readonly int Value() => throw null;

    void M()
    {
        var r = new R() { Property = ref Value() };
    }
}

ref struct R
{
    public ref int Property { get => throw null; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,27): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         var r = new R() { Property = ref Value() };
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "Property").WithLocation(8, 27)
                );
        }

        [Fact]
        public void RefInitializer_RefReadonlyValue_Property()
        {
            var source = @"
class C
{
    ref readonly int Value() => throw null;

    void M()
    {
        var r = new R() { Property = ref Value() };
    }
}

ref struct R
{
    public ref int Property { get => throw null; set => throw null; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,27): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         var r = new R() { Property = ref Value() };
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "Property").WithLocation(8, 27),
                // (14,50): error CS8147: Properties which return by reference cannot have set accessors
                //     public ref int Property { get => throw null; set => throw null; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(14, 50)
                );
        }

        [Fact]
        public void RefInitializer_RefReadonlyValue_Indexer()
        {
            var source = @"
var r = new R() { [0] = ref Value() }; // 1

R r2 = default;
r2[0] = ref Value(); // 2

ref readonly int Value() => throw null;

ref struct R
{
    public ref int this[int i] { get => throw null; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,19): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new R() { [0] = ref Value() }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[0]").WithLocation(2, 19),
                // (5,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // r2[0] = ref Value(); // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "r2[0]").WithLocation(5, 1)
                );
        }

        [Fact]
        public void RefInitializer_Indexer()
        {
            var source = @"
var r = new R() { [0] = ref Value() }; // 1

R r2 = default;
r2[0] = ref Value(); // 2

ref int Value() => throw null;

ref struct R
{
    public ref int this[int i] { get => throw null; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,19): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new R() { [0] = ref Value() }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[0]").WithLocation(2, 19),
                // (5,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // r2[0] = ref Value(); // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "r2[0]").WithLocation(5, 1)
                );
        }

        [Fact]
        public void RefInitializer_ValueMustReferToLocation()
        {
            var source = @"
class C
{
    int Value() => throw null;

    void M()
    {
        var r = new R() { field = ref Value() };
    }
}

ref struct R
{
    public ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,39): error CS1510: A ref or out value must be an assignable variable
                //         var r = new R() { field = ref Value() };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "Value()").WithLocation(8, 39)
                );
        }

        [Fact]
        public void RefInitializer_RefReadonlyField_RefReadonlyValue()
        {
            var source = @"
class C
{
    ref readonly int Value() => throw null;

    void M()
    {
        var r = new R() { field = ref Value() };
    }
}

ref struct R
{
    public ref readonly int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefInitializer_OnInterface_Field()
        {
            var source = @"
class C
{
    void M<T>() where T : I, new()
    {
        int x = 42;
        var t = new T() { field = ref x };
    }
}
interface I
{
    public ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,20): error CS0525: Interfaces cannot contain instance fields
                //     public ref int field;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field").WithLocation(12, 20),
                // (12,20): error CS9059: A ref field can only be declared in a ref struct.
                //     public ref int field;
                Diagnostic(ErrorCode.ERR_RefFieldInNonRefStruct, "field").WithLocation(12, 20)
                );
        }

        [Fact]
        public void RefInitializer_OnInterface_Property()
        {
            var source = @"
class C
{
    void M<T>() where T : I, new()
    {
        int x = 42;
        var t = new T() { Property = ref x };
    }
}

interface I
{
    public ref int Property { get; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,27): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         var t = new T() { Property = ref x };
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "Property").WithLocation(7, 27)
                );
        }

        [Fact]
        public void RefInitializer_Collection()
        {
            var source = @"
using System.Collections;

int x = 42;
int y = 43;
var r = new R() { ref x, ref y };

struct R : IEnumerable
{
    public void Add(ref int x) => throw null;
    public IEnumerator GetEnumerator() => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,5): warning CS0219: The variable 'x' is assigned but its value is never used
                // int x = 42;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(4, 5),
                // (5,5): warning CS0219: The variable 'y' is assigned but its value is never used
                // int y = 43;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(5, 5),
                // (6,19): error CS1073: Unexpected token 'ref'
                // var r = new R() { ref x, ref y };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(6, 19),
                // (6,26): error CS1073: Unexpected token 'ref'
                // var r = new R() { ref x, ref y };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(6, 26)
                );
        }

        [Fact]
        public void RefInitializer_OnNonRefField()
        {
            var source = @"
int x = 42;
var r = new R() { field = ref x }; // 1

R r2 = default;
r2.field = ref x; // 2

ref struct R
{
    public int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,19): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new R() { field = ref x }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "field").WithLocation(3, 19),
                // (6,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // r2.field = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "r2.field").WithLocation(6, 1)
                );
        }

        [Fact]
        public void RefInitializer_OnNonRefProperty()
        {
            var source = @"
int x = 42;
var r = new R() { Property = ref x }; // 1

R r2 = default;
r2.Property = ref x; // 2

ref struct R
{
    public int Property { get; set; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,19): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new R() { Property = ref x }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "Property").WithLocation(3, 19),
                // (6,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // r2.Property = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "r2.Property").WithLocation(6, 1)
                );
        }

        [Fact]
        public void RefInitializer_OnNonRefIndexer()
        {
            var source = @"
int x = 42;
var r = new R() { [0] = ref x }; // 1

R r2 = default;
r2[0] = ref x; // 2

ref struct R
{
    public int this[int i] { get => throw null; set => throw null; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,19): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new R() { [0] = ref x }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[0]").WithLocation(3, 19),
                // (6,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // r2[0] = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "r2[0]").WithLocation(6, 1)
                );
        }

        [Fact]
        public void RefInitializer_OnArray()
        {
            var source = @"
public ref struct C
{
    C M()
    {
        int x = 0;
        var c = new C { array = { [0] = ref x } }; // 1
        return c;
    }

    void M2()
    {
        int x = 0;
        C c2 = new C();
        c2.array[0] = ref x; // 2
    }

    public int[] array;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,35): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         var c = new C { array = { [0] = ref x } }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[0]").WithLocation(7, 35),
                // (15,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         c2.array[0] = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "c2.array[0]").WithLocation(15, 9)
                );
        }

        [Fact]
        public void RefInitializer_OnPointer()
        {
            var source = @"
public unsafe class C
{
    public int* pointer;

    C M()
    {
        int x = 0;
        var c = new C { pointer = { [0] = ref x } }; // 1
        return c;
    }

    void M2()
    {
        int x = 0;
        C c2 = new C();
        c2.pointer[0] = ref x; // 2
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (9,37): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         var c = new C { pointer = { [0] = ref x } }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[0]").WithLocation(9, 37),
                // (17,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         c2.pointer[0] = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "c2.pointer[0]").WithLocation(17, 9)
                );
        }

        [Fact]
        public void RefInitializer_OnEvent()
        {
            var source = @"
int x = 42;
var r = new C { a = ref x }; // 1

C c = default;
c.a = ref x; // 2

class C
{
    public event System.Action a;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,17): error CS0070: The event 'C.a' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                // var r = new C { a = ref x }; // 1
                Diagnostic(ErrorCode.ERR_BadEventUsage, "a").WithArguments("C.a", "C").WithLocation(3, 17),
                // (6,3): error CS0070: The event 'C.a' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                // c.a = ref x; // 2
                Diagnostic(ErrorCode.ERR_BadEventUsage, "a").WithArguments("C.a", "C").WithLocation(6, 3)
                );
        }

        [Fact]
        public void RefInitializer_OnEvent_ThisMemberAccess()
        {
            var source = @"
int x = 42;
var r1 = new C { this.a = ref x }; // 1

class C
{
    public event System.Action a;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,16): error CS1922: Cannot initialize type 'C' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                // var r1 = new C { this.a = ref x }; // 1
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ this.a = ref x }").WithArguments("C").WithLocation(3, 16),
                // (3,18): error CS0747: Invalid initializer member declarator
                // var r1 = new C { this.a = ref x }; // 1
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "this.a = ref x").WithLocation(3, 18),
                // (3,18): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                // var r1 = new C { this.a = ref x }; // 1
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this").WithLocation(3, 18),
                // (7,32): warning CS0067: The event 'C.a' is never used
                //     public event System.Action a;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "a").WithArguments("C.a").WithLocation(7, 32)
                );
        }

        [Fact]
        public void RefInitializer_OnMethodGroup()
        {
            var source = @"
int x = 42;
var r = new C { F = ref x }; // 1

C c = default;
c.F = ref x; // 2

class C
{
    public void F() { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,17): error CS1913: Member 'F' cannot be initialized. It is not a field or property.
                // var r = new C { F = ref x }; // 1
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "F").WithArguments("F").WithLocation(3, 17),
                // (6,1): error CS1656: Cannot assign to 'F' because it is a 'method group'
                // c.F = ref x; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "c.F").WithArguments("F", "method group").WithLocation(6, 1)
                );
        }

        [Fact]
        public void RefInitializer_Nested()
        {
            var source = @"
class C
{
    void M()
    {
        int x = 42;
        var r = new Container { item = { field = ref x } };
        System.Console.Write(r.item.field);
    }
}
ref struct Container
{
    public Item item;
}
ref struct Item
{
    public ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("42"));
            verifier.VerifyIL("C.M", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0, //x
                Container V_1)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    ""Container""
  IL_000b:  ldloc.1
  IL_000c:  ldfld      ""Item Container.item""
  IL_0011:  ldfld      ""ref int Item.field""
  IL_0016:  ldloc.0
  IL_0017:  stind.i4
  IL_0018:  ldloc.1
  IL_0019:  ldfld      ""Item Container.item""
  IL_001e:  ldfld      ""ref int Item.field""
  IL_0023:  ldind.i4
  IL_0024:  call       ""void System.Console.Write(int)""
  IL_0029:  ret
}
");
        }

        [Fact]
        public void RefInitializer_Nullability()
        {
            var source = @"
#nullable enable

S<object> x1 = default;
S<object?> x2 = default;

_ = new R<object> { field = ref x1 };
_ = new R<object> { field = ref x2 }; // 1
_ = new R<object?> { field = ref x1 }; // 2
_ = new R<object?> { field = ref x2 };

_ = new R<object>() with { field = ref x1 };
_ = new R<object>() with { field = ref x2 }; // 3
_ = new R<object?>() with { field = ref x1 }; // 4
_ = new R<object?>() with { field = ref x2 };

struct S<T> { }
ref struct R<T>
{
    public ref S<T> field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,33): warning CS8619: Nullability of reference types in value of type 'S<object?>' doesn't match target type 'S<object>'.
                // _ = new R<object> { field = ref x2 }; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x2").WithArguments("S<object?>", "S<object>").WithLocation(8, 33),
                // (9,34): warning CS8619: Nullability of reference types in value of type 'S<object>' doesn't match target type 'S<object?>'.
                // _ = new R<object?> { field = ref x1 }; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x1").WithArguments("S<object>", "S<object?>").WithLocation(9, 34),
                // (13,40): warning CS8619: Nullability of reference types in value of type 'S<object?>' doesn't match target type 'S<object>'.
                // _ = new R<object>() with { field = ref x2 }; // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x2").WithArguments("S<object?>", "S<object>").WithLocation(13, 40),
                // (14,41): warning CS8619: Nullability of reference types in value of type 'S<object>' doesn't match target type 'S<object?>'.
                // _ = new R<object?>() with { field = ref x1 }; // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "x1").WithArguments("S<object>", "S<object?>").WithLocation(14, 41)
                );
        }

        [Fact]
        public void RefInitializer_RefOnNestedInitializer()
        {
            var source = @"
int x = 42;
var r = new R { field = ref { item = 42 } }; // 1

struct S
{
    public int item;
}
ref struct R
{
    public ref S field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,5): warning CS0219: The variable 'x' is assigned but its value is never used
                // int x = 42;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(2, 5),
                // (3,29): error CS1525: Invalid expression term '{'
                // var r = new R { field = ref { item = 42 } }; // 1
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(3, 29),
                // (3,29): error CS1003: Syntax error, ',' expected
                // var r = new R { field = ref { item = 42 } }; // 1
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(3, 29),
                // (3,29): error CS0747: Invalid initializer member declarator
                // var r = new R { field = ref { item = 42 } }; // 1
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "{ item = 42 }").WithLocation(3, 29),
                // (3,31): error CS0103: The name 'item' does not exist in the current context
                // var r = new R { field = ref { item = 42 } }; // 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "item").WithArguments("item").WithLocation(3, 31)
                );
        }

        [Fact]
        public void RefInitializer_FieldOnDynamic()
        {
            var source = @"
int x = 42;
var r = new S { D = { field = ref x } }; // 1

S s = default;
s.D.field = ref x; // 2

struct S
{
    public dynamic D;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,23): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new S { D = { field = ref x } }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "field").WithLocation(3, 23),
                // (6,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // s.D.field = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.D.field").WithLocation(6, 1)
                );
        }

        [Fact]
        public void RefInitializer_DynamicField()
        {
            var source = @"
int i = 42;
var r = new R<dynamic> { F = ref i }; // 1

ref struct R<T>
{
    public ref T F;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,34): error CS8173: The expression must be of type 'dynamic' because it is being assigned by reference
                // var r = new R<dynamic> { F = ref i }; // 1
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "i").WithArguments("dynamic").WithLocation(3, 34)
                );
        }

        [Fact]
        public void RefInitializer_DynamicField_DynamicValue()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        dynamic i = 42;
        var r = new R<dynamic> { F = ref i };
        System.Console.Write(r.F);

        var r2 = new R<dynamic>(ref i);
        System.Console.Write(r2.F);
    }
}

ref struct R<T>
{
    public R(ref T f) { F = ref f; }
    public ref T F;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.NetCoreAppAndCSharp);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("4242"));
            verifier.VerifyIL("C.Main", """
{
  // Code size      258 (0x102)
  .maxstack  9
  .locals init (object V_0, //i
                R<dynamic> V_1, //r
                R<dynamic> V_2, //r2
                R<dynamic> V_3)
  IL_0000:  nop
  IL_0001:  ldc.i4.s   42
  IL_0003:  box        "int"
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_3
  IL_000b:  initobj    "R<dynamic>"
  IL_0011:  ldloc.3
  IL_0012:  ldfld      "ref dynamic R<dynamic>.F"
  IL_0017:  ldloc.0
  IL_0018:  stind.ref
  IL_0019:  ldloc.3
  IL_001a:  stloc.1
  IL_001b:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__0"
  IL_0020:  brfalse.s  IL_0024
  IL_0022:  br.s       IL_0064
  IL_0024:  ldc.i4     0x100
  IL_0029:  ldstr      "Write"
  IL_002e:  ldnull
  IL_002f:  ldtoken    "C"
  IL_0034:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
  IL_0039:  ldc.i4.2
  IL_003a:  ldc.i4.0
  IL_003b:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo[] System.GC.AllocateUninitializedArray<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>(int, bool)"
  IL_0040:  dup
  IL_0041:  ldc.i4.0
  IL_0042:  ldc.i4.s   33
  IL_0044:  ldnull
  IL_0045:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
  IL_004a:  stelem.ref
  IL_004b:  dup
  IL_004c:  ldc.i4.1
  IL_004d:  ldc.i4.0
  IL_004e:  ldnull
  IL_004f:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
  IL_0054:  stelem.ref
  IL_0055:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
  IL_005a:  call       "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
  IL_005f:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__0"
  IL_0064:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__0"
  IL_0069:  ldfld      "System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target"
  IL_006e:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__0"
  IL_0073:  ldtoken    "System.Console"
  IL_0078:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
  IL_007d:  ldloc.1
  IL_007e:  ldfld      "ref dynamic R<dynamic>.F"
  IL_0083:  ldind.ref
  IL_0084:  callvirt   "void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)"
  IL_0089:  nop
  IL_008a:  ldloca.s   V_0
  IL_008c:  newobj     "R<dynamic>..ctor(ref dynamic)"
  IL_0091:  stloc.2
  IL_0092:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__1"
  IL_0097:  brfalse.s  IL_009b
  IL_0099:  br.s       IL_00db
  IL_009b:  ldc.i4     0x100
  IL_00a0:  ldstr      "Write"
  IL_00a5:  ldnull
  IL_00a6:  ldtoken    "C"
  IL_00ab:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
  IL_00b0:  ldc.i4.2
  IL_00b1:  ldc.i4.0
  IL_00b2:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo[] System.GC.AllocateUninitializedArray<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>(int, bool)"
  IL_00b7:  dup
  IL_00b8:  ldc.i4.0
  IL_00b9:  ldc.i4.s   33
  IL_00bb:  ldnull
  IL_00bc:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
  IL_00c1:  stelem.ref
  IL_00c2:  dup
  IL_00c3:  ldc.i4.1
  IL_00c4:  ldc.i4.0
  IL_00c5:  ldnull
  IL_00c6:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
  IL_00cb:  stelem.ref
  IL_00cc:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
  IL_00d1:  call       "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
  IL_00d6:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__1"
  IL_00db:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__1"
  IL_00e0:  ldfld      "System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target"
  IL_00e5:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__0.<>p__1"
  IL_00ea:  ldtoken    "System.Console"
  IL_00ef:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
  IL_00f4:  ldloc.2
  IL_00f5:  ldfld      "ref dynamic R<dynamic>.F"
  IL_00fa:  ldind.ref
  IL_00fb:  callvirt   "void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)"
  IL_0100:  nop
  IL_0101:  ret
}
""");
        }

        [Fact]
        public void RefInitializer_SubstitutedObjectField()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        object i = 42;
        var r = new R<object> { F = ref i };
        System.Console.Write(r.F);

        var r2 = new R<object>(ref i);
        System.Console.Write(r2.F);
    }
}

ref struct R<T>
{
    public R(ref T f) { F = ref f; }
    public ref T F;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("4242"));
            verifier.VerifyIL("C.Main", """
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (object V_0, //i
                R<object> V_1)
  IL_0000:  ldc.i4.s   42
  IL_0002:  box        "int"
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_1
  IL_000a:  initobj    "R<object>"
  IL_0010:  ldloc.1
  IL_0011:  ldfld      "ref object R<object>.F"
  IL_0016:  ldloc.0
  IL_0017:  stind.ref
  IL_0018:  ldloc.1
  IL_0019:  ldfld      "ref object R<object>.F"
  IL_001e:  ldind.ref
  IL_001f:  call       "void System.Console.Write(object)"
  IL_0024:  ldloca.s   V_0
  IL_0026:  newobj     "R<object>..ctor(ref object)"
  IL_002b:  ldfld      "ref object R<object>.F"
  IL_0030:  ldind.ref
  IL_0031:  call       "void System.Console.Write(object)"
  IL_0036:  ret
}
""");
        }

        [Fact]
        public void RefInitializer_DynamicInstance()
        {
            var source = @"
int x = 42;
var r = new dynamic { field = ref x }; // 1

dynamic r2 = null;
r2.field = ref x; // 2
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,13): error CS8386: Invalid object creation
                // var r = new dynamic { field = ref x }; // 1
                Diagnostic(ErrorCode.ERR_InvalidObjectCreation, "dynamic").WithLocation(3, 13),
                // (3,23): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new dynamic { field = ref x }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "field").WithLocation(3, 23),
                // (6,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // r2.field = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "r2.field").WithLocation(6, 1)
                );
        }

        [Fact]
        public void RefInitializer_DynamicIndexer()
        {
            var source = @"
int x = 42;
var r = new S { D = { [0] = ref x } }; // 1

S s = default;
s.D[0] = ref x; // 2

struct S
{
    public dynamic D;
}
ref struct R
{
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,23): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var r = new S { D = { [0] = ref x } }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[0]").WithLocation(3, 23),
                // (6,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // s.D[0] = ref x; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.D[0]").WithLocation(6, 1)
                );
        }

        [Fact]
        public void RefInitializer_DynamicIndexer_Nested()
        {
            var source = @"
dynamic x = 1;
int i = 42;
var a = new A() { [y: x, x: x] = { X = ref i } }; // 1

A a2 = null;
a2[y: x, x: x].X = ref i; // 2

public class A
{
    public dynamic this[int x, int y] { get => throw null; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,36): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // var a = new A() { [y: x, x: x] = { X = ref i } }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "X").WithLocation(4, 36),
                // (7,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // a2[y: x, x: x].X = ref i; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "a2[y: x, x: x].X").WithLocation(7, 1)
                );
        }

        [Fact]
        public void RefInitializer_Escape()
        {
            var source = @"
public class C
{
    public static R M1()
    {
        int x = 42;
        var r = new R { field = ref x };
        return r; // 1
    }
    public static R M2(ref int x)
    {
        var r = new R { field = ref x };
        return r;
    }
    public static R M3()
    {
        R r = default;
        {
            int x = 42;
            r = new R { field = ref x }; // 2
        }
        return r;
    }
    public static R M4()
    {
        R r = default;
        int x = 42;
        {
            r = new R { field = ref x }; // 3
        }
        return r;
    }
    public static void M5(ref R r)
    {
        int x = 42;
        r = new R { field = ref x }; // 4
    }
}

public ref struct R
{
    public ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS8352: Cannot use variable 'r' in this context because it may expose referenced variables outside of their declaration scope
                //         return r; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r").WithArguments("r").WithLocation(8, 16),
                // (20,25): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //             r = new R { field = ref x }; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(20, 25),
                // (29,25): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //             r = new R { field = ref x }; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(29, 25),
                // (36,21): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //         r = new R { field = ref x }; // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(36, 21)
                );

            // Initializer values behave like constructor parameters for purpose of escape analysis
            source = @"
public class C
{
    public static R M1()
    {
        int x = 42;
        var r = new R(ref x);
        return r; // 1
    }
    public static R M2(ref int x)
    {
        var r = new R(ref x);
        return r;
    }
    public static R M3()
    {
        R r = default;
        {
            int x = 42;
            r = new R(ref x); // 2
        }
        return r;
    }
    public static R M4()
    {
        R r = default;
        int x = 42;
        {
            r = new R(ref x); // 3
        }
        return r;
    }
    public static void M5(ref R r)
    {
        int x = 42;
        r = new R(ref x); // 4
    }
}

public ref struct R
{
    public ref int field;
    public R(ref int i) { }
}
";
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS8352: Cannot use variable 'r' in this context because it may expose referenced variables outside of their declaration scope
                //         return r; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r").WithArguments("r").WithLocation(8, 16),
                // (20,17): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //             r = new R(ref x); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref x)").WithArguments("R.R(ref int)", "i").WithLocation(20, 17),
                // (20,27): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //             r = new R(ref x); // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "x").WithArguments("x").WithLocation(20, 27),
                // (29,17): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //             r = new R(ref x); // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref x)").WithArguments("R.R(ref int)", "i").WithLocation(29, 17),
                // (29,27): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //             r = new R(ref x); // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "x").WithArguments("x").WithLocation(29, 27),
                // (36,13): error CS8347: Cannot use a result of 'R.R(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         r = new R(ref x); // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(ref x)").WithArguments("R.R(ref int)", "i").WithLocation(36, 13),
                // (36,23): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //         r = new R(ref x); // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "x").WithArguments("x").WithLocation(36, 23)
                );
        }

        [Fact]
        public void RefInitializer_Escape_Nested()
        {
            var source = @"
class C
{
    public static Container M3()
    {
        Container r = default;
        {
            int x = 42;
            var r = new Container { item = { field = ref x } }; // 1
        }
        return r;
    }
    public static Container M4()
    {
        Container r = default;
        int x = 42;
        {
            r = new Container { item = { field = ref x } }; // 2
        }
        return r;
    }
    public static void M5(ref Container r)
    {
        int x = 42;
        r = new Container { item = { field = ref x } }; // 3
    }
    public static Container M6()
    {
        int x = 42;
        var r = new Container { item = { field = ref x } };
        return r; // 4
    }
 }
ref struct Container
{
    public Item item;
}
ref struct Item
{
    public ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,17): error CS0136: A local or parameter named 'r' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var r = new Container { item = { field = ref x } }; // 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "r").WithArguments("r").WithLocation(9, 17),
                // (18,42): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //             r = new Container { item = { field = ref x } }; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(18, 42),
                // (25,38): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //         r = new Container { item = { field = ref x } }; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(25, 38),
                // (31,16): error CS8352: Cannot use variable 'r' in this context because it may expose referenced variables outside of their declaration scope
                //         return r; // 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r").WithArguments("r").WithLocation(31, 16)
                );
        }

        [Fact]
        public void RefWith()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        int x = 42;
        var r = new R() with { field = ref x };
        System.Console.Write(r.ToString());
    }
}

ref struct R
{
    public ref int field;
    public override string ToString()
    {
        return field.ToString();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("42"));
            verifier.VerifyIL("C.Main",
"""
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int V_0, //x
                R V_1, //r
                R V_2)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_2
  IL_0005:  initobj    "R"
  IL_000b:  ldloc.2
  IL_000c:  ldfld      "ref int R.field"
  IL_0011:  ldloc.0
  IL_0012:  stind.i4
  IL_0013:  ldloc.2
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  constrained. "R"
  IL_001d:  callvirt   "string object.ToString()"
  IL_0022:  call       "void System.Console.Write(string)"
  IL_0027:  ret
}
""");
        }

        [Fact]
        public void RefWith_Escape()
        {
            var source = @"
public class C
{
    public static R M1()
    {
        int x = 42;
        var r = new R() with { field = ref x };
        return r; // 1
    }
    public static R M2(ref int x)
    {
        var r = new R() with { field = ref x };
        return r;
    }
    public static R M3()
    {
        R r = default;
        {
            int x = 42;
            r = new R() with { field = ref x }; // 2
        }
        return r;
    }
    public static R M4()
    {
        R r = default;
        int x = 42;
        {
            r = new R() with { field = ref x }; // 3
        }
        return r;
    }
    public static void M5(ref R r)
    {
        int x = 42;
        r = new R() with { field = ref x }; // 4
    }
}

public ref struct R
{
    public ref int field;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS8352: Cannot use variable 'r' in this context because it may expose referenced variables outside of their declaration scope
                //         return r; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r").WithArguments("r").WithLocation(8, 16),
                // (20,32): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //             r = new R() with { field = ref x }; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(20, 32),
                // (29,32): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //             r = new R() with { field = ref x }; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(29, 32),
                // (36,28): error CS8168: Cannot return local 'x' by reference because it is not a ref local
                //         r = new R() with { field = ref x }; // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "field = ref x").WithArguments("x").WithLocation(36, 28)
                );
        }


        [Fact]
        public void RefScoped()
        {
            var source =
@"ref struct R
{
    ref scoped R field;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (3,9): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                //     ref scoped R field;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 9),
                // (3,16): error CS0542: 'R': member names cannot be the same as their enclosing type
                //     ref scoped R field;
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "R").WithArguments("R").WithLocation(3, 16),
                // (3,16): warning CS0169: The field 'R.R' is never used
                //     ref scoped R field;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "R").WithArguments("R.R").WithLocation(3, 16),
                // (3,18): error CS1002: ; expected
                //     ref scoped R field;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "field").WithLocation(3, 18),
                // (3,23): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     ref scoped R field;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 23),
                // (3,23): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     ref scoped R field;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 23)
                );

            source =
@"ref struct R
{
    ref scoped R Property { get => throw null; }
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (3,9): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 9),
                // (3,16): error CS0542: 'R': member names cannot be the same as their enclosing type
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "R").WithArguments("R").WithLocation(3, 16),
                // (3,16): warning CS0169: The field 'R.R' is never used
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "R").WithArguments("R.R").WithLocation(3, 16),
                // (3,18): error CS1002: ; expected
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Property").WithLocation(3, 18),
                // (3,27): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(3, 27),
                // (3,27): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(3, 27),
                // (3,33): error CS1519: Invalid token '=>' in class, record, struct, or interface member declaration
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(3, 33),
                // (3,33): error CS1519: Invalid token '=>' in class, record, struct, or interface member declaration
                //     ref scoped R Property { get => throw null; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(3, 33),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1)
                );

            source =
@"ref struct R
{
    void M(ref scoped R parameter) 
    { 
    }
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (3,16): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     void M(ref scoped R parameter) 
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(3, 16)
                );

            source =
@"ref struct R
{
    void M(in scoped R parameter) 
    { 
    }
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (3,15): error CS8339:  The parameter modifier 'scoped' cannot follow 'in'
                //     void M(in scoped R parameter) 
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "in").WithLocation(3, 15)
                );

            source =
@"ref struct R
{
    void M(out scoped R parameter) 
    { 
    }
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (3,16): error CS8339:  The parameter modifier 'scoped' cannot follow 'out'
                //     void M(out scoped R parameter) 
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "out").WithLocation(3, 16)
                );

            source =
@"ref struct R
{
    void M(ref scoped scoped R parameter) 
    { 
    }
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (3,16): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     void M(ref scoped scoped R parameter) 
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(3, 16),
                // (3,23): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //     void M(ref scoped scoped R parameter) 
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(3, 23)
                );

            source =
@"ref struct R
{
    void M() 
    { 
        ref scoped R local;
    }
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (5,13): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                //         ref scoped R local;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(5, 13),
                // (5,20): error CS8174: A declaration of a by-reference variable must have an initializer
                //         ref scoped R local;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "R").WithLocation(5, 20),
                // (5,20): warning CS0168: The variable 'R' is declared but never used
                //         ref scoped R local;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "R").WithArguments("R").WithLocation(5, 20),
                // (5,22): error CS1002: ; expected
                //         ref scoped R local;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "local").WithLocation(5, 22),
                // (5,22): error CS0103: The name 'local' does not exist in the current context
                //         ref scoped R local;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(5, 22),
                // (5,22): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         ref scoped R local;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "local").WithLocation(5, 22)
                );

            source =
@"ref struct R
{
    void M() 
    { 
        scoped ref scoped R local;
    }
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (5,20): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                //         scoped ref scoped R local;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(5, 20),
                // (5,27): error CS8174: A declaration of a by-reference variable must have an initializer
                //         scoped ref scoped R local;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "R").WithLocation(5, 27),
                // (5,27): warning CS0168: The variable 'R' is declared but never used
                //         scoped ref scoped R local;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "R").WithArguments("R").WithLocation(5, 27),
                // (5,29): error CS1002: ; expected
                //         scoped ref scoped R local;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "local").WithLocation(5, 29),
                // (5,29): error CS0103: The name 'local' does not exist in the current context
                //         scoped ref scoped R local;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(5, 29),
                // (5,29): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         scoped ref scoped R local;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "local").WithLocation(5, 29)
                );

            source =
@"ref struct R
{
    ref scoped R M() => throw null;
}";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (3,9): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
                //     ref scoped R M() => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 9),
                // (3,16): error CS0542: 'R': member names cannot be the same as their enclosing type
                //     ref scoped R M() => throw null;
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "R").WithArguments("R").WithLocation(3, 16),
                // (3,16): warning CS0169: The field 'R.R' is never used
                //     ref scoped R M() => throw null;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "R").WithArguments("R.R").WithLocation(3, 16),
                // (3,18): error CS1002: ; expected
                //     ref scoped R M() => throw null;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "M").WithLocation(3, 18),
                // (3,18): error CS1520: Method must have a return type
                //     ref scoped R M() => throw null;
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "M").WithLocation(3, 18),
                // (3,18): error CS8958: The parameterless struct constructor must be 'public'.
                //     ref scoped R M() => throw null;
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "M").WithLocation(3, 18)
                );

            source = @"
delegate void M(ref scoped R parameter);
ref struct R { }
";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (2,21): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                // delegate void M(ref scoped R parameter);
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(2, 21)
                );

            source = @"
ref struct R
{
    void M()
    {
        _ = void (ref scoped R parameter) => throw null;
    }
}
";
            comp = CreateCompilationWithSpanAndMemoryExtensions(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         _ = void (ref scoped R parameter) => throw null;
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(6, 9),
                // (6,23): error CS8339:  The parameter modifier 'scoped' cannot follow 'ref'
                //         _ = void (ref scoped R parameter) => throw null;
                Diagnostic(ErrorCode.ERR_BadParameterModifiersOrder, "scoped").WithArguments("scoped", "ref").WithLocation(6, 23)
                );
        }
    }
}
