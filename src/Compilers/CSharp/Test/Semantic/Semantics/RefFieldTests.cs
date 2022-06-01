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
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RefFieldTests : CSharpTestBase
    {
        private static string IncludeExpectedOutput(string expectedOutput)
        {
#if NET7_0_OR_GREATER
            return expectedOutput;
#else
            return null;
#endif
        }

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
        this = new S<T>(ref t0);
        this = new S<T> { F1 = t0 };
        this = default;
        S<T> s;
        s = new S<T>();
        s = new S<T>(ref t0);
        s = new S<T> { F1 = t0 };
    }
    static void M1(T t1)
    {
        S<T> s1;
        s1 = default;
        s1 = new S<T>();
        s1 = new S<T>(ref t1);
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
        this = new S<T>(ref t4);
        this = new S<T> { F1 = t4 };
        S<T> s;
        s = new S<T>();
        s = new S<T>(ref t4);
        s = new S<T> { F1 = t4 };
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
                // (3,12): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public ref T F1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref T").WithArguments("ref fields").WithLocation(3, 12),
                // (4,12): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public ref readonly T F2;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref readonly T").WithArguments("ref fields").WithLocation(4, 12));

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
        s1 = new S<T>(ref t);
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
                // (9,25): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         s1 = new S<T> { F1 = t };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "F1").WithArguments("ref fields").WithLocation(9, 25),
                // (15,25): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         s2 = new S<T> { F1 = t };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "F1").WithArguments("ref fields").WithLocation(15, 25),
                // (20,12): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         M1(s.F1);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F1").WithArguments("ref fields").WithLocation(20, 12),
                // (21,12): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         M1(s.F2);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F2").WithArguments("ref fields").WithLocation(21, 12),
                // (22,16): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         M2(ref s.F1);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F1").WithArguments("ref fields").WithLocation(22, 16));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F1"), "ref T S<T>.F1", RefKind.Ref, new string[0]);
            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F2"), "ref readonly T S<T>.F2", RefKind.RefReadOnly, new string[0]);

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularNext);
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
                // (3,12): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public ref T F;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref T").WithArguments("ref fields").WithLocation(3, 12));

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
                // (8,9): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         s.F = 2;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F").WithArguments("ref fields").WithLocation(8, 9),
                // (9,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Console.WriteLine(s.F);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F").WithArguments("ref fields").WithLocation(9, 27),
                // (12,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Console.WriteLine(s.F);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F").WithArguments("ref fields").WithLocation(12, 27));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref T S<T>.F", RefKind.Ref, new string[0]);

            var verifier = CompileAndVerify(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularNext, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
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
                // (3,12): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public ref readonly T F;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref readonly T").WithArguments("ref fields").WithLocation(3, 12));

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
                // (13,9): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         s.F.G = 2;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F").WithArguments("ref fields").WithLocation(13, 9),
                // (14,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Console.WriteLine(s.F.G);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F").WithArguments("ref fields").WithLocation(14, 27),
                // (17,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Console.WriteLine(s.F.G);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.F").WithArguments("ref fields").WithLocation(17, 27));

            VerifyFieldSymbol(comp.GetMember<FieldSymbol>("S.F"), "ref readonly T S<T>.F", RefKind.RefReadOnly, new string[0]);

            var verifier = CompileAndVerify(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularNext, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
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
            // PROTOTYPE: `fixed ref` field declaration should be disallowed.
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyEmitDiagnostics();
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
            // PROTOTYPE: `fixed ref` field use should be disallowed.
            var comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics();
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
        F = ref tValue;
        F = ref tRef;
        F = ref tOut;
        F = ref tIn; // 1
    }
    object P
    {
        init
        {
            F = ref GetRef();
            F = ref GetRefReadonly(); // 2
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //         F = ref tIn; // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(10, 17),
                // (17,21): error CS8331: Cannot assign to method 'S<T>.GetRefReadonly()' because it is a readonly variable
                //             F = ref GetRefReadonly(); // 2
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "GetRefReadonly()").WithArguments("method", "S<T>.GetRefReadonly()").WithLocation(17, 21));
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
        F = ref tValue;
        F = ref tRef;
        F = ref tOut;
        F = ref tIn;
    }
    object P
    {
        init
        {
            F = ref GetRef();
            F = ref GetRefReadonly();
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
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
        F = ref tValue;
        F = ref tRef;
        F = ref tOut;
        F = ref tIn; // 1
    }
    object P
    {
        init
        {
            F = ref GetRef();
            F = ref GetRefReadonly(); // 2
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            // PROTOTYPE: Consider changing ERR_AssignReadonlyNotField to "Cannot take a writable 'ref' to a readonly variable".
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //         F = ref tIn; // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(10, 17),
                // (17,21): error CS8331: Cannot assign to method 'S<T>.GetRefReadonly()' because it is a readonly variable
                //             F = ref GetRefReadonly(); // 2
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "GetRefReadonly()").WithArguments("method", "S<T>.GetRefReadonly()").WithLocation(17, 21));
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
        F = ref tValue;
        F = ref tRef;
        F = ref tOut;
        F = ref tIn;
    }
    object P
    {
        init
        {
            F = ref GetRef();
            F = ref GetRefReadonly();
        }
    }
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
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
        S<int> s;

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
        S<int> s;

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
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; }
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; }
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 1

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 2
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; }
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 3

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 4
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; }
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 5

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 6
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 7
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 8
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 9
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,69): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //     static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; } // 1
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(12, 69),
                // (14,64): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sRef.F = ref tValue").WithArguments("F", "tValue").WithLocation(14, 64),
                // (17,77): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //     static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; } // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(17, 77),
                // (19,80): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 4
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sOut.F = ref tValue").WithArguments("F", "tValue").WithLocation(19, 80),
                // (22,93): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //     static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; } // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(22, 93),
                // (24,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 6
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(24, 61),
                // (25,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 7
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(25, 61),
                // (26,77): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 8
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(26, 77),
                // (27,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 9
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
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; }
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; }

    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; }

    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; }

    static void Main()
    {
        int x, y;
        S<int> s;

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

        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignRefToRef(ref s, ref y);
        Console.WriteLine(s.F);
        x = 3; y = 4;
        s = new S<int>(ref x);
        AssignOutToRef(ref s, out y);
        Console.WriteLine(s.F);

        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignRefToOut(out s, ref y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignOutToOut(out s, out y);
        Console.WriteLine(s.F);
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"1
1
1
4
0
6
0"));
            verifier.VerifyILMultiple(
                "Program.AssignValueToValue<T>",
@"{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_1
  IL_0004:  stfld      ""ref T S<T>.F""
  IL_0009:  ret
}",
                "Program.AssignRefToValue<T>",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  stfld      ""ref T S<T>.F""
  IL_0008:  ret
}",
                "Program.AssignOutToValue<T>",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarga.s   V_0
  IL_0009:  ldarg.1
  IL_000a:  stfld      ""ref T S<T>.F""
  IL_000f:  ret
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
                "Program.AssignOutToRef<T>",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""ref T S<T>.F""
  IL_000e:  ret
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
}",
                "Program.AssignOutToOut<T>",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S<T>""
  IL_0007:  ldarg.1
  IL_0008:  initobj    ""T""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.1
  IL_0010:  stfld      ""ref T S<T>.F""
  IL_0015:  ret
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
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; }
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; }
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; }

    static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 1
    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; }
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; }

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 2
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; }
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; }

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 3
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 4
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 5
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 6
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,64): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sRef.F = ref tValue").WithArguments("F", "tValue").WithLocation(14, 64),
                // (19,80): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sOut.F = ref tValue").WithArguments("F", "tValue").WithLocation(19, 80),
                // (24,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 3
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(24, 61),
                // (25,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; } // 4
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(25, 61),
                // (26,77): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; } // 5
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "sIn.F").WithArguments("variable", "in S<T>").WithLocation(26, 77),
                // (27,61): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 6
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
    static void AssignValueToValue<T>(S<T> s, T tValue) { s.F = ref tValue; }
    static void AssignRefToValue<T>(S<T> s, ref T tRef) { s.F = ref tRef; }
    static void AssignOutToValue<T>(S<T> s, out T tOut) { tOut = default; s.F = ref tOut; }
    static void AssignInToValue<T>(S<T> s, in T tIn)    { s.F = ref tIn; }

    static void AssignRefToRef<T>(ref S<T> sRef, ref T tRef) { sRef.F = ref tRef; }
    static void AssignOutToRef<T>(ref S<T> sRef, out T tOut) { tOut = default; sRef.F = ref tOut; }
    static void AssignInToRef<T>(ref S<T> sRef, in T tIn)    { sRef.F = ref tIn; }

    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = ref tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = ref tOut; }
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = ref tIn; }

    static void Main()
    {
        int x, y;
        S<int> s;

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
        AssignRefToOut(out s, ref y);
        Console.WriteLine(s.F);
        x = 5; y = 6;
        s = new S<int>(ref x);
        AssignOutToOut(out s, out y);
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
1
1
4
0
4
6
0
6"));
            verifier.VerifyILMultiple(
                "Program.AssignValueToValue<T>",
@"{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_1
  IL_0004:  stfld      ""ref readonly T S<T>.F""
  IL_0009:  ret
}",
                "Program.AssignRefToValue<T>",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  stfld      ""ref readonly T S<T>.F""
  IL_0008:  ret
}",
                "Program.AssignOutToValue<T>",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarga.s   V_0
  IL_0009:  ldarg.1
  IL_000a:  stfld      ""ref readonly T S<T>.F""
  IL_000f:  ret
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
                "Program.AssignOutToRef<T>",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  initobj    ""T""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""ref readonly T S<T>.F""
  IL_000e:  ret
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
                "Program.AssignOutToOut<T>",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S<T>""
  IL_0007:  ldarg.1
  IL_0008:  initobj    ""T""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.1
  IL_0010:  stfld      ""ref readonly T S<T>.F""
  IL_0015:  ret
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
            // PROTOTYPE: Should not report ERR_RefReturnStructThis.
            // PROTOTYPE: Should report errors for F5() and F9() since out parameters are implicitly scoped.
            comp.VerifyEmitDiagnostics(
                // (4,30): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref T F1() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithArguments("this").WithLocation(4, 30),
                // (5,39): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref readonly T F2() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithArguments("this").WithLocation(5, 39),
                // (9,39): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref T F3<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(9, 39),
                // (13,48): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref readonly T F7<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(13, 48));
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
            // PROTOTYPE: Should not report ERR_RefReturnStructThis.
            // PROTOTYPE: Should report error for F9() since out parameters are implicitly scoped.
            comp.VerifyEmitDiagnostics(
                // (4,30): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     public ref T F1() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(4, 30),
                // (5,39): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref readonly T F2() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithArguments("this").WithLocation(5, 39),
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
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(12, 42),
                // (13,48): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref readonly T F7<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(13, 48));
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
            // PROTOTYPE: Should not report ERR_RefReturnStructThis.
            // PROTOTYPE: Should report errors for F5() and F9() since out parameters are implicitly scoped.
            comp.VerifyEmitDiagnostics(
                // (4,30): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref T F1() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithArguments("this").WithLocation(4, 30),
                // (5,39): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref readonly T F2() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithArguments("this").WithLocation(5, 39),
                // (9,39): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref T F3<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(9, 39),
                // (13,48): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref readonly T F7<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(13, 48));
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
            // PROTOTYPE: Should not report ERR_RefReturnStructThis.
            // PROTOTYPE: Should report error for F9() since out parameters are implicitly scoped.
            comp.VerifyEmitDiagnostics(
                // (4,30): error CS8333: Cannot return field 'S<T>.F' by writable reference because it is a readonly variable
                //     public ref T F1() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(4, 30),
                // (5,39): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref readonly T F2() => ref F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithArguments("this").WithLocation(5, 39),
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
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(12, 42),
                // (13,48): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref readonly T F7<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(13, 48));
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
        var r = new R<int>();
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
        var s = new S<int>();
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
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"42
42
42
"));
            verifier.VerifyIL("Program.Read<T>",
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldfld      ""ref R1<T> R2<T>.R1""
  IL_0007:  ldfld      ""ref T R1<T>.F""
  IL_000c:  ldobj      ""T""
  IL_0011:  ret
}");
            verifier.VerifyIL("Program.ReadIn<T>",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref R1<T> R2<T>.R1""
  IL_0006:  ldfld      ""ref T R1<T>.F""
  IL_000b:  ldobj      ""T""
  IL_0010:  ret
}");
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
            // PROTOTYPE: The dereference of `new S<T>(...).F` should not be elided
            // since the behavior may be observable as a NullReferenceException.
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
            // PROTOTYPE: Should compile without errors.
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (17,46): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref T RefReturn<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(17, 46),
                // (18,63): error CS8167: Cannot return by reference a member of parameter 's' because it is not a ref or out parameter
                //     static ref readonly T RefReadonlyReturn<T>(S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "s").WithArguments("s").WithLocation(18, 63));
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

        // PROTOTYPE: Test with { ref readonly, readonly ref, readonly ref readonly }.
        // PROTOTYPE: Test from constructor and from instance method.
        [Fact]
        public void CompoundOperations()
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
        var s = new S<int>();
        s.F = ref x;
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
        public void RefAutoProperty()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T P { get; }
    public ref readonly T Q { get; }
    public S(ref T t)
    {
        P = ref t;
        Q = ref t;
    }
}
class Program
{
    static void Main()
    {
        int x = 1;
        var s = new S<int>(ref x);
        s.P = 2;
        Console.WriteLine(s.P);
        Console.WriteLine(s.Q);
        Console.WriteLine(x);
        x = 3;
        Console.WriteLine(s.P);
        Console.WriteLine(s.Q);
        Console.WriteLine(x);
        s.P = ref x;
        s.Q = ref x;
    }
}";
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should this scenario be supported? Test all valid combinations of { get, set, init }.
            // PROTOTYPE: Verify use of ref auto-property does not generate a LanguageVersion error
            // (since we generally don't look at how properties are implemented).
            comp.VerifyEmitDiagnostics(
                // (4,18): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref T P { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P").WithArguments("S<T>.P").WithLocation(4, 18),
                // (5,27): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref readonly T Q { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "Q").WithArguments("S<T>.Q").WithLocation(5, 27),
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P").WithLocation(8, 9),
                // (9,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         Q = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "Q").WithLocation(9, 9),
                // (26,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.P = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P").WithLocation(26, 9),
                // (27,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         s.Q = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.Q").WithLocation(27, 9));
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
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "t").WithArguments("this").WithLocation(5, 31));
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
            // PROTOTYPE: Should not report ERR_RefReturnStructThis.
            comp.VerifyEmitDiagnostics(
                // (5,31): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     internal ref T F() => ref t;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "t").WithArguments("this").WithLocation(5, 31));
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
    public static void F1(scoped R r1) { }
    public static void F2(ref R x2, ref scoped R y2) { }
    public static void F3(scoped in R r3) { }
    public static void F4(scoped out R r4) { r4 = default; }
    public static void F5(object o, ref scoped R r5) { }
    public static void F6(in scoped R r6) { }
    public static void F7(out scoped R r7) { r7 = default; }
    public static void F8(scoped ref scoped R r8) { }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (7,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F1(scoped R r1) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(7, 27),
                // (8,41): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F2(ref R x2, ref scoped R y2) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(8, 41),
                // (9,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F3(scoped in R r3) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(9, 27),
                // (10,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F4(scoped out R r4) { r4 = default; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(10, 27),
                // (11,41): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F5(object o, ref scoped R r5) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(11, 41),
                // (12,30): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F6(in scoped R r6) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(12, 30),
                // (13,31): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F7(out scoped R r7) { r7 = default; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(13, 31),
                // (14,27): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F8(scoped ref scoped R r8) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(14, 27),
                // (14,38): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F8(scoped ref scoped R r8) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(14, 38));

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
        A.F2(ref y, ref x);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics();

            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F1").Parameters[0], "scoped R r1", RefKind.None, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F2").Parameters[0], "ref R x2", RefKind.Ref, DeclarationScope.Unscoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F2").Parameters[1], "ref scoped R y2", RefKind.Ref, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F3").Parameters[0], "scoped in R r3", RefKind.In, DeclarationScope.RefScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F4").Parameters[0], "scoped out R r4", RefKind.Out, DeclarationScope.RefScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F5").Parameters[1], "ref scoped R r5", RefKind.Ref, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F6").Parameters[0], "in scoped R r6", RefKind.In, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F7").Parameters[0], "out scoped R r7", RefKind.Out, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.F8").Parameters[0], "ref scoped R r8", RefKind.Ref, DeclarationScope.ValueScoped);
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
    public static implicit operator B<T>(in scoped A<T> a) => default;
}
struct B<T>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (3,7): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     A(scoped ref T t) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(3, 7),
                // (4,12): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     T this[scoped in object o] => default;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(4, 12),
                // (5,45): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static implicit operator B<T>(in scoped A<T> a) => default;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(5, 45));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                VerifyParameterSymbol(comp.GetMember<NamedTypeSymbol>("A").Constructors.Single(c => !c.IsImplicitlyDeclared).Parameters[0], "scoped ref T t", RefKind.Ref, DeclarationScope.RefScoped);
                VerifyParameterSymbol(comp.GetMember<PropertySymbol>("A.this[]").GetMethod.Parameters[0], "scoped in System.Object o", RefKind.In, DeclarationScope.RefScoped);
                VerifyParameterSymbol(comp.GetMember<MethodSymbol>("A.op_Implicit").Parameters[0], "in scoped A<T> a", RefKind.In, DeclarationScope.ValueScoped);
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
        static void L5(object o, ref scoped R x5) { }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (7,24): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         static void L1(scoped R x1) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(7, 24),
                // (8,24): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         static void L2(scoped ref int x2) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(8, 24),
                // (9,24): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         static void L3(scoped in int x3) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(9, 24),
                // (10,24): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         static void L4(scoped out int x4) { x4 = 0; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(10, 24),
                // (11,38): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         static void L5(object o, ref scoped R x5) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(11, 38));
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
                VerifyParameterSymbol(localFunctions[3].Parameters[0], "scoped out System.Int32 x4", RefKind.Out, DeclarationScope.RefScoped);
                VerifyParameterSymbol(localFunctions[4].Parameters[1], "ref scoped R x5", RefKind.Ref, DeclarationScope.ValueScoped);
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
        var f5 = (object o, ref scoped R x5) => { };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (6,19): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var f1 = (scoped R x1) => { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(6, 19),
                // (7,19): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var f2 = (scoped ref int x2) => { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(7, 19),
                // (8,19): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var f3 = (scoped in int x3) => { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(8, 19),
                // (9,19): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var f4 = (scoped out int x4) => { x4 = 0; };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(9, 19),
                // (10,33): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var f5 = (object o, ref scoped R x5) => { };
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(10, 33));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var decls = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().ToArray();
                var lambdas = decls.Select(d => model.GetSymbolInfo(d).Symbol.GetSymbol<LambdaSymbol>()).ToArray();

                VerifyParameterSymbol(lambdas[0].Parameters[0], "scoped R x1", RefKind.None, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(lambdas[1].Parameters[0], "scoped ref System.Int32 x2", RefKind.Ref, DeclarationScope.RefScoped);
                VerifyParameterSymbol(lambdas[2].Parameters[0], "scoped in System.Int32 x3", RefKind.In, DeclarationScope.RefScoped);
                VerifyParameterSymbol(lambdas[3].Parameters[0], "scoped out System.Int32 x4", RefKind.Out, DeclarationScope.RefScoped);
                VerifyParameterSymbol(lambdas[4].Parameters[1], "ref scoped R x5", RefKind.Ref, DeclarationScope.ValueScoped);
            }
        }

        [Fact]
        public void ParameterScope_05()
        {
            var source =
@"ref struct R { }
delegate void D1(scoped R r1);
delegate void D2(scoped ref R r2);
delegate void D3(object o, ref scoped R r3);
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (2,18): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // delegate void D1(scoped R r1);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(2, 18),
                // (3,18): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // delegate void D2(scoped ref R r2);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(3, 18),
                // (4,32): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // delegate void D3(object o, ref scoped R r3);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(4, 32));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                VerifyParameterSymbol(comp.GetMember<NamedTypeSymbol>("D1").DelegateInvokeMethod.Parameters[0], "scoped R r1", RefKind.None, DeclarationScope.ValueScoped);
                VerifyParameterSymbol(comp.GetMember<NamedTypeSymbol>("D2").DelegateInvokeMethod.Parameters[0], "scoped ref R r2", RefKind.Ref, DeclarationScope.RefScoped);
                VerifyParameterSymbol(comp.GetMember<NamedTypeSymbol>("D3").DelegateInvokeMethod.Parameters[1], "ref scoped R r3", RefKind.Ref, DeclarationScope.ValueScoped);
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
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var methods = decls.Select(d => ((FunctionPointerTypeSymbol)model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>().Type).Signature).ToArray();

            VerifyParameterSymbol(methods[0].Parameters[0], "scoped R", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(methods[1].Parameters[0], "ref scoped R", RefKind.Ref, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(methods[1].Parameters[1], "scoped ref System.Int32", RefKind.Ref, DeclarationScope.RefScoped);
        }

        [Fact]
        public void ParameterScope_07()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F1(scoped scoped R r) { }
    static void F2(ref scoped scoped R r) { }
    static void F3(scoped scoped ref R r) { }
}";
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report duplicate modifiers.
            comp.VerifyEmitDiagnostics();
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
        var f = (scoped scoped R r) => { };
    }
}";
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report duplicate modifiers.
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterScope_09()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
        var f = (ref scoped scoped int i) => { };
    }
}";
            var comp = CreateCompilation(source);
            // Duplicate scoped modifiers result are parse errors rather than binding errors.
            comp.VerifyDiagnostics(
                // (6,18): error CS1525: Invalid expression term 'ref'
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(6, 18),
                // (6,18): error CS1073: Unexpected token 'ref'
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(6, 18),
                // (6,29): error CS1026: ) expected
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "scoped").WithLocation(6, 29),
                // (6,29): error CS1002: ; expected
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "scoped").WithLocation(6, 29),
                // (6,36): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "int").WithLocation(6, 36),
                // (6,40): warning CS0168: The variable 'i' is declared but never used
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i").WithArguments("i").WithLocation(6, 40),
                // (6,41): error CS1002: ; expected
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 41),
                // (6,41): error CS1513: } expected
                //         var f = (ref scoped scoped int i) => { };
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 41));
        }

        [Fact]
        public void ParameterScope_10()
        {
            var source =
@"ref struct scoped { }
class Program
{
    static void F1(scoped scoped x, ref scoped y, ref scoped scoped z, scoped ref scoped w) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,12): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // ref struct scoped { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 12));

            var method = comp.GetMember<MethodSymbol>("Program.F1");
            VerifyParameterSymbol(method.Parameters[0], "scoped scoped x", RefKind.None, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(method.Parameters[1], "ref scoped y", RefKind.Ref, DeclarationScope.Unscoped);
            VerifyParameterSymbol(method.Parameters[2], "ref scoped scoped z", RefKind.Ref, DeclarationScope.ValueScoped);
            VerifyParameterSymbol(method.Parameters[3], "scoped ref scoped w", RefKind.Ref, DeclarationScope.RefScoped);
        }

        // PROTOTYPE: Test 'scoped' with 'this'.
        // PROTOTYPE: Test 'scoped' with 'params'.

        // PROTOTYPE: Report error for implicit conversion between delegate types that differ by 'scoped',
        // and between function pointer types and methods that differ by 'scoped'.

        // PROTOTYPE: Test distinct 'scoped' annotations in partial method parts.

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void ReturnTypeScope(LanguageVersion langVersion)
        {
            var source =
@"ref struct R { }
class Program
{
    static scoped R F1<T>() => throw null;
    static scoped ref R F2<T>() => throw null;
    static ref scoped R F3<T>() => throw null;
    static void Main()
    {
#pragma warning disable 8321
        static scoped R L1<T>() => throw null;
        static scoped ref readonly R L2<T>() => throw null;
        static ref readonly scoped R L3<T>() => throw null;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            // PROTOTYPE: Should report errors for 'ref scoped' and 'ref readonly scoped' return types as well.
            comp.VerifyDiagnostics(
                    // (4,21): error CS0106: The modifier 'scoped' is not valid for this item
                    //     static scoped R F1<T>() => throw null;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "F1").WithArguments("scoped").WithLocation(4, 21),
                    // (5,25): error CS0106: The modifier 'scoped' is not valid for this item
                    //     static scoped ref R F2<T>() => throw null;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "F2").WithArguments("scoped").WithLocation(5, 25),
                    // (10,16): error CS0106: The modifier 'scoped' is not valid for this item
                    //         static scoped R L1<T>() => throw null;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "scoped").WithArguments("scoped").WithLocation(10, 16),
                    // (11,16): error CS0106: The modifier 'scoped' is not valid for this item
                    //         static scoped ref readonly R L2<T>() => throw null;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "scoped").WithArguments("scoped").WithLocation(11, 16));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void PropertyValueScope(LanguageVersion langVersion)
        {
            var source =
@"ref struct R1 { }
ref struct R2
{
    scoped R1 P1 { get; }
    scoped R1 P2 { get; init; }
    scoped R1 P3 { set { } }
    ref scoped R1 P4 => throw null;
    scoped ref int P5 => throw null;
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            // PROTOTYPE: Should report error for 'scoped' on P4.
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
                // (8,20): error CS0106: The modifier 'scoped' is not valid for this item
                //     scoped ref int P5 => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P5").WithArguments("scoped").WithLocation(8, 20));
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
            AddParameterOptions(SymbolDisplayParameterOptions.IncludeScoped).
            AddLocalOptions(SymbolDisplayLocalOptions.IncludeRef | SymbolDisplayLocalOptions.IncludeScoped);

        private static void VerifyParameterSymbol(ParameterSymbol parameter, string expectedDisplayString, RefKind expectedRefKind, DeclarationScope expectedScope)
        {
            Assert.Equal(expectedRefKind, parameter.RefKind);
            Assert.Equal(expectedScope, parameter.Scope);
            Assert.Equal(expectedDisplayString, parameter.ToDisplayString(displayFormatWithScoped));

            VerifyParameterSymbol(parameter.GetPublicSymbol(), expectedDisplayString, expectedRefKind, expectedScope);
        }

        private static void VerifyParameterSymbol(IParameterSymbol parameter, string expectedDisplayString, RefKind expectedRefKind, DeclarationScope expectedScope)
        {
            Assert.Equal(expectedRefKind, parameter.RefKind);
            Assert.Equal(expectedScope == DeclarationScope.RefScoped, parameter.IsRefScoped);
            Assert.Equal(expectedScope == DeclarationScope.ValueScoped, parameter.IsValueScoped);
            Assert.Equal(expectedDisplayString, parameter.ToDisplayString(displayFormatWithScoped));
        }

        [Fact]
        public void LocalScope_01()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
        scoped R r1;
        scoped ref R r2 = ref r1;
        ref scoped R r3 = ref r1;
        scoped ref scoped R r4 = ref r1;
        scoped ref readonly R r5 = ref r1;
        ref readonly scoped R r6 = ref r1;
        scoped ref readonly scoped R r7 = ref r1;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         scoped R r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(6, 9),
                // (7,9): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         scoped ref R r2 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(7, 9),
                // (8,13): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         ref scoped R r3 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(8, 13),
                // (9,9): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         scoped ref scoped R r4 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(9, 9),
                // (9,20): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         scoped ref scoped R r4 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(9, 20),
                // (10,9): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         scoped ref readonly R r5 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(10, 9),
                // (11,22): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         ref readonly scoped R r6 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(11, 22),
                // (12,9): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         scoped ref readonly scoped R r7 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(12, 9),
                // (12,29): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         scoped ref readonly scoped R r7 = ref r1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(12, 29));
            verify(comp);

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
                var locals = decls.Select(d => model.GetDeclaredSymbol(d).GetSymbol<LocalSymbol>()).ToArray();

                VerifyLocalSymbol(locals[0], "scoped R r1", RefKind.None, DeclarationScope.ValueScoped);
                VerifyLocalSymbol(locals[1], "scoped ref R r2", RefKind.Ref, DeclarationScope.RefScoped);
                VerifyLocalSymbol(locals[2], "ref scoped R r3", RefKind.Ref, DeclarationScope.ValueScoped);
                VerifyLocalSymbol(locals[3], "ref scoped R r4", RefKind.Ref, DeclarationScope.ValueScoped);
                VerifyLocalSymbol(locals[4], "scoped ref readonly R r5", RefKind.RefReadOnly, DeclarationScope.RefScoped);
                VerifyLocalSymbol(locals[5], "ref readonly scoped R r6", RefKind.RefReadOnly, DeclarationScope.ValueScoped);
                VerifyLocalSymbol(locals[6], "ref readonly scoped R r7", RefKind.RefReadOnly, DeclarationScope.ValueScoped);
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
        ref scoped scoped R y = ref x;
        scoped scoped ref R z = ref x;
    }
}";
            var comp = CreateCompilation(source);
            // Duplicate scoped modifiers result are parse errors rather than binding errors.
            comp.VerifyDiagnostics(
                // (6,16): error CS1031: Type expected
                //         scoped scoped R x = default;
                Diagnostic(ErrorCode.ERR_TypeExpected, "scoped").WithArguments("scoped").WithLocation(6, 16),
                // (7,20): error CS0118: 'scoped' is a variable but is used like a type
                //         ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "scoped").WithArguments("scoped", "variable", "type").WithLocation(7, 20),
                // (7,27): error CS8174: A declaration of a by-reference variable must have an initializer
                //         ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "R").WithLocation(7, 27),
                // (7,27): warning CS0168: The variable 'R' is declared but never used
                //         ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "R").WithArguments("R").WithLocation(7, 27),
                // (7,29): error CS1002: ; expected
                //         ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "y").WithLocation(7, 29),
                // (7,29): error CS0103: The name 'y' does not exist in the current context
                //         ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(7, 29),
                // (8,9): error CS0118: 'scoped' is a variable but is used like a type
                //         scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "scoped").WithArguments("scoped", "variable", "type").WithLocation(8, 9),
                // (8,16): warning CS0168: The variable 'scoped' is declared but never used
                //         scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "scoped").WithArguments("scoped").WithLocation(8, 16),
                // (8,23): error CS1002: ; expected
                //         scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(8, 23));
        }

        [Fact]
        public void LocalScope_03()
        {
            var source =
@"scoped scoped R x = default;
ref scoped scoped R y = ref x;
scoped scoped ref R z = ref x;
ref struct R { }
";
            var comp = CreateCompilation(source);
            // Duplicate scoped modifiers result are parse errors rather than binding errors.
            comp.VerifyDiagnostics(
                // (1,8): error CS1031: Type expected
                // scoped scoped R x = default;
                Diagnostic(ErrorCode.ERR_TypeExpected, "scoped").WithArguments("scoped").WithLocation(1, 8),
                // (1,17): warning CS0219: The variable 'x' is assigned but its value is never used
                // scoped scoped R x = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(1, 17),
                // (2,12): error CS0118: 'scoped' is a variable but is used like a type
                // ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "scoped").WithArguments("scoped", "variable", "type").WithLocation(2, 12),
                // (2,19): error CS8174: A declaration of a by-reference variable must have an initializer
                // ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "R").WithLocation(2, 19),
                // (2,19): warning CS0168: The variable 'R' is declared but never used
                // ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "R").WithArguments("R").WithLocation(2, 19),
                // (2,21): error CS1003: Syntax error, ',' expected
                // ref scoped scoped R y = ref x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "y").WithArguments(",", "").WithLocation(2, 21),
                // (3,1): error CS0118: 'scoped' is a variable but is used like a type
                // scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "scoped").WithArguments("scoped", "variable", "type").WithLocation(3, 1),
                // (3,8): warning CS0168: The variable 'scoped' is declared but never used
                // scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "scoped").WithArguments("scoped").WithLocation(3, 8),
                // (3,15): error CS1003: Syntax error, ',' expected
                // scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",", "ref").WithLocation(3, 15));
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
                // (3,1): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // scoped scoped s3 = default;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(3, 1),
                // (3,15): warning CS0219: The variable 's3' is assigned but its value is never used
                // scoped scoped s3 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s3").WithArguments("s3").WithLocation(3, 15),
                // (4,1): error CS8652: The feature 'ref fields' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // scoped ref scoped s4 = ref s1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "scoped").WithArguments("ref fields").WithLocation(4, 1),
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
        [InlineData(LanguageVersionFacts.CSharpNext)]
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
            Assert.Equal(expectedScope == DeclarationScope.RefScoped, local.IsRefScoped);
            Assert.Equal(expectedScope == DeclarationScope.ValueScoped, local.IsValueScoped);
            Assert.Equal(expectedDisplayString, local.ToDisplayString(displayFormatWithScoped));
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
                // (4,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F1(scoped S s) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped S s").WithLocation(4, 20),
                // (5,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F2(ref scoped S s) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "ref scoped S s").WithLocation(5, 20),
                // (7,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F4(scoped ref scoped S s) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped ref scoped S s").WithLocation(7, 20),
                // (8,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F5(ref scoped int i) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "ref scoped int i").WithLocation(8, 20),
                // (9,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F6(in scoped  int i) { }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "in scoped  int i").WithLocation(9, 20),
                // (10,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     static void F7(out scoped int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "out scoped int i").WithLocation(10, 20));
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
                // (4,16): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     void F1<T>(scoped T t);
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped T t").WithLocation(4, 16),
                // (5,16): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     void F2<T>(scoped T t) where T : class;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped T t").WithLocation(5, 16),
                // (6,16): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //     void F3<T>(scoped T t) where T : struct;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped T t").WithLocation(6, 16),
                // (7,16): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
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
                // (6,43): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         var f = (scoped ref E x, scoped E y) => { };
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "y").WithLocation(6, 43),
                // (8,39): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
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
                // (1,17): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                // delegate void D(scoped C c);
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped C c").WithLocation(1, 17),
                // (6,19): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         delegate*<scoped C, int> d = default;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped C").WithLocation(6, 19));
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
        {refModifier} scoped S s1 = ref s;
        scoped {refModifier} S s2 = ref s;
        scoped {refModifier} scoped S s3 = ref s;
    }}
}}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         ref readonly scoped S s1 = ref s;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "S").WithLocation(6, 29),
                // (8,36): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         scoped ref readonly scoped S s3 = ref s;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "S").WithLocation(8, 36));
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
        ref scoped var x2 = ref x1;
        scoped ref var x3 = ref x1;
        scoped ref scoped var x4 = ref x1;
        scoped var y1 = new S<int>(); // 1
        ref scoped var y2 = ref y1; // 2
        scoped ref var y3 = ref y1;
        scoped ref scoped var y4 = ref y1; // 3
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,16): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         scoped var y1 = new S<int>(); // 1
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "var").WithLocation(11, 16),
                // (12,20): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         ref scoped var y2 = ref y1; // 2
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "var").WithLocation(12, 20),
                // (14,27): error CS8986: The 'scoped' modifier can be used for refs and ref struct values only.
                //         scoped ref scoped var y4 = ref y1; // 3
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "var").WithLocation(14, 27));
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
        ref scoped R<int> z = ref y;
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
  // sequence point: ref scoped R<int> z = ref y;
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

        // PROTOTYPE: Test `const scoped int local = 0;`. Are there other invalid combinations of modifiers?
    }
}
