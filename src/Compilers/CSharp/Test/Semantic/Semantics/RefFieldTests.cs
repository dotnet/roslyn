// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
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
            // PROTOTYPE: Enable.
#if RuntimeSupport
            return expectedOutput;
#else
            return null;
#endif
        }

        [CombinatorialData]
        [Theory]
        public void LanguageVersion(bool useCompilationReference)
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

            verifyField(comp.GetMember<FieldSymbol>("S.F1"), RefKind.Ref, "ref T S<T>.F1");
            verifyField(comp.GetMember<FieldSymbol>("S.F2"), RefKind.RefReadOnly, "ref readonly T S<T>.F2");

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularNext);
            comp.VerifyEmitDiagnostics();

            verifyField(comp.GetMember<FieldSymbol>("S.F1"), RefKind.Ref, "ref T S<T>.F1");
            verifyField(comp.GetMember<FieldSymbol>("S.F2"), RefKind.RefReadOnly, "ref readonly T S<T>.F2");

            static void verifyField(FieldSymbol field, RefKind refKind, string displayName)
            {
                Assert.Equal(refKind, field.RefKind);
                Assert.Equal(displayName, field.ToTestDisplayString());
                Assert.Null(field.GetUseSiteDiagnostic());
            }
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

            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.Ref, "ref T S<T>.F");

            comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);
            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.Ref, "ref T S<T>.F");

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

            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.Ref, "ref T S<T>.F");

            var verifier = CompileAndVerify(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularNext, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"2
2
3
3
"));
            comp = (CSharpCompilation)verifier.Compilation;
            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.Ref, "ref T S<T>.F");

            static void verifyField(FieldSymbol field, RefKind refKind, string displayName)
            {
                Assert.Equal(refKind, field.RefKind);
                Assert.Equal(displayName, field.ToTestDisplayString());
            }
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

            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.RefReadOnly, "ref readonly T S<T>.F");

            comp = CreateCompilation(sourceA);
            comp.VerifyEmitDiagnostics();
            var refA = AsReference(comp, useCompilationReference);
            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.RefReadOnly, "ref readonly T S<T>.F");

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

            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.RefReadOnly, "ref readonly T S<T>.F");

            var verifier = CompileAndVerify(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularNext, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"2
2
3
3
"));
            comp = (CSharpCompilation)verifier.Compilation;
            verifyField(comp.GetMember<FieldSymbol>("S.F"), RefKind.RefReadOnly, "ref readonly T S<T>.F");

            static void verifyField(FieldSymbol field, RefKind refKind, string displayName)
            {
                Assert.Equal(refKind, field.RefKind);
                Assert.Equal(displayName, field.ToTestDisplayString());
            }
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
    static void Main()
    {
        int i = 0;
        new S1<int>().F = ref i;
        new S2<int>().F = ref i;
        new S3<int>().F = ref i;
        new S4<int>().F = ref i;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,12): error CS0171: Field 'S2<T>.F' must be fully assigned before control is returned to the caller
                //     public S2(ref T t) { }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S2").WithArguments("S2<T>.F").WithLocation(8, 12));
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
                // (7,9): error CS0170: Use of possibly unassigned field 'F'
                //         F = tValue; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "F").WithArguments("F").WithLocation(7, 9),
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
                // (7,9): error CS0170: Use of possibly unassigned field 'F'
                //         F = tValue; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "F").WithArguments("F").WithLocation(7, 9),
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
@"ref struct S<T>
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

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = tValue; }
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = tOut; }
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = tIn; }

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; }
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; }
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; }
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
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
@"ref struct S<T>
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

    static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = tValue; }
    static void AssignRefToOut<T>(out S<T> sOut, ref T tRef) { sOut = default; sOut.F = tRef; }
    static void AssignOutToOut<T>(out S<T> sOut, out T tOut) { sOut = default; tOut = default; sOut.F = tOut; }
    static void AssignInToOut<T>(out S<T> sOut, in T tIn)    { sOut = default; sOut.F = tIn; }

    static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = tValue; }
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = tRef; }
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = tOut; }
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = tIn; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
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
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; }
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; }
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 7
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
                // (24,61): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 6
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sIn.F = ref tValue").WithArguments("F", "tValue").WithLocation(24, 61),
                // (27,73): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //     static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; } // 7
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(27, 73));
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
    static void AssignRefToIn<T>(in S<T> sIn, ref T tRef) { sIn.F = ref tRef; }
    static void AssignOutToIn<T>(in S<T> sIn, out T tOut) { tOut = default; sIn.F = ref tOut; }
    static void AssignInToIn<T>(in S<T> sIn, in T tIn)    { sIn.F = ref tIn; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,64): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToRef<T>(ref S<T> sRef, T tValue) { sRef.F = ref tValue; } // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sRef.F = ref tValue").WithArguments("F", "tValue").WithLocation(14, 64),
                // (19,80): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToOut<T>(out S<T> sOut, T tValue) { sOut = default; sOut.F = ref tValue; } // 2
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sOut.F = ref tValue").WithArguments("F", "tValue").WithLocation(19, 80),
                // (24,61): error CS8374: Cannot ref-assign 'tValue' to 'F' because 'tValue' has a narrower escape scope than 'F'.
                //     static void AssignValueToIn<T>(in S<T> sIn, T tValue) { sIn.F = ref tValue; } // 3
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "sIn.F = ref tValue").WithArguments("F", "tValue").WithLocation(24, 61));
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
        public void AssignOutFrom_RefField()
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
        public void AssignRefFrom_RefField()
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
        public void AssignRefReadonlyFrom_RefField()
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
        public void ReadWriteFieldWithTemp_01()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
}
class Program
{
    static void Main()
    {
        int i = 42;
        Console.WriteLine(ReadWrite(ref i));
    }
    static T ReadWrite<T>(ref T t)
    {
        return new S<T>().F = ref t;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(@"42"));
            verifier.VerifyIL("Program.ReadWrite<T>",
@"{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (S<T> V_0,
                T& V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  dup
  IL_0003:  initobj    ""S<T>""
  IL_0009:  ldarg.0
  IL_000a:  dup
  IL_000b:  stloc.1
  IL_000c:  stfld      ""ref T S<T>.F""
  IL_0011:  ldloc.1
  IL_0012:  ldobj      ""T""
  IL_0017:  ret
}");
        }

        [Fact]
        public void ReadWriteFieldWithTemp_02()
        {
            var source =
@"using System;
ref struct S<T>
{
    public ref T F;
    private int _other;
    public S(int other) : this() { _other = other; }
}
class Program
{
    static void Main()
    {
        int i = 42;
        Console.WriteLine(ReadWrite(ref i));
    }
    static T ReadWrite<T>(ref T t)
    {
        return new S<T>(1).F = ref t;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(@"42"));
            verifier.VerifyIL("Program.ReadWrite<T>",
@"{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (S<T> V_0,
                T& V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""S<T>..ctor(int)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldarg.0
  IL_000a:  dup
  IL_000b:  stloc.1
  IL_000c:  stfld      ""ref T S<T>.F""
  IL_0011:  ldloc.1
  IL_0012:  ldobj      ""T""
  IL_0017:  ret
}");
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
        ReadAndDiscard(ref i);
        ReadAndDiscardNoArg<int>();
    }
    static void ReadAndDiscard<T>(ref T t)
    {
        _ = new S<T>(ref t).F;
    }
    static void ReadAndDiscardNoArg<T>()
    {
        _ = new S<T>().F;
    }
}";
            // PROTOTYPE: The dereference of `new S<T>(...).F` should not be elided
            // since the behavior may be observable as a NullReferenceException.
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(""));
            verifier.VerifyIL("Program.ReadAndDiscard<T>",
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
34
34
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
                // (8,9): error CS8079: Use of possibly unassigned auto-implemented property 'P'
                //         P = ref t;
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "P").WithArguments("P").WithLocation(8, 9),
                // (9,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         Q = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "Q").WithLocation(9, 9),
                // (9,9): error CS8079: Use of possibly unassigned auto-implemented property 'Q'
                //         Q = ref t;
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "Q").WithArguments("Q").WithLocation(9, 9),
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
    }
}
