// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StructConstructorTests : CSharpTestBase
    {
        [CombinatorialData]
        [Theory]
        public void PublicParameterlessConstructor(bool useCompilationReference)
        {
            var sourceA =
@"public struct S
{
    public readonly bool Initialized;
    public S() { Initialized = true; }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public S() { Initialized = true; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "S").WithArguments("parameterless struct constructors").WithLocation(4, 12));

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"using System;
class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static T CreateStruct<T>() where T : struct => new T();
    static void Main()
    {
        Console.WriteLine(new S().Initialized);
        Console.WriteLine(CreateNew<S>().Initialized);
        Console.WriteLine(CreateStruct<S>().Initialized);
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, expectedOutput:
$@"True
True
{ExecutionConditionUtil.IsCoreClr}"); // Desktop framework ignores constructor in Activator.CreateInstance<T>() where T : struct.
        }

        [Fact]
        public void NonPublicParameterlessConstructor_01()
        {
            var source =
@"public struct A0 { public A0() { } }
public struct A1 { internal A1() { } }
public struct A2 { private A2() { } }
public struct A3 { A3() { } }

internal struct B0 { public B0() { } }
internal struct B1 { internal B1() { } }
internal struct B2 { private B2() { } }
internal struct B3 { B3() { } }

public class C
{
    internal protected struct C0 { public C0() { } }
    internal protected struct C1 { internal C1() { } }
    internal protected struct C2 { private C2() { } }
    internal protected struct C3 { C3() { } }

    protected struct D0 { public D0() { } }
    protected struct D1 { internal D1() { } }
    protected struct D2 { private D2() { } }
    protected struct D3 { D3() { } }

    private protected struct E0 { public E0() { } }
    private protected struct E1 { internal E1() { } }
    private protected struct E2 { private E2() { } }
    private protected struct E3 { E3() { } }

    private struct F0 { public F0() { } }
    private struct F1 { internal F1() { } }
    private struct F2 { private F2() { } }
    private struct F3 { F3() { } }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (2,29): error CS8912: The parameterless struct constructor must be 'public'.
                // public struct A1 { internal A1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "A1").WithLocation(2, 29),
                // (3,28): error CS8912: The parameterless struct constructor must be 'public'.
                // public struct A2 { private A2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "A2").WithLocation(3, 28),
                // (4,20): error CS8912: The parameterless struct constructor must be 'public'.
                // public struct A3 { A3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "A3").WithLocation(4, 20),
                // (7,31): error CS8912: The parameterless struct constructor must be 'public'.
                // internal struct B1 { internal B1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "B1").WithLocation(7, 31),
                // (8,30): error CS8912: The parameterless struct constructor must be 'public'.
                // internal struct B2 { private B2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "B2").WithLocation(8, 30),
                // (9,22): error CS8912: The parameterless struct constructor must be 'public'.
                // internal struct B3 { B3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "B3").WithLocation(9, 22),
                // (14,45): error CS8912: The parameterless struct constructor must be 'public'.
                //     internal protected struct C1 { internal C1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "C1").WithLocation(14, 45),
                // (15,44): error CS8912: The parameterless struct constructor must be 'public'.
                //     internal protected struct C2 { private C2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "C2").WithLocation(15, 44),
                // (16,36): error CS8912: The parameterless struct constructor must be 'public'.
                //     internal protected struct C3 { C3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "C3").WithLocation(16, 36),
                // (19,36): error CS8912: The parameterless struct constructor must be 'public'.
                //     protected struct D1 { internal D1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "D1").WithLocation(19, 36),
                // (20,35): error CS8912: The parameterless struct constructor must be 'public'.
                //     protected struct D2 { private D2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "D2").WithLocation(20, 35),
                // (21,27): error CS8912: The parameterless struct constructor must be 'public'.
                //     protected struct D3 { D3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "D3").WithLocation(21, 27),
                // (24,44): error CS8912: The parameterless struct constructor must be 'public'.
                //     private protected struct E1 { internal E1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "E1").WithLocation(24, 44),
                // (25,43): error CS8912: The parameterless struct constructor must be 'public'.
                //     private protected struct E2 { private E2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "E2").WithLocation(25, 43),
                // (26,35): error CS8912: The parameterless struct constructor must be 'public'.
                //     private protected struct E3 { E3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "E3").WithLocation(26, 35),
                // (29,34): error CS8912: The parameterless struct constructor must be 'public'.
                //     private struct F1 { internal F1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "F1").WithLocation(29, 34),
                // (30,33): error CS8912: The parameterless struct constructor must be 'public'.
                //     private struct F2 { private F2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "F2").WithLocation(30, 33),
                // (31,25): error CS8912: The parameterless struct constructor must be 'public'.
                //     private struct F3 { F3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "F3").WithLocation(31, 25));
        }

        [InlineData("assembly")]
        [InlineData("private")]
        [Theory]
        public void NonPublicParameterlessConstructor_02(string accessibility)
        {
            var sourceA =
$@".class public sealed S extends [mscorlib]System.ValueType
{{
    .method {accessibility} hidebysig specialname rtspecialname instance void .ctor() cil managed {{ ret }}
}}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static T CreateStruct1<T>() where T : struct => new T();
    static T CreateStruct2<T>() where T : struct => CreateNew<T>();
    static string Invoke(Func<object> f)
    {
        object obj;
        try
        {
            obj = f();
        }
        catch (Exception e)
        {
            obj = e;
        }
        return obj.GetType().FullName;
    }
    static void Main()
    {
        Console.WriteLine(Invoke(() => new S()));
        Console.WriteLine(Invoke(() => CreateNew<S>()));
        Console.WriteLine(Invoke(() => CreateStruct1<S>()));
        Console.WriteLine(Invoke(() => CreateStruct2<S>()));
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, expectedOutput:
@"S
System.MissingMethodException
System.MissingMethodException
System.MissingMethodException");
        }

        [InlineData("internal")]
        [InlineData("private")]
        [Theory]
        public void NonPublicParameterlessConstructor_03(string accessibility)
        {
            var sourceA =
$@"public struct S
{{
    {accessibility} S() {{ }}
}}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            var refA = comp.ToMetadataReference();

            var sourceB =
@"class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static T CreateStruct<T>() where T : struct => new T();
    static void Main()
    {
        _ = new S();
        _ = CreateNew<S>();
        _ = CreateStruct<S>();
    }
}";
            var expectedDiagnostics = new[]
            {
                // (7,17): error CS0122: 'S.S()' is inaccessible due to its protection level
                //         _ = new S();
                Diagnostic(ErrorCode.ERR_BadAccess, "S").WithArguments("S.S()").WithLocation(7, 17),
                // (8,13): error CS0310: 'S' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Program.CreateNew<T>()'
                //         _ = CreateNew<S>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "CreateNew<S>").WithArguments("Program.CreateNew<T>()", "T", "S").WithLocation(8, 13),
            };

            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private")]
        [InlineData("private protected")]
        [Theory]
        public void PublicConstructorPrivateStruct_NewConstraint(string accessibility)
        {
            var sourceA =
$@"partial class Program
{{
    {accessibility} struct S
    {{
        public readonly bool Initialized;
        public S()
        {{
            Initialized = true;
        }}
    }}    
}}";
            var sourceB =
@"using System;
partial class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static void Main()
    {
        Console.WriteLine(CreateNew<S>().Initialized);
    }
}";
            CompileAndVerify(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview, expectedOutput: "True");
        }

        [Fact]
        public void ThisInitializer_01()
        {
            var source =
@"using System;
struct S1
{
    internal bool Initialized;
    public S1() { Initialized = true; }
    internal S1(object obj) : this() { }
}
struct S2
{
    internal bool Initialized;
    public S2() : this(null) { }
    S2(object obj) { Initialized = true; }
}
class Program
{
    static void Main()
    {
        Console.WriteLine($""{new S1(null).Initialized}, {new S2().Initialized}"");
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "True, True");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""bool S1.Initialized""
  IL_0007:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""S2..ctor(object)""
  IL_0007:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""bool S2.Initialized""
  IL_0007:  ret
}");
        }

        [Fact]
        public void ThisInitializer_02()
        {
            var source =
@"using System;
struct S1
{
    internal bool Initialized;
    public S1() { Initialized = false; }
    internal S1(object obj) : this() { Initialized = true; }
}
struct S2
{
    internal bool Initialized;
    public S2() : this(null) { Initialized = true; }
    S2(object obj) { Initialized = false; }
}
class Program
{
    static void Main()
    {
        Console.WriteLine($""{new S1(null).Initialized}, {new S2().Initialized}"");
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "True, True");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""bool S1.Initialized""
  IL_0007:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      ""bool S1.Initialized""
  IL_000d:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""S2..ctor(object)""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""bool S2.Initialized""
  IL_000e:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""bool S2.Initialized""
  IL_0007:  ret
}");
        }

        [Fact]
        public void ThisInitializer_03()
        {
            var source =
@"using System;
struct S0
{
    internal bool Initialized = true;
    internal S0(object obj) : this() { }
}
struct S1
{
    internal bool Initialized = true;
    public S1() { }
    internal S1(object obj) : this() { }
}
struct S2
{
    internal bool Initialized = true;
    public S2() : this(null) { }
    S2(object obj) { }
}
class Program
{
    static void Main()
    {
        Console.WriteLine($""{new S0().Initialized}, {new S0(null).Initialized}, {new S1(null).Initialized}, {new S2().Initialized}"");
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "True, True, True, True");
            verifier.VerifyIL("S0..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""bool S0.Initialized""
  IL_0007:  ret
}");
            verifier.VerifyIL("S0..ctor(object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S0..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""bool S1.Initialized""
  IL_0007:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""S2..ctor(object)""
  IL_0007:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""bool S2.Initialized""
  IL_0007:  ret
}");
        }

        [Fact]
        public void ThisInitializer_04()
        {
            var source =
@"using System;
struct S0
{
    internal bool Initialized = false;
    internal S0(object obj) : this() { Initialized = true; }
}
struct S1
{
    internal bool Initialized = false;
    public S1() { }
    internal S1(object obj) : this() { Initialized = true; }
}
struct S2
{
    internal bool Initialized = false;
    public S2() : this(null) { Initialized = true; }
    S2(object obj) { }
}
class Program
{
    static void Main()
    {
        Console.WriteLine($""{new S0().Initialized}, {new S0(null).Initialized}, {new S1(null).Initialized}, {new S2().Initialized}"");
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "False, True, True, True");
            verifier.VerifyIL("S0..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""bool S0.Initialized""
  IL_0007:  ret
}");
            verifier.VerifyIL("S0..ctor(object)",
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S0..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      ""bool S0.Initialized""
  IL_000d:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""bool S1.Initialized""
  IL_0007:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      ""bool S1.Initialized""
  IL_000d:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""S2..ctor(object)""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""bool S2.Initialized""
  IL_000e:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""bool S2.Initialized""
  IL_0007:  ret
}");
        }

        [Fact]
        public void FieldInitializers_None()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S0
{
    object X;
    object Y;
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
struct S1
{
    object X;
    object Y;
    public S1() { Y = 1; }
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
struct S2
{
    object X;
    object Y;
    public S2(object y) { Y = y; }
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S0());
        Console.WriteLine(new S1());
        Console.WriteLine(new S2());
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (13,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "S1").WithArguments("parameterless struct constructors").WithLocation(13, 12),
                // (13,12): error CS0171: Field 'S1.X' must be fully assigned before control is returned to the caller
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.X").WithLocation(13, 12),
                // (20,12): error CS0171: Field 'S2.X' must be fully assigned before control is returned to the caller
                //     public S2(object y) { Y = y; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S2").WithArguments("S2.X").WithLocation(20, 12));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (13,12): error CS0171: Field 'S1.X' must be fully assigned before control is returned to the caller
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.X").WithLocation(13, 12),
                // (20,12): error CS0171: Field 'S2.X' must be fully assigned before control is returned to the caller
                //     public S2(object y) { Y = y; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S2").WithArguments("S2.X").WithLocation(20, 12));
        }

        [Fact]
        public void FieldInitializers_01()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S0
{
    object X = null;
    object Y;
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
struct S1
{
    object X = null;
    object Y;
    public S1() { Y = 1; }
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
struct S2
{
    object X;
    object Y = null;
    public S2(object x) { X = x; }
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S0());
        Console.WriteLine(new S1());
        Console.WriteLine(new S2());
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     object X = null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("parameterless struct constructors").WithLocation(5, 12),
                // (11,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     object X = null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("parameterless struct constructors").WithLocation(11, 12),
                // (13,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "S1").WithArguments("parameterless struct constructors").WithLocation(13, 12),
                // (19,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     object Y = null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "Y").WithArguments("parameterless struct constructors").WithLocation(19, 12));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"X = , Y = 
X = , Y = 1
X = , Y = ");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       46 (0x2e)
  .maxstack  1
  IL_0000:  newobj     ""S0..ctor()""
  IL_0005:  box        ""S0""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  newobj     ""S1..ctor()""
  IL_0014:  box        ""S1""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  newobj     ""S2..ctor()""
  IL_0023:  box        ""S2""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ret
}");
            verifier.VerifyIL("S0..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S0.X""
  IL_0007:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S1.X""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  box        ""int""
  IL_000e:  stfld      ""object S1.Y""
  IL_0013:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S2.Y""
  IL_0007:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S2.Y""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""object S2.X""
  IL_000e:  ret
}");
        }

        [Fact]
        public void FieldInitializers_02()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S0
{
    object X = 0;
    object Y;
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
struct S1
{
    object X = 1;
    object Y;
    public S1() { Y = 1; }
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
struct S2
{
    object X;
    object Y = 2;
    public S2(object x) { X = x; }
    public override string ToString() => string.Format(""X = {0}, Y = {1}"", X, Y);
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S0());
        Console.WriteLine(new S1());
        Console.WriteLine(new S2());
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     object X = 0;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("parameterless struct constructors").WithLocation(5, 12),
                // (11,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     object X = 1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("parameterless struct constructors").WithLocation(11, 12),
                // (13,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "S1").WithArguments("parameterless struct constructors").WithLocation(13, 12),
                // (19,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     object Y = 2;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "Y").WithArguments("parameterless struct constructors").WithLocation(19, 12));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"X = 0, Y = 
X = 1, Y = 1
X = , Y = 2");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       46 (0x2e)
  .maxstack  1
  IL_0000:  newobj     ""S0..ctor()""
  IL_0005:  box        ""S0""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  newobj     ""S1..ctor()""
  IL_0014:  box        ""S1""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  newobj     ""S2..ctor()""
  IL_0023:  box        ""S2""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ret
}");
            verifier.VerifyIL("S0..ctor()",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S0.X""
  IL_000c:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S1.X""
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.1
  IL_000e:  box        ""int""
  IL_0013:  stfld      ""object S1.Y""
  IL_0018:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S2.Y""
  IL_000c:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S2.Y""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.1
  IL_000e:  stfld      ""object S2.X""
  IL_0013:  ret
}");
        }

        [Fact]
        public void NullableAnalysis_01()
        {
            var source =
@"#pragma warning disable 169
#nullable enable
struct S0
{
    object F0;
}
struct S1
{
    object F1;
    public S1() { }
}
struct S2
{
    object F2;
    public S2() : this(null) { }
    S2(object? obj) { }
}
struct S3
{
    object F3;
    public S3() { F3 = GetValue(); }
    static object? GetValue() => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,12): error CS0171: Field 'S1.F1' must be fully assigned before control is returned to the caller
                //     public S1() { }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.F1").WithLocation(10, 12),
                // (16,5): error CS0171: Field 'S2.F2' must be fully assigned before control is returned to the caller
                //     S2(object? obj) { }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S2").WithArguments("S2.F2").WithLocation(16, 5),
                // (21,12): warning CS8618: Non-nullable field 'F3' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S3() { F3 = GetValue(); }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S3").WithArguments("field", "F3").WithLocation(21, 12),
                // (21,24): warning CS8601: Possible null reference assignment.
                //     public S3() { F3 = GetValue(); }
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "GetValue()").WithLocation(21, 24));
        }

        [Fact]
        public void NullableAnalysis_02()
        {
            var source =
@"#nullable enable
struct S0
{
    object F0 = Utils.GetValue();
}
struct S1
{
    object F1 = Utils.GetValue();
    public S1() { }
}
struct S2
{
    object F2 = Utils.GetValue();
    public S2() : this(null) { }
    S2(object obj) { }
}
static class Utils
{
    internal static object? GetValue() => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,17): warning CS8601: Possible null reference assignment.
                //     object F0 = Utils.GetValue();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "Utils.GetValue()").WithLocation(4, 17),
                // (8,17): warning CS8601: Possible null reference assignment.
                //     object F1 = Utils.GetValue();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "Utils.GetValue()").WithLocation(8, 17),
                // (13,17): warning CS8601: Possible null reference assignment.
                //     object F2 = Utils.GetValue();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "Utils.GetValue()").WithLocation(13, 17),
                // (14,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     public S2() : this(null) { }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(14, 24));
        }

        [Fact]
        public void DefiniteAssignment()
        {
            var source =
@"#pragma warning disable 169
unsafe struct S0
{
    fixed int Y[1];
}
unsafe struct S1
{
    fixed int Y[1];
    public S1() { }
}
unsafe struct S2
{
    int X;
    fixed int Y[1];
}
unsafe struct S3
{
    int X;
    fixed int Y[1];
    public S3() { }
}
unsafe struct S4
{
    int X = 4;
    fixed int Y[1];
}
unsafe struct S5
{
    int X;
    fixed int Y[1];
    public S5() { X = 5; }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (20,12): error CS0171: Field 'S3.X' must be fully assigned before control is returned to the caller
                //     public S3() { }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S3").WithArguments("S3.X").WithLocation(20, 12),
                // (24,9): warning CS0414: The field 'S4.X' is assigned but its value is never used
                //     int X = 4;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "X").WithArguments("S4.X").WithLocation(24, 9),
                // (29,9): warning CS0414: The field 'S5.X' is assigned but its value is never used
                //     int X;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "X").WithArguments("S5.X").WithLocation(29, 9));
        }

        [Fact]
        public void ParameterDefaultValues()
        {
            var sourceA =
@"struct S0 { }
struct S1 { object F1 = 1; }
struct S2 { public S2() { } }
struct S3 { internal S3() { } }
struct S4 { private S4() { } }
";
            var sourceB1 =
@"class Program
{
    static void F0(S0 s = default) { }
    static void F1(S1 s = default) { }
    static void F2(S2 s = default) { }
    static void F3(S3 s = default) { }
    static void F4(S4 s = default) { }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,22): error CS8912: The parameterless struct constructor must be 'public'.
                // struct S3 { internal S3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S3").WithLocation(4, 22),
                // (5,21): error CS8912: The parameterless struct constructor must be 'public'.
                // struct S4 { private S4() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S4").WithLocation(5, 21));

            var sourceB2 =
@"class Program
{
    static void F0(S0 s = new()) { }
    static void F1(S1 s = new()) { }
    static void F2(S2 s = new()) { }
    static void F3(S3 s = new()) { }
    static void F4(S4 s = new()) { }
}";
            comp = CreateCompilation(new[] { sourceA, sourceB2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,22): error CS8912: The parameterless struct constructor must be 'public'.
                // struct S3 { internal S3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S3").WithLocation(4, 22),
                // (4,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void F1(S1 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(4, 27),
                // (5,21): error CS8912: The parameterless struct constructor must be 'public'.
                // struct S4 { private S4() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S4").WithLocation(5, 21),
                // (5,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void F2(S2 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(5, 27),
                // (6,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void F3(S3 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(6, 27),
                // (7,27): error CS0122: 'S4.S4()' is inaccessible due to its protection level
                //     static void F4(S4 s = new()) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "new()").WithArguments("S4.S4()").WithLocation(7, 27),
                // (7,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void F4(S4 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(7, 27));
        }
    }
}
