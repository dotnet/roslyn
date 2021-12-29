// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
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
@"public struct S<T>
{
    public readonly bool Initialized;
    public S() { Initialized = true; }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S() { Initialized = true; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S").WithArguments("parameterless struct constructors", "10.0").WithLocation(4, 12));

            comp = CreateCompilation(sourceA);
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
        Console.WriteLine(new S<int>().Initialized);
        Console.WriteLine(CreateNew<S<int>>().Initialized);
        Console.WriteLine(CreateStruct<S<int>>().Initialized);
        Console.WriteLine(CreateStruct<S<string>>().Initialized);
        Console.WriteLine(CreateNew<S<string>>().Initialized);
        Console.WriteLine(new S<string>().Initialized);
    }
}";
            bool secondCall = ExecutionConditionUtil.IsCoreClr; // .NET Framework ignores constructor in second call to Activator.CreateInstance<T>().
            CompileAndVerify(sourceB, references: new[] { refA }, expectedOutput:
$@"True
True
{secondCall}
True
{secondCall}
True");
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,29): error CS8938: The parameterless struct constructor must be 'public'.
                // public struct A1 { internal A1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "A1").WithLocation(2, 29),
                // (3,28): error CS8938: The parameterless struct constructor must be 'public'.
                // public struct A2 { private A2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "A2").WithLocation(3, 28),
                // (4,20): error CS8938: The parameterless struct constructor must be 'public'.
                // public struct A3 { A3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "A3").WithLocation(4, 20),
                // (7,31): error CS8938: The parameterless struct constructor must be 'public'.
                // internal struct B1 { internal B1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "B1").WithLocation(7, 31),
                // (8,30): error CS8938: The parameterless struct constructor must be 'public'.
                // internal struct B2 { private B2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "B2").WithLocation(8, 30),
                // (9,22): error CS8938: The parameterless struct constructor must be 'public'.
                // internal struct B3 { B3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "B3").WithLocation(9, 22),
                // (14,45): error CS8938: The parameterless struct constructor must be 'public'.
                //     internal protected struct C1 { internal C1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "C1").WithLocation(14, 45),
                // (15,44): error CS8938: The parameterless struct constructor must be 'public'.
                //     internal protected struct C2 { private C2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "C2").WithLocation(15, 44),
                // (16,36): error CS8938: The parameterless struct constructor must be 'public'.
                //     internal protected struct C3 { C3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "C3").WithLocation(16, 36),
                // (19,36): error CS8938: The parameterless struct constructor must be 'public'.
                //     protected struct D1 { internal D1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "D1").WithLocation(19, 36),
                // (20,35): error CS8938: The parameterless struct constructor must be 'public'.
                //     protected struct D2 { private D2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "D2").WithLocation(20, 35),
                // (21,27): error CS8938: The parameterless struct constructor must be 'public'.
                //     protected struct D3 { D3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "D3").WithLocation(21, 27),
                // (24,44): error CS8938: The parameterless struct constructor must be 'public'.
                //     private protected struct E1 { internal E1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "E1").WithLocation(24, 44),
                // (25,43): error CS8938: The parameterless struct constructor must be 'public'.
                //     private protected struct E2 { private E2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "E2").WithLocation(25, 43),
                // (26,35): error CS8938: The parameterless struct constructor must be 'public'.
                //     private protected struct E3 { E3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "E3").WithLocation(26, 35),
                // (29,34): error CS8938: The parameterless struct constructor must be 'public'.
                //     private struct F1 { internal F1() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "F1").WithLocation(29, 34),
                // (30,33): error CS8938: The parameterless struct constructor must be 'public'.
                //     private struct F2 { private F2() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "F2").WithLocation(30, 33),
                // (31,25): error CS8938: The parameterless struct constructor must be 'public'.
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
    {accessibility}
    S() {{ }}
}}";
            var comp = CreateCompilation(sourceA);
            comp.VerifyDiagnostics(
                // (4,5): error CS8938: The parameterless struct constructor must be 'public'.
                //     S() { }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S").WithLocation(4, 5));

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

            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(sourceB, references: new[] { refA });
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
            CompileAndVerify(new[] { sourceA, sourceB }, expectedOutput: "True");
        }

        [Fact]
        public void ThisInitializer_01()
        {
            var source =
@"using static System.Console;
using static Program;
struct S1
{
    internal int Value;
    public S1() { Value = Two(); }
    internal S1(object obj) : this() { }
}
struct S2
{
    internal int Value;
    public S2() : this(null) { }
    S2(object obj) { Value = Three(); }
}
class Program
{
    internal static int Two() { WriteLine(""Two()""); return 2; }
    internal static int Three() { WriteLine(""Three()""); return 3; }
    static void Main()
    {
        WriteLine($""new S1().Value: {new S1().Value}"");
        WriteLine($""new S1(null).Value: {new S1(null).Value}"");
        WriteLine($""new S2().Value: {new S2().Value}"");
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput:
@"Two()
new S1().Value: 2
Two()
new S1(null).Value: 2
Three()
new S2().Value: 3
");

            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.Two()""
  IL_0006:  stfld      ""int S1.Value""
  IL_000b:  ret
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
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.Three()""
  IL_0006:  stfld      ""int S2.Value""
  IL_000b:  ret
}");
        }

        [Fact]
        public void ThisInitializer_02()
        {
            var source =
@"using static System.Console;
using static Program;
struct S1
{
    internal int Value;
    public S1() { Value = Two(); }
    internal S1(object obj) : this() { Value = Three(); }
}
struct S2
{
    internal int Value;
    public S2() : this(null) { Value = Two(); }
    S2(object obj) { Value = Three(); }
}
class Program
{
    internal static int Two() { WriteLine(""Two()""); return 2; }
    internal static int Three() { WriteLine(""Three()""); return 3; }
    static void Main()
    {
        WriteLine($""new S1().Value: {new S1().Value}"");
        WriteLine($""new S1(null).Value: {new S1(null).Value}"");
        WriteLine($""new S2().Value: {new S2().Value}"");
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput:
@"Two()
new S1().Value: 2
Two()
Three()
new S1(null).Value: 3
Three()
Two()
new S2().Value: 2
");

            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.Two()""
  IL_0006:  stfld      ""int S1.Value""
  IL_000b:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  call       ""int Program.Three()""
  IL_000c:  stfld      ""int S1.Value""
  IL_0011:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""S2..ctor(object)""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.Two()""
  IL_000d:  stfld      ""int S2.Value""
  IL_0012:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.Three()""
  IL_0006:  stfld      ""int S2.Value""
  IL_000b:  ret
}");
        }

        [Fact]
        public void ThisInitializer_03()
        {
            var source =
@"using static System.Console;
using static Program;
struct S0
{
    internal int Value = One();
    internal S0(object obj) : this() { }
}
struct S1
{
    internal int Value = One();
    public S1() { }
    internal S1(object obj) : this() { }
}
struct S2
{
    internal int Value = One();
    public S2() : this(null) { }
    S2(object obj) { }
}
class Program
{
    internal static int One() { WriteLine(""One()""); return 1; }
    static void Main()
    {
        WriteLine($""new S0().Value: {new S0().Value}"");
        WriteLine($""new S0(null).Value: {new S0(null).Value}"");
        WriteLine($""new S1().Value: {new S1().Value}"");
        WriteLine($""new S1(null).Value: {new S1(null).Value}"");
        WriteLine($""new S2().Value: {new S2().Value}"");
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput: @"
new S0().Value: 0
One()
new S0(null).Value: 1
One()
new S1().Value: 1
One()
new S1(null).Value: 1
One()
new S2().Value: 1
");

            verifier.VerifyMissing("S0..ctor()");
            verifier.VerifyIL("S0..ctor(object)",
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.One()""
  IL_0006:  stfld      ""int S0.Value""
  IL_000b:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.One()""
  IL_0006:  stfld      ""int S1.Value""
  IL_000b:  ret
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
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.One()""
  IL_0006:  stfld      ""int S2.Value""
  IL_000b:  ret
}");
        }

        [Fact]
        public void ThisInitializer_04()
        {
            var source =
@"using static System.Console;
using static Program;
struct S0
{
    internal int Value = One();
    internal S0(object obj) : this() { Value = Two(); }
}
struct S1
{
    internal int Value = One();
    public S1() { Value = Two(); }
    internal S1(object obj) : this() { Value = Three(); }
}
struct S2
{
    internal int Value = One();
    public S2() : this(null) { Value = Two(); }
    S2(object obj) { Value = Three(); }
}
class Program
{
    internal static int One() { WriteLine(""One()""); return 1; }
    internal static int Two() { WriteLine(""Two()""); return 2; }
    internal static int Three() { WriteLine(""Three()""); return 3; }
    static void Main()
    {
        WriteLine($""new S0().Value: {new S0().Value}"");
        WriteLine($""new S0(null).Value: {new S0(null).Value}"");
        WriteLine($""new S1().Value: {new S1().Value}"");
        WriteLine($""new S1(null).Value: {new S1(null).Value}"");
        WriteLine($""new S2().Value: {new S2().Value}"");
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput:
@"new S0().Value: 0
One()
Two()
new S0(null).Value: 2
One()
Two()
new S1().Value: 2
One()
Two()
Three()
new S1(null).Value: 3
One()
Three()
Two()
new S2().Value: 2
");

            verifier.VerifyMissing("S0..ctor()");
            verifier.VerifyIL("S0..ctor(object)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.One()""
  IL_0006:  stfld      ""int S0.Value""
  IL_000b:  ldarg.0
  IL_000c:  call       ""int Program.Two()""
  IL_0011:  stfld      ""int S0.Value""
  IL_0016:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.One()""
  IL_0006:  stfld      ""int S1.Value""
  IL_000b:  ldarg.0
  IL_000c:  call       ""int Program.Two()""
  IL_0011:  stfld      ""int S1.Value""
  IL_0016:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  call       ""int Program.Three()""
  IL_000c:  stfld      ""int S1.Value""
  IL_0011:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""S2..ctor(object)""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.Two()""
  IL_000d:  stfld      ""int S2.Value""
  IL_0012:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.One()""
  IL_0006:  stfld      ""int S2.Value""
  IL_000b:  ldarg.0
  IL_000c:  call       ""int Program.Three()""
  IL_0011:  stfld      ""int S2.Value""
  IL_0016:  ret
}");
        }

        /// <summary>
        /// Initializers with default values to verify that the decision whether
        /// to execute an initializer is independent of the initializer value.
        /// </summary>
        [Fact]
        public void ThisInitializer_05()
        {
            var source =
@"using static System.Console;
using static Program;
struct S0
{
    internal int Value = 0;
    internal S0(object obj) : this() { Value = Two(); }
}
struct S1
{
    internal int Value = 0;
    public S1() { Value = Two(); }
    internal S1(object obj) : this() { Value = Three(); }
}
struct S2
{
    internal int Value = 0;
    public S2() : this(null) { Value = Two(); }
    S2(object obj) { Value = Three(); }
}
class Program
{
    internal static int Two() { WriteLine(""Two()""); return 2; }
    internal static int Three() { WriteLine(""Three()""); return 3; }
    static void Main()
    {
        WriteLine($""new S0().Value: {new S0().Value}"");
        WriteLine($""new S0(null).Value: {new S0(null).Value}"");
        WriteLine($""new S1().Value: {new S1().Value}"");
        WriteLine($""new S1(null).Value: {new S1(null).Value}"");
        WriteLine($""new S2().Value: {new S2().Value}"");
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput:
@"new S0().Value: 0
Two()
new S0(null).Value: 2
Two()
new S1().Value: 2
Two()
Three()
new S1(null).Value: 3
Three()
Two()
new S2().Value: 2
");

            verifier.VerifyMissing("S0..ctor()");
            verifier.VerifyIL("S0..ctor(object)",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S0.Value""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.Two()""
  IL_000d:  stfld      ""int S0.Value""
  IL_0012:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S1.Value""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.Two()""
  IL_000d:  stfld      ""int S1.Value""
  IL_0012:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  call       ""int Program.Three()""
  IL_000c:  stfld      ""int S1.Value""
  IL_0011:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""S2..ctor(object)""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.Two()""
  IL_000d:  stfld      ""int S2.Value""
  IL_0012:  ret
}");
            verifier.VerifyIL("S2..ctor(object)",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S2.Value""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.Three()""
  IL_000d:  stfld      ""int S2.Value""
  IL_0012:  ret
}");
        }

        [Fact]
        public void ThisInitializer_06()
        {
            var source =
@"using static System.Console;
using static Program;
struct S0
{
    internal int Value;
    public S0(params object[] args) { Value = Two(); }
    public S0(int i) : this() { }
}
struct S1
{
    internal int Value;
    public S1(object obj = null) { Value = Two(); }
    public S1(int i) : this() { }
}
class Program
{
    internal static int Two() { WriteLine(""Two()""); return 2; }
    static void Main()
    {
        WriteLine($""new S0().Value: {new S0().Value}"");
        WriteLine($""new S0(0).Value: {new S0(0).Value}"");
        WriteLine($""new S0((object)0).Value: {new S0((object)0).Value}"");
        WriteLine($""new S1().Value: {new S1().Value}"");
        WriteLine($""new S1(1).Value: {new S1(1).Value}"");
        WriteLine($""new S1((object)1).Value: {new S1((object)1).Value}"");
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput:
@"new S0().Value: 0
new S0(0).Value: 0
Two()
new S0((object)0).Value: 2
new S1().Value: 0
new S1(1).Value: 0
Two()
new S1((object)1).Value: 2
");

            verifier.VerifyMissing("S0..ctor()");
            verifier.VerifyIL("S0..ctor(int)",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S0""
  IL_0007:  ret
}");
            verifier.VerifyMissing("S1..ctor()");
            verifier.VerifyIL("S1..ctor(int)",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S1""
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
    public override string ToString() => (X, Y).ToString();
}
struct S1
{
    object X;
    object Y;
    public S1() { Y = 1; }
    public override string ToString() => (X, Y).ToString();
}
struct S2
{
    object X;
    object Y;
    public S2(object y) { Y = y; }
    public override string ToString() => (X, Y).ToString();
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
                // (13,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S1").WithArguments("parameterless struct constructors", "10.0").WithLocation(13, 12),
                // (13,12): error CS0171: Field 'S1.X' must be fully assigned before control is returned to the caller
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.X").WithLocation(13, 12),
                // (20,12): error CS0171: Field 'S2.X' must be fully assigned before control is returned to the caller
                //     public S2(object y) { Y = y; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S2").WithArguments("S2.X").WithLocation(20, 12));

            comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
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
struct S2
{
    object X = null;
    object Y;
    public S2() { Y = 1; }
    public override string ToString() => (X, Y).ToString();
}
struct S3
{
    object X;
    object Y = null;
    public S3(object x) { X = x; }
    public override string ToString() => (X, Y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S2());
        Console.WriteLine(new S3());
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,12): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     object X = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(5, 12),
                // (7,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S2() { Y = 1; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(7, 12),
                // (13,12): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     object Y = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(13, 12));

            comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"(, 1)
(, )");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (S3 V_0)
  IL_0000:  newobj     ""S2..ctor()""
  IL_0005:  box        ""S2""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""S3""
  IL_0017:  ldloc.0
  IL_0018:  box        ""S3""
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S2.X""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  box        ""int""
  IL_000e:  stfld      ""object S2.Y""
  IL_0013:  ret
}");
            verifier.VerifyMissing("S3..ctor()");
            verifier.VerifyIL("S3..ctor(object)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S3.Y""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""object S3.X""
  IL_000e:  ret
}");
        }

        [Fact]
        public void FieldInitializers_02()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S2
{
    internal object X = 2;
    internal object Y;
    public S2() { Y = 2; }
    public override string ToString() => (X, Y).ToString();
}
struct S3
{
    internal object X = 3;
    internal object Y;
    public S3(object _) { Y = 3; }
    public override string ToString() => (X, Y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S2());
        Console.WriteLine(new S3());
        Console.WriteLine(new S2 { });
        Console.WriteLine(new S3 { });
        Console.WriteLine(new S2 { Y = 4 });
        Console.WriteLine(new S3 { Y = 6 });
    }
}";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"(2, 2)
(, )
(2, 2)
(, )
(2, 4)
(, 6)");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      132 (0x84)
  .maxstack  2
  .locals init (S3 V_0,
                S2 V_1)
  IL_0000:  newobj     ""S2..ctor()""
  IL_0005:  box        ""S2""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""S3""
  IL_0017:  ldloc.0
  IL_0018:  box        ""S3""
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  newobj     ""S2..ctor()""
  IL_0027:  box        ""S2""
  IL_002c:  call       ""void System.Console.WriteLine(object)""
  IL_0031:  ldloca.s   V_0
  IL_0033:  initobj    ""S3""
  IL_0039:  ldloc.0
  IL_003a:  box        ""S3""
  IL_003f:  call       ""void System.Console.WriteLine(object)""
  IL_0044:  ldloca.s   V_1
  IL_0046:  call       ""S2..ctor()""
  IL_004b:  ldloca.s   V_1
  IL_004d:  ldc.i4.4
  IL_004e:  box        ""int""
  IL_0053:  stfld      ""object S2.Y""
  IL_0058:  ldloc.1
  IL_0059:  box        ""S2""
  IL_005e:  call       ""void System.Console.WriteLine(object)""
  IL_0063:  ldloca.s   V_0
  IL_0065:  initobj    ""S3""
  IL_006b:  ldloca.s   V_0
  IL_006d:  ldc.i4.6
  IL_006e:  box        ""int""
  IL_0073:  stfld      ""object S3.Y""
  IL_0078:  ldloc.0
  IL_0079:  box        ""S3""
  IL_007e:  call       ""void System.Console.WriteLine(object)""
  IL_0083:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S2.X""
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.2
  IL_000e:  box        ""int""
  IL_0013:  stfld      ""object S2.Y""
  IL_0018:  ret
}");
            verifier.VerifyMissing("S3..ctor()");
            verifier.VerifyIL("S3..ctor(object)",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.3
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S3.X""
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.3
  IL_000e:  box        ""int""
  IL_0013:  stfld      ""object S3.Y""
  IL_0018:  ret
}");
        }

        // As above but with auto-properties.
        [Fact]
        public void FieldInitializers_03()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S2
{
    internal object X { get; init; } = 2;
    internal object Y { get; init; }
    public S2() { Y = 2; }
    public override string ToString() => (X, Y).ToString();
}
struct S3
{
    internal object X { get; private set; } = 3;
    internal object Y { get; set; }
    public S3(object _) { Y = 3; }
    public override string ToString() => (X, Y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S2());
        Console.WriteLine(new S3());
        Console.WriteLine(new S2 { });
        Console.WriteLine(new S3 { });
        Console.WriteLine(new S2 { Y = 4 });
        Console.WriteLine(new S3 { Y = 6 });
    }
}";

            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(2, 2)
(, )
(2, 2)
(, )
(2, 4)
(, 6)");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      132 (0x84)
  .maxstack  2
  .locals init (S3 V_0,
                S2 V_1)
  IL_0000:  newobj     ""S2..ctor()""
  IL_0005:  box        ""S2""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""S3""
  IL_0017:  ldloc.0
  IL_0018:  box        ""S3""
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  newobj     ""S2..ctor()""
  IL_0027:  box        ""S2""
  IL_002c:  call       ""void System.Console.WriteLine(object)""
  IL_0031:  ldloca.s   V_0
  IL_0033:  initobj    ""S3""
  IL_0039:  ldloc.0
  IL_003a:  box        ""S3""
  IL_003f:  call       ""void System.Console.WriteLine(object)""
  IL_0044:  ldloca.s   V_1
  IL_0046:  call       ""S2..ctor()""
  IL_004b:  ldloca.s   V_1
  IL_004d:  ldc.i4.4
  IL_004e:  box        ""int""
  IL_0053:  call       ""void S2.Y.init""
  IL_0058:  ldloc.1
  IL_0059:  box        ""S2""
  IL_005e:  call       ""void System.Console.WriteLine(object)""
  IL_0063:  ldloca.s   V_0
  IL_0065:  initobj    ""S3""
  IL_006b:  ldloca.s   V_0
  IL_006d:  ldc.i4.6
  IL_006e:  box        ""int""
  IL_0073:  call       ""void S3.Y.set""
  IL_0078:  ldloc.0
  IL_0079:  box        ""S3""
  IL_007e:  call       ""void System.Console.WriteLine(object)""
  IL_0083:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S2.<X>k__BackingField""
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.2
  IL_000e:  box        ""int""
  IL_0013:  call       ""void S2.Y.init""
  IL_0018:  ret
}");
            verifier.VerifyMissing("S3..ctor()");
            verifier.VerifyIL("S3..ctor(object)",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.3
  IL_0002:  box        ""int""
  IL_0007:  stfld      ""object S3.<X>k__BackingField""
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.3
  IL_000e:  box        ""int""
  IL_0013:  call       ""void S3.Y.set""
  IL_0018:  ret
}");
        }

        [Fact]
        public void FieldInitializers_04()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S1<T> { internal int X = 1; }
struct S2<T> { internal int X = 2; public S2() { } }
struct S3<T> { internal int X = 3; public S3(int _) { } }
class Program
{
    static void Main()
    {
        Console.WriteLine(new S1<object>().X);
        Console.WriteLine(new S2<object>().X);
        Console.WriteLine(new S3<object>().X);
    }
}";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"1
2
0");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       50 (0x32)
  .maxstack  1
  .locals init (S3<object> V_0)
  IL_0000:  newobj     ""S1<object>..ctor()""
  IL_0005:  ldfld      ""int S1<object>.X""
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  newobj     ""S2<object>..ctor()""
  IL_0014:  ldfld      ""int S2<object>.X""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldloca.s   V_0
  IL_0020:  initobj    ""S3<object>""
  IL_0026:  ldloc.0
  IL_0027:  ldfld      ""int S3<object>.X""
  IL_002c:  call       ""void System.Console.WriteLine(int)""
  IL_0031:  ret
}");
        }

        [Fact]
        public void FieldInitializers_05()
        {
            var source =
@"#pragma warning disable 649
using System;
class A<T>
{
    internal struct S1 { internal int X { get; } = 1; }
    internal struct S2 { internal int X { get; init; } = 2; public S2() { } }
    internal struct S3 { internal int X { get; set; } = 3; public S3(int _) { } }
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new A<object>.S1().X);
        Console.WriteLine(new A<object>.S2().X);
        Console.WriteLine(new A<object>.S3().X);
    }
}";

            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
2
0");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (A<object>.S1 V_0,
                A<object>.S2 V_1,
                A<object>.S3 V_2)
  IL_0000:  newobj     ""A<object>.S1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""readonly int A<object>.S1.X.get""
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  newobj     ""A<object>.S2..ctor()""
  IL_0017:  stloc.1
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""readonly int A<object>.S2.X.get""
  IL_001f:  call       ""void System.Console.WriteLine(int)""
  IL_0024:  ldloca.s   V_2
  IL_0026:  dup
  IL_0027:  initobj    ""A<object>.S3""
  IL_002d:  call       ""readonly int A<object>.S3.X.get""
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  ret
}");
            verifier.VerifyIL("A<T>.S1..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int A<T>.S1.<X>k__BackingField""
  IL_0007:  ret
}");
            verifier.VerifyIL("A<T>.S2..ctor()",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  stfld      ""int A<T>.S2.<X>k__BackingField""
  IL_0007:  ret
}");
            verifier.VerifyIL("A<T>.S3..ctor(int)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.3
  IL_0002:  stfld      ""int A<T>.S3.<X>k__BackingField""
  IL_0007:  ret
}");
        }

        [WorkItem(57870, "https://github.com/dotnet/roslyn/issues/57870")]
        [Fact]
        public void FieldInitializers_06()
        {
            var source =
@"#pragma warning disable 649
struct S1
{
    internal object X = null;
    internal object Y;
}
struct S2
{
    internal object X = 2;
    internal object Y;
    public S2() { }
}
struct S3
{
    internal object X;
    internal object Y = 3;
    public S3() { Y = 3; }
}
struct S4
{
    internal object X;
    internal object Y = 4;
    public S4() { X = 4; }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,8): error CS0171: Field 'S1.Y' must be fully assigned before control is returned to the caller
                // struct S1
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.Y").WithLocation(2, 8),
                // (4,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(4, 21),
                // (9,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X = 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(9, 21),
                // (11,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(11, 12),
                // (11,12): error CS0171: Field 'S2.Y' must be fully assigned before control is returned to the caller
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S2").WithArguments("S2.Y").WithLocation(11, 12),
                // (16,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y = 3;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(16, 21),
                // (17,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S3").WithArguments("parameterless struct constructors", "10.0").WithLocation(17, 12),
                // (17,12): error CS0171: Field 'S3.X' must be fully assigned before control is returned to the caller
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S3").WithArguments("S3.X").WithLocation(17, 12),
                // (22,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y = 4;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(22, 21),
                // (23,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S4() { X = 4; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S4").WithArguments("parameterless struct constructors", "10.0").WithLocation(23, 12));

            var expectedDiagnostics = new[]
            {
                // (2,8): error CS0171: Field 'S1.Y' must be fully assigned before control is returned to the caller
                // struct S1
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.Y").WithLocation(2, 8),
                // (11,12): error CS0171: Field 'S2.Y' must be fully assigned before control is returned to the caller
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S2").WithArguments("S2.Y").WithLocation(11, 12),
                // (17,12): error CS0171: Field 'S3.X' must be fully assigned before control is returned to the caller
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S3").WithArguments("S3.X").WithLocation(17, 12),
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(57870, "https://github.com/dotnet/roslyn/issues/57870")]
        [Fact]
        public void FieldInitializers_07()
        {
            var source =
@"#pragma warning disable 649
struct S1
{
    internal object X { get; } = null;
    internal object Y { get; }
}
struct S2
{
    internal object X { get; } = 2;
    internal object Y { get; }
    public S2() { }
}
struct S3
{
    internal object X { get; }
    internal object Y { get; } = 3;
    public S3() { Y = 3; }
}
struct S4
{
    internal object X { get; }
    internal object Y { get; } = 4;
    public S4() { X = 4; }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,8): error CS0843: Auto-implemented property 'S1.Y' must be fully assigned before control is returned to the caller.
                // struct S1
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S1").WithArguments("S1.Y").WithLocation(2, 8),
                // (4,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X { get; } = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(4, 21),
                // (9,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X { get; } = 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(9, 21),
                // (11,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(11, 12),
                // (11,12): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S2").WithArguments("S2.Y").WithLocation(11, 12),
                // (16,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y { get; } = 3;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(16, 21),
                // (17,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S3").WithArguments("parameterless struct constructors", "10.0").WithLocation(17, 12),
                // (17,12): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S3").WithArguments("S3.X").WithLocation(17, 12),
                // (22,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y { get; } = 4;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(22, 21),
                // (23,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S4() { X = 4; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S4").WithArguments("parameterless struct constructors", "10.0").WithLocation(23, 12));

            var expectedDiagnostics = new[]
            {
                // (2,8): error CS0843: Auto-implemented property 'S1.Y' must be fully assigned before control is returned to the caller.
                // struct S1
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S1").WithArguments("S1.Y").WithLocation(2, 8),
                // (11,12): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S2").WithArguments("S2.Y").WithLocation(11, 12),
                // (17,12): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S3").WithArguments("S3.X").WithLocation(17, 12),
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(57870, "https://github.com/dotnet/roslyn/issues/57870")]
        [Fact]
        public void FieldInitializers_08()
        {
            var source =
@"#pragma warning disable 649
record struct S1
{
    internal object X = 1;
    internal object Y;
}
record struct S2
{
    internal object X { get; } = 2;
    internal object Y { get; }
    public S2() { }
}
record struct S3
{
    internal object X { get; init; }
    internal object Y { get; init; } = 3;
    public S3() { Y = 3; }
}
record struct S4
{
    internal object X { get; init; }
    internal object Y { get; init; } = 4;
    public S4() { X = 4; }
}
";

            var expectedDiagnostics = new[]
            {
                // (2,15): error CS0171: Field 'S1.Y' must be fully assigned before control is returned to the caller
                // record struct S1
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.Y").WithLocation(2, 15),
                // (11,12): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S2").WithArguments("S2.Y").WithLocation(11, 12),
                // (17,12): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S3").WithArguments("S3.X").WithLocation(17, 12)
            };

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void FieldInitializers_09()
        {
            var source =
@"#pragma warning disable 649
record struct S1()
{
    internal object X = 1;
    internal object Y;
}
record struct S2()
{
    internal object X { get; } = 2;
    internal object Y { get; }
}
record struct S3()
{
    internal object X { get; init; }
    internal object Y { get; init; } = 3;
}
";

            var expectedDiagnostics = new[]
            {
                // (2,15): error CS0171: Field 'S1.Y' must be fully assigned before control is returned to the caller
                // record struct S1()
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.Y").WithLocation(2, 15),
                // (7,15): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller.
                // record struct S2()
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S2").WithArguments("S2.Y").WithLocation(7, 15),
                // (12,15): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller.
                // record struct S3()
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S3").WithArguments("S3.X").WithLocation(12, 15)
            };

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void FieldInitializers_10()
        {
            var source =
@"#pragma warning disable 649
record struct S1(object X)
{
    internal object X = 1;
    internal object Y;
}
record struct S2(object X)
{
    internal object X { get; } = 2;
    internal object Y { get; }
}
record struct S3(object Y)
{
    internal object X { get; init; }
    internal object Y { get; init; } = 3;
}
";

            var expectedDiagnostics = new[]
            {
                // (2,15): error CS0171: Field 'S1.Y' must be fully assigned before control is returned to the caller
                // record struct S1(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.Y").WithLocation(2, 15),
                // (2,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S1(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 25),
                // (7,15): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller.
                // record struct S2(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S2").WithArguments("S2.Y").WithLocation(7, 15),
                // (7,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S2(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(7, 25),
                // (12,15): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller.
                // record struct S3(object Y)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S3").WithArguments("S3.X").WithLocation(12, 15),
                // (12,25): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S3(object Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(12, 25)
            };

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void FieldInitializers_11()
        {
            var source =
@"#pragma warning disable 649
record struct S1(object X)
{
    internal object X;
    internal object Y = 1;
}
record struct S2(object X)
{
    internal object X { get; }
    internal object Y { get; } = 2;
}
record struct S3(object Y)
{
    internal object X { get; init; } = 3;
    internal object Y { get; init; }
}
";

            var expectedDiagnostics = new[]
            {
                // (2,15): error CS0171: Field 'S1.X' must be fully assigned before control is returned to the caller
                // record struct S1(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.X").WithLocation(2, 15),
                // (2,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S1(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 25),
                // (7,15): error CS0843: Auto-implemented property 'S2.X' must be fully assigned before control is returned to the caller.
                // record struct S2(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S2").WithArguments("S2.X").WithLocation(7, 15),
                // (7,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S2(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(7, 25),
                // (12,15): error CS0843: Auto-implemented property 'S3.Y' must be fully assigned before control is returned to the caller.
                // record struct S3(object Y)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S3").WithArguments("S3.Y").WithLocation(12, 15),
                // (12,25): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S3(object Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(12, 25)
            };

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void FieldInitializers_12()
        {
            var source =
@"#pragma warning disable 649
using System;
record struct S1(object X)
{
    internal object Y = 1;
}
record struct S2(object X)
{
    internal object Y { get; } = 2;
}
record struct S3(object X)
{
    internal object Y { get; init; } = 3;
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S1());
        Console.WriteLine(new S1(10));
        Console.WriteLine(new S2());
        Console.WriteLine(new S2(20));
        Console.WriteLine(new S3());
        Console.WriteLine(new S3(30));
    }
}
";

            var expectedOutput =
@"S1 { X =  }
S1 { X = 10 }
S2 { X =  }
S2 { X = 20 }
S3 { X =  }
S3 { X = 30 }
";

            CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10, verify: Verification.Skipped, expectedOutput: expectedOutput);
            CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, verify: Verification.Skipped, expectedOutput: expectedOutput);
        }

        [WorkItem(57870, "https://github.com/dotnet/roslyn/issues/57870")]
        [Fact]
        public void FieldInitializers_13()
        {
            var source =
@"#nullable enable

using System;

var x = new S { P1 = ""x1"", P2 = ""x2"" };
var y = new S { P2 = ""y2"" };

Console.WriteLine(y.P1);

record struct S
{
    public string? P1 { get; init; }
    public string? P2 { get; init; } = """";
}";

            var expectedDiagnostics = new[]
            {
                // (10,15): error CS0843: Auto-implemented property 'S.P1' must be fully assigned before control is returned to the caller.
                // record struct S
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "S").WithArguments("S.P1").WithLocation(10, 15)
            };

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void ExpressionTrees()
        {
            var source =
@"#pragma warning disable 649
using System;
using System.Linq.Expressions;
struct S0
{
    int X;
    public override string ToString() => X.ToString();
}
struct S1
{
    int X = 1;
    public override string ToString() => X.ToString();
}
struct S2
{
    int X = 2;
    public S2() { }
    public override string ToString() => X.ToString();
}
class Program
{
    static void Main()
    {
        Report(() => new S0());
        Report(() => new S1());
        Report(() => new S2());
    }
    static void Report<T>(Expression<Func<T>> e)
    {
        var t = e.Compile().Invoke();
        Console.WriteLine(t);
    }
}";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"0
1
2");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      111 (0x6f)
  .maxstack  2
  IL_0000:  ldtoken    ""S0""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""System.Linq.Expressions.NewExpression System.Linq.Expressions.Expression.New(System.Type)""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()""
  IL_0014:  call       ""System.Linq.Expressions.Expression<System.Func<S0>> System.Linq.Expressions.Expression.Lambda<System.Func<S0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0019:  call       ""void Program.Report<S0>(System.Linq.Expressions.Expression<System.Func<S0>>)""
  IL_001e:  ldtoken    ""S1..ctor()""
  IL_0023:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0028:  castclass  ""System.Reflection.ConstructorInfo""
  IL_002d:  call       ""System.Linq.Expressions.Expression[] System.Array.Empty<System.Linq.Expressions.Expression>()""
  IL_0032:  call       ""System.Linq.Expressions.NewExpression System.Linq.Expressions.Expression.New(System.Reflection.ConstructorInfo, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression>)""
  IL_0037:  call       ""System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()""
  IL_003c:  call       ""System.Linq.Expressions.Expression<System.Func<S1>> System.Linq.Expressions.Expression.Lambda<System.Func<S1>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0041:  call       ""void Program.Report<S1>(System.Linq.Expressions.Expression<System.Func<S1>>)""
  IL_0046:  ldtoken    ""S2..ctor()""
  IL_004b:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0050:  castclass  ""System.Reflection.ConstructorInfo""
  IL_0055:  call       ""System.Linq.Expressions.Expression[] System.Array.Empty<System.Linq.Expressions.Expression>()""
  IL_005a:  call       ""System.Linq.Expressions.NewExpression System.Linq.Expressions.Expression.New(System.Reflection.ConstructorInfo, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression>)""
  IL_005f:  call       ""System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()""
  IL_0064:  call       ""System.Linq.Expressions.Expression<System.Func<S2>> System.Linq.Expressions.Expression.Lambda<System.Func<S2>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0069:  call       ""void Program.Report<S2>(System.Linq.Expressions.Expression<System.Func<S2>>)""
  IL_006e:  ret
}");
        }

        [Fact]
        public void Retargeting_01()
        {
            var sourceA =
@"public struct S1
{
    public int X = 1;
}
public struct S2
{
    public int X = 2;
    public S2() { }
}
public struct S3
{
    public int X = 3;
    public S3(object _) { }
}";
            var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Mscorlib40);
            var refA = comp.ToMetadataReference();

            var typeA = comp.GetMember<FieldSymbol>("S1.X").Type;
            var corLibA = comp.Assembly.CorLibrary;
            Assert.Equal(corLibA, typeA.ContainingAssembly);

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine(new S1().X);
        Console.WriteLine(new S2().X);
        Console.WriteLine(new S3().X);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Mscorlib45);
            CompileAndVerify(comp, expectedOutput:
@"1
2
0");

            var corLibB = comp.Assembly.CorLibrary;
            Assert.NotEqual(corLibA, corLibB);

            var field = comp.GetMember<FieldSymbol>("S1.X");
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingFieldSymbol>(field);
            var typeB = (NamedTypeSymbol)field.Type;
            Assert.Equal(corLibB, typeB.ContainingAssembly);
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,12): warning CS8618: Non-nullable field 'F1' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S1() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S1").WithArguments("field", "F1").WithLocation(10, 12),
                // (10,12): error CS0171: Field 'S1.F1' must be fully assigned before control is returned to the caller
                //     public S1() { }
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S1").WithArguments("S1.F1").WithLocation(10, 12),
                // (16,5): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     S2(object? obj) { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S2").WithArguments("field", "F2").WithLocation(16, 5),
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
            var comp = CreateCompilation(source);
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

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
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
        public void ParameterDefaultValues_01()
        {
            var source =
@"struct S1 { }
struct S2 { public S2() { } }
struct S3 { internal S3() { } }
struct S4 { private S4() { } }
class Program
{
    static void F1(S1 s = default) { }
    static void F2(S2 s = default) { }
    static void F3(S3 s = default) { }
    static void F4(S4 s = default) { }
    static void G1(S1 s = new()) { }
    static void G2(S2 s = new()) { }
    static void G3(S3 s = new()) { }
    static void G4(S4 s = new()) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,22): error CS8938: The parameterless struct constructor must be 'public'.
                // struct S3 { internal S3() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S3").WithLocation(3, 22),
                // (4,21): error CS8938: The parameterless struct constructor must be 'public'.
                // struct S4 { private S4() { } }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S4").WithLocation(4, 21),
                // (12,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G2(S2 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(12, 27),
                // (13,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G3(S3 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(13, 27),
                // (14,27): error CS0122: 'S4.S4()' is inaccessible due to its protection level
                //     static void G4(S4 s = new()) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "new()").WithArguments("S4.S4()").WithLocation(14, 27),
                // (14,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G4(S4 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(14, 27));
        }

        [Fact]
        public void ParameterDefaultValues_02()
        {
            var source =
@"struct S1
{
    object X = 1;
}
struct S2
{
    object X = 2;
    public S2() { }
}
struct S3
{
    object X = 3;
    public S3(object x) { X = x; }
}
class Program
{
    static void F1(S1 s = default) { }
    static void F2(S2 s = default) { }
    static void F3(S3 s = default) { }
    static void G1(S1 s = new()) { }
    static void G2(S2 s = new()) { }
    static void G3(S3 s = new()) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (20,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G1(S1 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(20, 27),
                // (21,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G2(S2 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(21, 27));
        }

        [Fact]
        public void Constants()
        {
            var source =
@"struct S0
{
}
struct S1
{
    object X = 1;
}
struct S2
{
    public S2() { }
}
class Program
{
    const object d0 = default(S0);
    const object d1 = default(S1);
    const object d2 = default(S2);
    const object s0 = new S0();
    const object s1 = new S1();
    const object s2 = new S2();
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,23): error CS0133: The expression being assigned to 'Program.d0' must be constant
                //     const object d0 = default(S0);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(S0)").WithArguments("Program.d0").WithLocation(14, 23),
                // (15,23): error CS0133: The expression being assigned to 'Program.d1' must be constant
                //     const object d1 = default(S1);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(S1)").WithArguments("Program.d1").WithLocation(15, 23),
                // (16,23): error CS0133: The expression being assigned to 'Program.d2' must be constant
                //     const object d2 = default(S2);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(S2)").WithArguments("Program.d2").WithLocation(16, 23),
                // (17,23): error CS0133: The expression being assigned to 'Program.s0' must be constant
                //     const object s0 = new S0();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new S0()").WithArguments("Program.s0").WithLocation(17, 23),
                // (18,23): error CS0133: The expression being assigned to 'Program.s1' must be constant
                //     const object s1 = new S1();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new S1()").WithArguments("Program.s1").WithLocation(18, 23),
                // (19,23): error CS0133: The expression being assigned to 'Program.s2' must be constant
                //     const object s2 = new S2();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new S2()").WithArguments("Program.s2").WithLocation(19, 23));
        }

        [Fact]
        public void NoPIA()
        {
            var sourceA =
@"using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""9758B46C-5297-4832-BB58-F2B5B78B0D01"")]
[ComImport()]
[Guid(""6D947BE5-75B1-4D97-B444-2720624761D7"")]
public interface I
{
    S0 F0();
    S1 F1();
    S2 F2();
}
public struct S0
{
}
public struct S1
{
    public S1() { }
}
public struct S2
{
    object F = 2;
}";
            var comp = CreateCompilationWithMscorlib40(sourceA);
            var refA = comp.EmitToImageReference(embedInteropTypes: true);

            var sourceB =
@"class Program
{
    static void M(I i)
    {
        var s0 = i.F0();
        var s1 = i.F1();
        var s2 = i.F2();
    }
}";
            comp = CreateCompilationWithMscorlib40(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (6,18): error CS1757: Embedded interop struct 'S1' can contain only public instance fields.
                //         var s1 = i.F1();
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "i.F1()").WithArguments("S1").WithLocation(6, 18),
                // (7,18): error CS1757: Embedded interop struct 'S2' can contain only public instance fields.
                //         var s2 = i.F2();
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "i.F2()").WithArguments("S2").WithLocation(7, 18));
        }
    }
}
