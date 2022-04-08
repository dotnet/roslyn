// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
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
            Assert.Equal(RefKind.Ref, field.RefKind);
            Assert.Equal(new[] { "System.SByte", "System.Object" }, field.RefCustomModifiers.SelectAsArray(m => m.Modifier.ToTestDisplayString()));
            Assert.Equal("ref modopt(System.SByte) modopt(System.Object) System.Int32 A<System.Int32>.F", field.ToTestDisplayString());
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
            Assert.Equal(RefKind.RefReadOnly, field.RefKind);
            // Currently, source symbols cannot declare RefCustomModifiers. If that
            // changes, update this test to verify retargeting of RefCutomModifiers.
            Assert.Empty(field.RefCustomModifiers);
            Assert.Equal("ref readonly System.Int32 A.F", field.ToTestDisplayString());
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
        public void Assignment_Ref()
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
        F = ref tIn;
        F = tValue;
        F = tRef;
        F = tOut;
        F = tIn;
    }
    object P
    {
        init
        {
            F = ref GetValue();
            F = ref GetRef();
            F = ref GetRefReadonly();
            F = GetValue();
            F = GetRef();
            F = GetRefReadonly();
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}
class Program
{
    static void Assign<T>(S<T> s, T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        s.F =  ref tValue;
        s.F =  ref tRef;
        s.F =  ref tOut;
        s.F =  ref tIn;
        s.F =  tValue;
        s.F =  tRef;
        s.F =  tOut;
        s.F =  tIn;
    }
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //         F = ref tIn;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(10, 17),
                // (20,21): error CS1510: A ref or out value must be an assignable variable
                //             F = ref GetValue();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "GetValue()").WithLocation(20, 21),
                // (22,21): error CS8331: Cannot assign to method 'S<T>.GetRefReadonly()' because it is a readonly variable
                //             F = ref GetRefReadonly();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "GetRefReadonly()").WithArguments("method", "S<T>.GetRefReadonly()").WithLocation(22, 21),
                // (40,20): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //         s.F =  ref tIn;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(40, 20));
        }

        [Fact]
        public void Assignment_RefReadonly()
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
        F = tValue;
        F = tRef;
        F = tOut;
        F = tIn;
    }
    object P
    {
        init
        {
            F = ref GetValue();
            F = ref GetRef();
            F = ref GetRefReadonly();
            F = GetValue();
            F = GetRef();
            F = GetRefReadonly();
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}
class Program
{
    static void Assign<T>(S<T> s, T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        s.F =  ref tValue;
        s.F =  ref tRef;
        s.F =  ref tOut;
        s.F =  ref tIn;
        s.F =  tValue;
        s.F =  tRef;
        s.F =  tOut;
        s.F =  tIn;
    }
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (11,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tValue;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(11, 9),
                // (12,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tRef;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(12, 9),
                // (13,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tOut;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(13, 9),
                // (14,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tIn;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(14, 9),
                // (20,21): error CS1510: A ref or out value must be an assignable variable
                //             F = ref GetValue();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "GetValue()").WithLocation(20, 21),
                // (23,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetValue();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(23, 13),
                // (24,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRef();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(24, 13),
                // (25,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRefReadonly();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(25, 13),
                // (41,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tValue;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(41, 9),
                // (42,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tRef;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(42, 9),
                // (43,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tOut;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(43, 9),
                // (44,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tIn;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(44, 9));
        }

        [Fact]
        public void Assignment_ReadonlyRef()
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
        F = ref tIn;
        F = tValue;
        F = tRef;
        F = tOut;
        F = tIn;
    }
    object P
    {
        init
        {
            F = ref GetValue();
            F = ref GetRef();
            F = ref GetRefReadonly();
            F = GetValue();
            F = GetRef();
            F = GetRefReadonly();
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}
class Program
{
    static void Assign<T>(S<T> s, T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        s.F =  ref tValue;
        s.F =  ref tRef;
        s.F =  ref tOut;
        s.F =  ref tIn;
        s.F =  tValue;
        s.F =  tRef;
        s.F =  tOut;
        s.F =  tIn;
    }
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS8331: Cannot assign to variable 'in T' because it is a readonly variable
                //         F = ref tIn;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "tIn").WithArguments("variable", "in T").WithLocation(10, 17),
                // (20,21): error CS1510: A ref or out value must be an assignable variable
                //             F = ref GetValue();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "GetValue()").WithLocation(20, 21),
                // (22,21): error CS8331: Cannot assign to method 'S<T>.GetRefReadonly()' because it is a readonly variable
                //             F = ref GetRefReadonly();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "GetRefReadonly()").WithArguments("method", "S<T>.GetRefReadonly()").WithLocation(22, 21),
                // (37,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         s.F =  ref tValue;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(37, 9),
                // (38,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         s.F =  ref tRef;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(38, 9),
                // (39,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         s.F =  ref tOut;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(39, 9),
                // (40,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         s.F =  ref tIn;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.F").WithLocation(40, 9));
        }

        [Fact]
        public void Assignment_ReadonlyRefReadonly()
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
        F = tValue;
        F = tRef;
        F = tOut;
        F = tIn;
    }
    object P
    {
        init
        {
            F = ref GetValue();
            F = ref GetRef();
            F = ref GetRefReadonly();
            F = GetValue();
            F = GetRef();
            F = GetRefReadonly();
        }
    }
    static T GetValue() => throw null;
    static ref T GetRef() => throw null;
    static ref readonly T GetRefReadonly() => throw null;
}
class Program
{
    static void Assign<T>(S<T> s, T tValue, ref T tRef, out T tOut, in T tIn)
    {
        tOut = default;
        s.F =  ref tValue;
        s.F =  ref tRef;
        s.F =  ref tOut;
        s.F =  ref tIn;
        s.F =  tValue;
        s.F =  tRef;
        s.F =  tOut;
        s.F =  tIn;
    }
}";
            // PROTOTYPE: Report a ref-safe-to-escape error for: F = ref tValue;
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (11,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tValue;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(11, 9),
                // (12,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tRef;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(12, 9),
                // (13,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tOut;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(13, 9),
                // (14,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         F = tIn;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(14, 9),
                // (20,21): error CS1510: A ref or out value must be an assignable variable
                //             F = ref GetValue();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "GetValue()").WithLocation(20, 21),
                // (23,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetValue();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(23, 13),
                // (24,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRef();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(24, 13),
                // (25,13): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //             F = GetRefReadonly();
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(25, 13),
                // (41,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tValue;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(41, 9),
                // (42,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tRef;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(42, 9),
                // (43,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tOut;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(43, 9),
                // (44,9): error CS8331: Cannot assign to field 'S<T>.F' because it is a readonly variable
                //         s.F =  tIn;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(44, 9));
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
                // (12,42): error CS8334: Members of variable 'in S<T>' cannot be returned by writable reference because it is a readonly variable
                //     static ref T F6<T>(in S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "s.F").WithArguments("variable", "in S<T>").WithLocation(12, 42),
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
                // (12,42): error CS8334: Members of variable 'in S<T>' cannot be returned by writable reference because it is a readonly variable
                //     static ref T F6<T>(in S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "s.F").WithArguments("variable", "in S<T>").WithLocation(12, 42),
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
        public void RefParameter_Ref()
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
    public static void M1(T t) { }
    public static void M2(ref T t) { }
    public static void M3(out T t) { t = default; }
    public static void M4(in T t) { }
}
class Program
{
    static void M<T>()
    {
        var s = new S<T>();
        S<T>.M1(s.F);
        S<T>.M2(ref s.F);
        S<T>.M3(out s.F);
        S<T>.M4(s.F);
        S<T>.M4(in s.F);
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefParameter_RefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public ref readonly T F;
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
    public static void M1(T t) { }
    public static void M2(ref T t) { }
    public static void M3(out T t) { t = default; }
    public static void M4(in T t) { }
}
class Program
{
    static void M<T>()
    {
        var s = new S<T>();
        S<T>.M1(s.F);
        S<T>.M2(ref s.F);
        S<T>.M3(out s.F);
        S<T>.M4(s.F);
        S<T>.M4(in s.F);
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(8, 16),
                // (9,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(9, 16),
                // (18,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M2(ref F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(18, 20),
                // (19,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M3(out F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(19, 20),
                // (27,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(27, 16),
                // (28,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(28, 16),
                // (43,21): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         S<T>.M2(ref s.F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(43, 21),
                // (44,21): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         S<T>.M3(out s.F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(44, 21));
        }

        [Fact]
        public void RefParameter_ReadonlyRef()
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
    public static void M1(T t) { }
    public static void M2(ref T t) { }
    public static void M3(out T t) { t = default; }
    public static void M4(in T t) { }
}
class Program
{
    static void M<T>()
    {
        var s = new S<T>();
        S<T>.M1(s.F);
        S<T>.M2(ref s.F);
        S<T>.M3(out s.F);
        S<T>.M4(s.F);
        S<T>.M4(in s.F);
    }
}";
            // PROTOTYPE: Execute code and verify IL for each of the cases.
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefParameter_ReadonlyRefReadonly()
        {
            var source =
@"ref struct S<T>
{
    public readonly ref readonly T F;
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
    public static void M1(T t) { }
    public static void M2(ref T t) { }
    public static void M3(out T t) { t = default; }
    public static void M4(in T t) { }
}
class Program
{
    static void M<T>()
    {
        var s = new S<T>();
        S<T>.M1(s.F);
        S<T>.M2(ref s.F);
        S<T>.M3(out s.F);
        S<T>.M4(s.F);
        S<T>.M4(in s.F);
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(8, 16),
                // (9,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(9, 16),
                // (18,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M2(ref F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(18, 20),
                // (19,20): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //             M3(out F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(19, 20),
                // (27,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M2(ref F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(27, 16),
                // (28,16): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         M3(out F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F").WithArguments("field", "S<T>.F").WithLocation(28, 16),
                // (43,21): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         S<T>.M2(ref s.F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(43, 21),
                // (44,21): error CS8329: Cannot use field 'S<T>.F' as a ref or out value because it is a readonly variable
                //         S<T>.M3(out s.F);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s.F").WithArguments("field", "S<T>.F").WithLocation(44, 21));
        }

        [Fact]
        public void AssignToReadonlyStruct()
        {
            var source =
@"ref struct S<T>
{
    public ref T F1;
    public ref readonly T F2;
    public readonly ref T F3;
    public readonly ref readonly T F4;
}
class Program
{
    static void M<T>(in S<T> s, T t)
    {
        s.F1 = t;
        s.F2 = t;
        s.F3 = t;
        s.F4 = t;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,9): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //         s.F1 = t;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "s.F1").WithArguments("variable", "in S<T>").WithLocation(12, 9),
                // (13,9): error CS8331: Cannot assign to field 'S<T>.F2' because it is a readonly variable
                //         s.F2 = t;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F2").WithArguments("field", "S<T>.F2").WithLocation(13, 9),
                // (14,9): error CS8332: Cannot assign to a member of variable 'in S<T>' because it is a readonly variable
                //         s.F3 = t;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "s.F3").WithArguments("variable", "in S<T>").WithLocation(14, 9),
                // (15,9): error CS8331: Cannot assign to field 'S<T>.F4' because it is a readonly variable
                //         s.F4 = t;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "s.F4").WithArguments("field", "S<T>.F4").WithLocation(15, 9));
        }

        [Fact]
        public void RefAssignFromReadonlyStruct()
        {
            var source =
@"ref struct S<T>
{
    public ref T F1;
    public ref readonly T F2;
    public readonly ref T F3;
    public readonly ref readonly T F4;
}
class Program
{
    static void M1<T>(in S<T> s1)
    {
        ref T t1 = ref s1.F1;
        ref T t2 = ref s1.F2;
        ref T t3 = ref s1.F3;
        ref T t4 = ref s1.F4;
    }
    static void M2<T>(in S<T> s2)
    {
        ref readonly T t1 = ref s2.F1;
        ref readonly T t2 = ref s2.F2;
        ref readonly T t3 = ref s2.F3;
        ref readonly T t4 = ref s2.F4;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,24): error CS8330: Members of variable 'in S<T>' cannot be used as a ref or out value because it is a readonly variable
                //         ref T t1 = ref s1.F1;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "s1.F1").WithArguments("variable", "in S<T>").WithLocation(12, 24),
                // (13,24): error CS8329: Cannot use field 'S<T>.F2' as a ref or out value because it is a readonly variable
                //         ref T t2 = ref s1.F2;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s1.F2").WithArguments("field", "S<T>.F2").WithLocation(13, 24),
                // (14,24): error CS8330: Members of variable 'in S<T>' cannot be used as a ref or out value because it is a readonly variable
                //         ref T t3 = ref s1.F3;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "s1.F3").WithArguments("variable", "in S<T>").WithLocation(14, 24),
                // (15,24): error CS8329: Cannot use field 'S<T>.F4' as a ref or out value because it is a readonly variable
                //         ref T t4 = ref s1.F4;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "s1.F4").WithArguments("field", "S<T>.F4").WithLocation(15, 24));
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
    static T ReadIn<T>(in R2<T> r2)
    {
        return r2.R1.F;
    }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(
@"42
42
42
"));
            verifier.VerifyIL("Program.Read<T>",
@"{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""ref R1<T> R2<T>.R1""
  IL_0006:  ldobj      ""R1<T>""
  IL_000b:  ldfld      ""ref T R1<T>.F""
  IL_0010:  ldobj      ""T""
  IL_0015:  ret
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
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (17,49): error CS8334: Members of variable 'in S<T>' cannot be returned by writable reference because it is a readonly variable
                //     static ref T RefReturn<T>(in S<T> s) => ref s.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "s.F").WithArguments("variable", "in S<T>").WithLocation(17, 49));
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
            // PROTOTYPE: Change text of ERR_RefLocalOrParamExpected to "The left-hand side of a ref assignment must be a ref variable."
            comp.VerifyEmitDiagnostics(
                // (4,18): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref T P { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P").WithArguments("S<T>.P").WithLocation(4, 18),
                // (5,27): error CS8145: Auto-implemented properties cannot return by reference
                //     public ref readonly T Q { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "Q").WithArguments("S<T>.Q").WithLocation(5, 27),
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref local or parameter.
                //         P = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P").WithLocation(8, 9),
                // (8,9): error CS8079: Use of possibly unassigned auto-implemented property 'P'
                //         P = ref t;
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "P").WithArguments("P").WithLocation(8, 9),
                // (9,9): error CS8373: The left-hand side of a ref assignment must be a ref local or parameter.
                //         Q = ref t;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "Q").WithLocation(9, 9),
                // (9,9): error CS8079: Use of possibly unassigned auto-implemented property 'Q'
                //         Q = ref t;
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "Q").WithArguments("Q").WithLocation(9, 9),
                // (26,9): error CS8373: The left-hand side of a ref assignment must be a ref local or parameter.
                //         s.P = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "s.P").WithLocation(26, 9),
                // (27,9): error CS8373: The left-hand side of a ref assignment must be a ref local or parameter.
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
