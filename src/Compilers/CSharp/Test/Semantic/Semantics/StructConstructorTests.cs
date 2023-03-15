// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S0""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.One()""
  IL_000d:  stfld      ""int S0.Value""
  IL_0012:  ret
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
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S0""
  IL_0007:  ldarg.0
  IL_0008:  call       ""int Program.One()""
  IL_000d:  stfld      ""int S0.Value""
  IL_0012:  ldarg.0
  IL_0013:  call       ""int Program.Two()""
  IL_0018:  stfld      ""int S0.Value""
  IL_001d:  ret
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
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S0""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.0
  IL_0009:  stfld      ""int S0.Value""
  IL_000e:  ldarg.0
  IL_000f:  call       ""int Program.Two()""
  IL_0014:  stfld      ""int S0.Value""
  IL_0019:  ret
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

        private static CSharpParseOptions GetParseOptions(LanguageVersion? languageVersion)
        {
            return languageVersion is null ?
                null :
                TestOptions.Regular.WithLanguageVersion(languageVersion.Value);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_01(LanguageVersion? languageVersion)
        {
            var source =
@"using System;
struct S
{
    int x;
    int y;
    public S(int y) : this()
    {
    }
    public override string ToString() => (x, y).ToString();
}
record struct R
{
    int x;
    int y;
    public R(int y) : this()
    {
    }
    public override string ToString() => (x, y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S(2));
        Console.WriteLine(new S());
        Console.WriteLine(new R(2));
        Console.WriteLine(new R());
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: GetParseOptions(languageVersion), expectedOutput:
@"(0, 0)
(0, 0)
(0, 0)
(0, 0)
");
            verifier.VerifyIL("S..ctor(int)",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ret
}");
            verifier.VerifyIL("R..ctor(int)",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""R""
  IL_0007:  ret
}");
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_02(LanguageVersion? languageVersion)
        {
            var source =
@"using System;
struct S
{
    int x;
    int y;
    public S(int y) : this()
    {
        this.y = y;
    }
    public override string ToString() => (x, y).ToString();
}
record struct R
{
    int x;
    int y;
    public R(int y) : this()
    {
        this.y = y;
    }
    public override string ToString() => (x, y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S(2));
        Console.WriteLine(new S());
        Console.WriteLine(new R(2));
        Console.WriteLine(new R());
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: GetParseOptions(languageVersion), expectedOutput:
@"(0, 2)
(0, 0)
(0, 2)
(0, 0)
");
            verifier.VerifyIL("S..ctor(int)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""int S.y""
  IL_000e:  ret
}");
            verifier.VerifyIL("R..ctor(int)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""R""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""int R.y""
  IL_000e:  ret
}");
        }

        [WorkItem(58790, "https://github.com/dotnet/roslyn/issues/58790")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_03(LanguageVersion? languageVersion)
        {
            var source =
@"using System;
struct S
{
    int x = 1;
    int y;
    public S(int y) : this()
    {
    }
    public override string ToString() => (x, y).ToString();
}
record struct R
{
    int x = 1;
    int y;
    public R(int y) : this()
    {
    }
    public override string ToString() => (x, y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S(2));
        Console.WriteLine(new S());
        Console.WriteLine(new R(2));
        Console.WriteLine(new R());
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: GetParseOptions(languageVersion), expectedOutput:
@"(1, 0)
(0, 0)
(1, 0)
(0, 0)
");
            verifier.VerifyIL("S..ctor(int)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int S.x""
  IL_000e:  ret
}");
            verifier.VerifyIL("R..ctor(int)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""R""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int R.x""
  IL_000e:  ret
}");

            var comp = (CSharpCompilation)verifier.Compilation;
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
            var operation = model.GetOperation(syntax);
            var actualText = OperationTreeVerifier.GetOperationTree(comp, operation);
            OperationTreeVerifier.Verify(
@"IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public S(in ... }')
  Initializer:
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
      Expression:
        IInvocationOperation ( S..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
          Instance Receiver:
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: S, IsImplicit) (Syntax: ': this()')
          Arguments(0)
  BlockBody:
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  ExpressionBody:
    null
",
                actualText);
        }

        [WorkItem(58790, "https://github.com/dotnet/roslyn/issues/58790")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_04(LanguageVersion? languageVersion)
        {
            var source =
@"using System;
struct S
{
    int x = 1;
    int y = 2;
    public S(int y) : this()
    {
    }
    public override string ToString() => (x, y).ToString();
}
record struct R
{
    int x = 1;
    int y = 2;
    public R(int y) : this()
    {
    }
    public override string ToString() => (x, y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S(2));
        Console.WriteLine(new S());
        Console.WriteLine(new R(2));
        Console.WriteLine(new R());
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: GetParseOptions(languageVersion), expectedOutput:
@"(1, 2)
(0, 0)
(1, 2)
(0, 0)
");
            verifier.VerifyIL("S..ctor(int)",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int S.x""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.2
  IL_0010:  stfld      ""int S.y""
  IL_0015:  ret
}");
            verifier.VerifyIL("R..ctor(int)",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""R""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int R.x""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.2
  IL_0010:  stfld      ""int R.y""
  IL_0015:  ret
}");

            var comp = (CSharpCompilation)verifier.Compilation;
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
            var operation = model.GetOperation(syntax);
            var actualText = OperationTreeVerifier.GetOperationTree(comp, operation);
            OperationTreeVerifier.Verify(
@"IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public S(in ... }')
  Initializer:
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
      Expression:
        IInvocationOperation ( S..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
          Instance Receiver:
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: S, IsImplicit) (Syntax: ': this()')
          Arguments(0)
  BlockBody:
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  ExpressionBody:
    null
",
                actualText);
        }

        [WorkItem(58790, "https://github.com/dotnet/roslyn/issues/58790")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_05(LanguageVersion? languageVersion)
        {
            var source =
@"using System;
struct S
{
    int x = 1;
    int y;
    public S(int y) : this()
    {
        this.y = y;
    }
    public override string ToString() => (x, y).ToString();
}
record struct R
{
    int x = 1;
    int y;
    public R(int y) : this()
    {
        this.y = y;
    }
    public override string ToString() => (x, y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S(2));
        Console.WriteLine(new S());
        Console.WriteLine(new R(2));
        Console.WriteLine(new R());
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: GetParseOptions(languageVersion), expectedOutput:
@"(1, 2)
(0, 0)
(1, 2)
(0, 0)
");
            verifier.VerifyIL("S..ctor(int)",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int S.x""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.1
  IL_0010:  stfld      ""int S.y""
  IL_0015:  ret
}");
            verifier.VerifyIL("R..ctor(int)",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""R""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int R.x""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.1
  IL_0010:  stfld      ""int R.y""
  IL_0015:  ret
}");

            var comp = (CSharpCompilation)verifier.Compilation;
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
            var operation = model.GetOperation(syntax);
            var actualText = OperationTreeVerifier.GetOperationTree(comp, operation);
            OperationTreeVerifier.Verify(
@"IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public S(in ... }')
  Initializer:
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
      Expression:
        IInvocationOperation ( S..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
          Instance Receiver:
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: S, IsImplicit) (Syntax: ': this()')
          Arguments(0)
  BlockBody:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'this.y = y;')
        Expression:
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'this.y = y')
            Left:
              IFieldReferenceOperation: System.Int32 S.y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'this.y')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: S) (Syntax: 'this')
            Right:
              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
  ExpressionBody:
    null
",
                actualText);
        }

        [Fact]
        public void DefaultThisInitializer_06()
        {
            var source =
@"#pragma warning disable 169
#pragma warning disable 414
using System;
struct S1
{
    int x;
    int y;
    public S1(int y) : this()
    {
    }
    public S1() { }
    public override string ToString() => (x, y).ToString();
}
struct S2
{
    int x;
    int y;
    public S2(int y) : this()
    {
        this.y = y;
    }
    public S2() { }
    public override string ToString() => (x, y).ToString();
}
struct S3
{
    int x;
    int y;
    public S3(int y) : this()
    {
    }
    public S3()
    {
        this.y = -3;
    }
    public override string ToString() => (x, y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S1(1));
        Console.WriteLine(new S1());
        Console.WriteLine(new S2(2));
        Console.WriteLine(new S2());
        Console.WriteLine(new S3(3));
        Console.WriteLine(new S3());
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (11,12): error CS0171: Field 'S1.y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S1() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.y", "11.0").WithLocation(11, 12),
                // (11,12): error CS0171: Field 'S1.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S1() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.x", "11.0").WithLocation(11, 12),
                // (22,12): error CS0171: Field 'S2.y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S2").WithArguments("S2.y", "11.0").WithLocation(22, 12),
                // (22,12): error CS0171: Field 'S2.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S2").WithArguments("S2.x", "11.0").WithLocation(22, 12),
                // (32,12): error CS0171: Field 'S3.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S3()
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S3").WithArguments("S3.x", "11.0").WithLocation(32, 12));

            var verifier = CompileAndVerify(source, expectedOutput:
@"(0, 0)
(0, 0)
(0, 2)
(0, 0)
(0, -3)
(0, -3)
");
            verifier.VerifyIL("S1..ctor(int)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S1.x""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.0
  IL_0009:  stfld      ""int S1.y""
  IL_000e:  ret
}");
            verifier.VerifyIL("S2..ctor(int)",
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S2..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""int S2.y""
  IL_000d:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S2.x""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.0
  IL_0009:  stfld      ""int S2.y""
  IL_000e:  ret
}");
            verifier.VerifyIL("S3..ctor(int)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S3..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S3..ctor()",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S3.x""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      ""int S3.y""
  IL_000f:  ret
}");
        }

        [Fact]
        public void DefaultThisInitializer_07()
        {
            var source =
@"#pragma warning disable 169
#pragma warning disable 414
using System;
struct S1
{
    int x = 1;
    int y;
    public S1(int y) : this()
    {
    }
    public S1() { }
    public override string ToString() => (x, y).ToString();
}
struct S2
{
    int x = 2;
    int y;
    public S2(int y) : this()
    {
        this.y = y;
    }
    public S2() { }
    public override string ToString() => (x, y).ToString();
}
struct S3
{
    int x = 3;
    int y;
    public S3(int y) : this()
    {
    }
    public S3()
    {
        this.y = -3;
    }
    public override string ToString() => (x, y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S1(1));
        Console.WriteLine(new S1());
        Console.WriteLine(new S2(2));
        Console.WriteLine(new S2());
        Console.WriteLine(new S3(3));
        Console.WriteLine(new S3());
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (11,12): error CS0171: Field 'S1.y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S1() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.y", "11.0").WithLocation(11, 12),
                // (22,12): error CS0171: Field 'S2.y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S2").WithArguments("S2.y", "11.0").WithLocation(22, 12));

            var verifier = CompileAndVerify(source, expectedOutput:
@"(1, 0)
(1, 0)
(2, 2)
(2, 0)
(3, -3)
(3, -3)
");
            verifier.VerifyIL("S1..ctor(int)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S1.y""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int S1.x""
  IL_000e:  ret
}");
            verifier.VerifyIL("S2..ctor(int)",
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""S2..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""int S2.y""
  IL_000d:  ret
}");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S2.y""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.2
  IL_0009:  stfld      ""int S2.x""
  IL_000e:  ret
}");
            verifier.VerifyIL("S3..ctor(int)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S3..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S3..ctor()",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.3
  IL_0002:  stfld      ""int S3.x""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      ""int S3.y""
  IL_000f:  ret
}");
        }

        [Fact]
        public void DefaultThisInitializer_08()
        {
            var source =
@"struct S1
{
    int x1 = y1 + 1;
    int y1;
    public S1(int y)
    {
        this.y1 = y;
    }
}
record struct R1
{
    int x1 = y1 + 1;
    int y1;
    public R1(int y)
    {
        this.y1 = y;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'S1.y1'
                //     int x1 = y1 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y1").WithArguments("S1.y1").WithLocation(3, 14),
                // (3,14): error CS9015: Use of possibly unassigned field 'y1'. Consider updating to language version '11.0' to auto-default the field.
                //     int x1 = y1 + 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion, "y1").WithArguments("y1", "11.0").WithLocation(3, 14),
                // (12,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'R1.y1'
                //     int x1 = y1 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y1").WithArguments("R1.y1").WithLocation(12, 14),
                // (12,14): error CS9015: Use of possibly unassigned field 'y1'. Consider updating to language version '11.0' to auto-default the field.
                //     int x1 = y1 + 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion, "y1").WithArguments("y1", "11.0").WithLocation(12, 14));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'S1.y1'
                //     int x1 = y1 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y1").WithArguments("S1.y1").WithLocation(3, 14),
                // (12,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'R1.y1'
                //     int x1 = y1 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y1").WithArguments("R1.y1").WithLocation(12, 14));
        }

        [WorkItem(58790, "https://github.com/dotnet/roslyn/issues/58790")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_09(LanguageVersion? languageVersion)
        {
            var source =
@"struct S2
{
    int x2 = y2 + 1;
    int y2;
    public S2(int y) : this()
    {
        this.y2 = y;
    }
}
record struct R2
{
    int x2 = y2 + 1;
    int y2;
    public R2(int y) : this()
    {
        this.y2 = y;
    }
}";

            var comp = CreateCompilation(source, parseOptions: GetParseOptions(languageVersion));
            comp.VerifyDiagnostics(
                // (3,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'S2.y2'
                //     int x2 = y2 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y2").WithArguments("S2.y2").WithLocation(3, 14),
                // (12,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'R2.y2'
                //     int x2 = y2 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y2").WithArguments("R2.y2").WithLocation(12, 14));
        }

        [Fact]
        public void DefaultThisInitializer_10()
        {
            var source =
@"struct S3
{
    int x3 = y3 + 1;
    int y3;
    public S3(int y)
    {
        this = default;
        this.y3 = y;
    }
}
record struct R3
{
    int x3 = y3 + 1;
    int y3;
    public R3(int y)
    {
        this = default;
        this.y3 = y;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'S3.y3'
                //     int x3 = y3 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y3").WithArguments("S3.y3").WithLocation(3, 14),
                // (3,14): error CS9015: Use of possibly unassigned field 'y3'. Consider updating to language version '11.0' to auto-default the field.
                //     int x3 = y3 + 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion, "y3").WithArguments("y3", "11.0").WithLocation(3, 14),
                // (13,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'R3.y3'
                //     int x3 = y3 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y3").WithArguments("R3.y3").WithLocation(13, 14),
                // (13,14): error CS9015: Use of possibly unassigned field 'y3'. Consider updating to language version '11.0' to auto-default the field.
                //     int x3 = y3 + 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion, "y3").WithArguments("y3", "11.0").WithLocation(13, 14));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'S3.y3'
                //     int x3 = y3 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y3").WithArguments("S3.y3").WithLocation(3, 14),
                // (13,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'R3.y3'
                //     int x3 = y3 + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y3").WithArguments("R3.y3").WithLocation(13, 14));
        }

        [WorkItem(58790, "https://github.com/dotnet/roslyn/issues/58790")]
        [Fact]
        public void DefaultThisInitializer_11()
        {
            var source =
@"#pragma warning disable 169
#pragma warning disable 414
using System;
struct S
{
    int x1;
    int y1 = 1;
    public S(int unused) { }
    public S(string unused) : this() { }
    public override string ToString() => (x1, y1).ToString();
}
record struct R
{
    int x2;
    int y2 = -1;
    public R(int unused) { }
    public R(string unused) : this() { }
    public override string ToString() => (x2, y2).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S(0));
        Console.WriteLine(new S(string.Empty));
        Console.WriteLine(new R(0));
        Console.WriteLine(new R(string.Empty));
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,12): error CS0171: Field 'S.x1' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S(int unused) { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.x1", "11.0").WithLocation(8, 12),
                // (16,12): error CS0171: Field 'R.x2' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public R(int unused) { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "R").WithArguments("R.x2", "11.0").WithLocation(16, 12));

            var verifier = CompileAndVerify(source, expectedOutput:
@"(0, 1)
(0, 1)
(0, -1)
(0, -1)
");
            verifier.VerifyIL("S..ctor(int)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S.x1""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int S.y1""
  IL_000e:  ret
}");
            verifier.VerifyIL("S..ctor(string)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int S.y1""
  IL_000e:  ret
}");
            verifier.VerifyIL("R..ctor(int)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int R.x2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.m1
  IL_0009:  stfld      ""int R.y2""
  IL_000e:  ret
}");
            verifier.VerifyIL("R..ctor(string)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""R""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.m1
  IL_0009:  stfld      ""int R.y2""
  IL_000e:  ret
}");
        }

        [WorkItem(58790, "https://github.com/dotnet/roslyn/issues/58790")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_12(LanguageVersion? languageVersion)
        {
            var source =
@"using System;
struct S
{
    int x1;
    int y1 = 1;
    public S(int unused) { x1 = 2; }
    public S(string unused) : this() { x1 = 3; }
    public override string ToString() => (x1, y1).ToString();
}
record struct R
{
    int x2;
    int y2 = -1;
    public R(int unused) { x2 = -2; }
    public R(string unused) : this() { x2 = -3; }
    public override string ToString() => (x2, y2).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S(0));
        Console.WriteLine(new S(string.Empty));
        Console.WriteLine(new R(0));
        Console.WriteLine(new R(string.Empty));
    }
}";

            var verifier = CompileAndVerify(source, parseOptions: GetParseOptions(languageVersion), expectedOutput:
@"(2, 1)
(3, 1)
(-2, -1)
(-3, -1)
");
            verifier.VerifyIL("S..ctor(int)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int S.y1""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.2
  IL_0009:  stfld      ""int S.x1""
  IL_000e:  ret
}");
            verifier.VerifyIL("S..ctor(string)",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int S.y1""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.3
  IL_0010:  stfld      ""int S.x1""
  IL_0015:  ret
}");
            verifier.VerifyIL("R..ctor(int)",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.m1
  IL_0002:  stfld      ""int R.y2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -2
  IL_000a:  stfld      ""int R.x2""
  IL_000f:  ret
}");
            verifier.VerifyIL("R..ctor(string)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""R""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.m1
  IL_0009:  stfld      ""int R.y2""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   -3
  IL_0011:  stfld      ""int R.x2""
  IL_0016:  ret
}");
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_13(LanguageVersion? languageVersion)
        {
            var source =
@"#pragma warning disable 169
struct S
{
    int x1;
    int y1;
    public S() : this() { }
}
record struct R
{
    int x2;
    int y2;
    public R() : this() { }
}";
            var comp = CreateCompilation(source, parseOptions: GetParseOptions(languageVersion));
            comp.VerifyDiagnostics(
                // (6,18): error CS0516: Constructor 'S.S()' cannot call itself
                //     public S() : this() { }
                Diagnostic(ErrorCode.ERR_RecursiveConstructorCall, "this").WithArguments("S.S()").WithLocation(6, 18),
                // (12,18): error CS0516: Constructor 'R.R()' cannot call itself
                //     public R() : this() { }
                Diagnostic(ErrorCode.ERR_RecursiveConstructorCall, "this").WithArguments("R.R()").WithLocation(12, 18));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(null)]
        public void DefaultThisInitializer_14(LanguageVersion? languageVersion)
        {
            var source =
@"#pragma warning disable 169
struct S
{
    int x1;
    int y1 = 1;
    public S() : this() { }
}
record struct R
{
    int x2;
    int y2 = 2;
    public R() : this() { }
}";
            var comp = CreateCompilation(source, parseOptions: GetParseOptions(languageVersion));
            comp.VerifyDiagnostics(
                // (6,18): error CS0516: Constructor 'S.S()' cannot call itself
                //     public S() : this() { }
                Diagnostic(ErrorCode.ERR_RecursiveConstructorCall, "this").WithArguments("S.S()").WithLocation(6, 18),
                // (12,18): error CS0516: Constructor 'R.R()' cannot call itself
                //     public R() : this() { }
                Diagnostic(ErrorCode.ERR_RecursiveConstructorCall, "this").WithArguments("R.R()").WithLocation(12, 18));
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
                // (13,12): error CS0171: Field 'S1.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S1() { Y = 1; }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.X", "11.0").WithLocation(13, 12),
                // (20,12): error CS0171: Field 'S2.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S2(object y) { Y = y; }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S2").WithArguments("S2.X", "11.0").WithLocation(20, 12));

            var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular11, expectedOutput:
@"(, )
(, 1)
(, )");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void FieldInitializers_None_WithEmptyParameterlessConstructor()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S0
{
    object X;
    object Y;
    public S0() { }
    public override string ToString() => (X, Y).ToString();
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S0());
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S0() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S0").WithArguments("parameterless struct constructors", "10.0").WithLocation(7, 12),
                // (7,12): error CS0171: Field 'S0.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S0() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S0").WithArguments("S0.Y", "11.0").WithLocation(7, 12),
                // (7,12): error CS0171: Field 'S0.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S0() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S0").WithArguments("S0.X", "11.0").WithLocation(7, 12));

            var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular11, expectedOutput: "(, )");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("S0..ctor", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S0.X""
  IL_0007:  ldarg.0
  IL_0008:  ldnull
  IL_0009:  stfld      ""object S0.Y""
  // sequence point: }
  IL_000e:  ret
}
", sequencePoints: "S0..ctor", source: source);
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
        public void FieldInitializers_04A()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S1<T> { internal int X = 1; }
class Program
{
    static void Main()
    {
        Console.WriteLine(new S1<object>().X);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1<T> { internal int X = 1; }
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(3, 8));
        }

        [Fact]
        public void FieldInitializers_04B()
        {
            var source =
@"#pragma warning disable 649
using System;
struct S2<T> { internal int X = 2; public S2() { } }
struct S3<T> { internal int X = 3; public S3(int _) { } }
class Program
{
    static void Main()
    {
        Console.WriteLine(new S2<object>().X);
        Console.WriteLine(new S3<object>().X);
    }
}";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"2
0");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (S3<object> V_0)
  IL_0000:  newobj     ""S2<object>..ctor()""
  IL_0005:  ldfld      ""int S2<object>.X""
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""S3<object>""
  IL_0017:  ldloc.0
  IL_0018:  ldfld      ""int S3<object>.X""
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  ret
}");
        }

        [Fact]
        public void FieldInitializers_05A()
        {
            var source =
@"#pragma warning disable 649
using System;
class A<T>
{
    internal struct S1 { internal int X { get; } = 1; }
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new A<object>.S1().X);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,21): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                //     internal struct S1 { internal int X { get; } = 1; }
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(5, 21));
        }

        [Fact]
        public void FieldInitializers_05B()
        {
            var source =
@"#pragma warning disable 649
using System;
class A<T>
{
    internal struct S2 { internal int X { get; init; } = 2; public S2() { } }
    internal struct S3 { internal int X { get; set; } = 3; public S3(int _) { } }
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new A<object>.S2().X);
        Console.WriteLine(new A<object>.S3().X);
    }
}";

            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"2
0");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (A<object>.S2 V_0,
                A<object>.S3 V_1)
  IL_0000:  newobj     ""A<object>.S2..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""readonly int A<object>.S2.X.get""
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  ldloca.s   V_1
  IL_0014:  dup
  IL_0015:  initobj    ""A<object>.S3""
  IL_001b:  call       ""readonly int A<object>.S3.X.get""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  ret
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
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 8),
                // (4,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(4, 21),
                // (9,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X = 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(9, 21),
                // (11,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(11, 12),
                // (11,12): error CS0171: Field 'S2.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S2").WithArguments("S2.Y", "11.0").WithLocation(11, 12),
                // (16,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y = 3;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(16, 21),
                // (17,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S3").WithArguments("parameterless struct constructors", "10.0").WithLocation(17, 12),
                // (17,12): error CS0171: Field 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(17, 12),
                // (22,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y = 4;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(22, 21),
                // (23,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S4() { X = 4; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S4").WithArguments("parameterless struct constructors", "10.0").WithLocation(23, 12));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 8),
                // (11,12): error CS0171: Field 'S2.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S2").WithArguments("S2.Y", "11.0").WithLocation(11, 12),
                // (17,12): error CS0171: Field 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(17, 12));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 8));
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
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 8),
                // (4,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X { get; } = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(4, 21),
                // (9,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object X { get; } = 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("struct field initializers", "10.0").WithLocation(9, 21),
                // (11,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(11, 12),
                // (11,12): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S2").WithArguments("S2.Y", "11.0").WithLocation(11, 12),
                // (16,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y { get; } = 3;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(16, 21),
                // (17,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S3").WithArguments("parameterless struct constructors", "10.0").WithLocation(17, 12),
                // (17,12): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(17, 12),
                // (22,21): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal object Y { get; } = 4;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Y").WithArguments("struct field initializers", "10.0").WithLocation(22, 21),
                // (23,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S4() { X = 4; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S4").WithArguments("parameterless struct constructors", "10.0").WithLocation(23, 12));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 8),
                // (11,12): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S2").WithArguments("S2.Y", "11.0").WithLocation(11, 12),
                // (17,12): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(17, 12));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 8));
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

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (2,15): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // record struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 15),
                // (11,12): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S2").WithArguments("S2.Y", "11.0").WithLocation(11, 12),
                // (17,12): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public S3() { Y = 3; }
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(17, 12));

            comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (2,15): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // record struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(2, 15));
        }

        [Fact]
        public void FieldInitializers_09()
        {
            var source = @"
using System;

#pragma warning disable 649
record struct S1()
{
    public object X = 1;
    public object Y;
}
record struct S2()
{
    public object X { get; } = 2;
    public object Y { get; }
}
record struct S3()
{
    public object X { get; init; }
    public object Y { get; init; } = 3;
}

class Program
{
    static void Main()
    {
        Console.WriteLine(new S1());
        Console.WriteLine(new S2());
        Console.WriteLine(new S3());
    }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,15): error CS0171: Field 'S1.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                // record struct S1()
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.Y", "11.0").WithLocation(5, 15),
                // (10,15): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct S2()
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S2").WithArguments("S2.Y", "11.0").WithLocation(10, 15),
                // (15,15): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct S3()
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(15, 15));

            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular11, expectedOutput:
@"
S1 { X = 1, Y =  }
S2 { X = 2, Y =  }
S3 { X = , Y = 3 }
", verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S1..ctor", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S1.Y""
  // sequence point: public object X = 1;
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  box        ""int""
  IL_000e:  stfld      ""object S1.X""
  IL_0013:  ret
}", sequencePoints: "S1..ctor", source: source);

            verifier.VerifyIL("S2..ctor", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S2.<Y>k__BackingField""
  // sequence point: 2
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.2
  IL_0009:  box        ""int""
  IL_000e:  stfld      ""object S2.<X>k__BackingField""
  IL_0013:  ret
}", sequencePoints: "S2..ctor", source: source);

            verifier.VerifyIL("S3..ctor", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object S3.<X>k__BackingField""
  // sequence point: 3
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.3
  IL_0009:  box        ""int""
  IL_000e:  stfld      ""object S3.<Y>k__BackingField""
  IL_0013:  ret
}", sequencePoints: "S3..ctor", source: source);
        }

        [Fact]
        public void FieldInitializers_10()
        {
            var source = @"
using System;

#pragma warning disable 649
record struct S1(object X)
{
    public object X = 1;
    public object Y;
}
record struct S2(object X)
{
    public object X { get; } = 2;
    public object Y { get; }
}
record struct S3(object Y)
{
    public object X { get; init; }
    public object Y { get; init; } = 3;
}

class Program
{
    static void Main()
    {
        Console.WriteLine(new S1(""a""));
        Console.WriteLine(new S2(""b""));
        Console.WriteLine(new S3(""c""));
    }
}
";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,15): error CS0171: Field 'S1.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                // record struct S1(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.Y", "11.0").WithLocation(5, 15),
                // (5,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S1(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(5, 25),
                // (10,15): error CS0843: Auto-implemented property 'S2.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct S2(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S2").WithArguments("S2.Y", "11.0").WithLocation(10, 15),
                // (10,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S2(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(10, 25),
                // (15,15): error CS0843: Auto-implemented property 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct S3(object Y)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(15, 15),
                // (15,25): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S3(object Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(15, 25));

            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular11, expectedOutput:
@"S1 { X = 1, Y =  }
S2 { X = 2, Y =  }
S3 { X = , Y = 3 }
", verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (5,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S1(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(5, 25),
                // (10,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S2(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(10, 25),
                // (15,25): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S3(object Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(15, 25));
        }

        [Fact]
        public void FieldInitializers_11()
        {
            var source = @"
using System;

#pragma warning disable 649
record struct S1(object X)
{
    public object X;
    public object Y = 1;
}
record struct S2(object X)
{
    public object X { get; }
    public object Y { get; } = 2;
}
record struct S3(object Y)
{
    public object X { get; init; } = 3;
    public object Y { get; init; }
}

class Program
{
    static void Main()
    {
        Console.WriteLine(new S1(""a""));
        Console.WriteLine(new S2(""b""));
        Console.WriteLine(new S3(""c""));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,15): error CS0171: Field 'S1.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                // record struct S1(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.X", "11.0").WithLocation(5, 15),
                // (5,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S1(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(5, 25),
                // (10,15): error CS0843: Auto-implemented property 'S2.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct S2(object X)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S2").WithArguments("S2.X", "11.0").WithLocation(10, 15),
                // (10,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S2(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(10, 25),
                // (15,15): error CS0843: Auto-implemented property 'S3.Y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct S3(object Y)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "S3").WithArguments("S3.Y", "11.0").WithLocation(15, 15),
                // (15,25): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S3(object Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(15, 25));

            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular11, expectedOutput:
@"S1 { X = , Y = 1 }
S2 { X = , Y = 2 }
S3 { X = 3, Y =  }", verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (5,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S1(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(5, 25),
                // (10,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S2(object X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(10, 25),
                // (15,25): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S3(object Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(15, 25));
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
                // (10,15): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // record struct S
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(10, 15)
            };

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        /// <summary>
        /// Should still report binding errors in field initializers
        /// even if there is no explicit constructor.
        /// </summary>
        [Fact]
        public void FieldInitializers_14()
        {
            var source =
@"struct S
{
    private object F = Unknown();
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(1, 8),
                // (3,20): warning CS0169: The field 'S.F' is never used
                //     private object F = Unknown();
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("S.F").WithLocation(3, 20),
                // (3,24): error CS0103: The name 'Unknown' does not exist in the current context
                //     private object F = Unknown();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Unknown").WithArguments("Unknown").WithLocation(3, 24));
        }

        [Fact]
        public void FieldInitializers_15()
        {
            var source =
@"struct S0
{
    static S0() { }
    public int F = 1;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S0
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S0").WithLocation(1, 8),
                // (4,16): warning CS0649: Field 'S0.F' is never assigned to, and will always have its default value 0
                //     public int F = 1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("S0.F", "0").WithLocation(4, 16));
        }

        [Fact]
        public void FieldInitializers_16()
        {
            var source =
@"using System;
struct S1
{
    static S1() { }
    public S1() { }
    public int F = 1;
}
struct S2
{
    static S2() { }
    public S2(object o) { }
    public int F = 2;
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new S1().F);
        Console.WriteLine(new S2().F);
    }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
0");
        }

        [Fact]
        public void ExpressionTrees_01()
        {
            var source =
@"#pragma warning disable 649
using System;
using System.Linq.Expressions;
struct S1
{
    int X = 1;
    public override string ToString() => X.ToString();
}
class Program
{
    static void Main()
    {
        Report(() => new S1());
    }
    static void Report<T>(Expression<Func<T>> e)
    {
        var t = e.Compile().Invoke();
        Console.WriteLine(t);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(4, 8));
        }

        [Fact]
        public void ExpressionTrees_02()
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
2");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       71 (0x47)
  .maxstack  2
  IL_0000:  ldtoken    ""S0""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""System.Linq.Expressions.NewExpression System.Linq.Expressions.Expression.New(System.Type)""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()""
  IL_0014:  call       ""System.Linq.Expressions.Expression<System.Func<S0>> System.Linq.Expressions.Expression.Lambda<System.Func<S0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0019:  call       ""void Program.Report<S0>(System.Linq.Expressions.Expression<System.Func<S0>>)""
  IL_001e:  ldtoken    ""S2..ctor()""
  IL_0023:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0028:  castclass  ""System.Reflection.ConstructorInfo""
  IL_002d:  call       ""System.Linq.Expressions.Expression[] System.Array.Empty<System.Linq.Expressions.Expression>()""
  IL_0032:  call       ""System.Linq.Expressions.NewExpression System.Linq.Expressions.Expression.New(System.Reflection.ConstructorInfo, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression>)""
  IL_0037:  call       ""System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()""
  IL_003c:  call       ""System.Linq.Expressions.Expression<System.Func<S2>> System.Linq.Expressions.Expression.Lambda<System.Func<S2>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0041:  call       ""void Program.Report<S2>(System.Linq.Expressions.Expression<System.Func<S2>>)""
  IL_0046:  ret
}");
        }

        [Fact]
        public void Retargeting_01()
        {
            var sourceA =
@"public struct S2
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

            var typeA = comp.GetMember<FieldSymbol>("S2.X").Type;
            var corLibA = comp.Assembly.CorLibrary;
            Assert.Equal(corLibA, typeA.ContainingAssembly);

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine(new S2().X);
        Console.WriteLine(new S3().X);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Mscorlib45);
            CompileAndVerify(comp, expectedOutput:
@"2
0");

            var corLibB = comp.Assembly.CorLibrary;
            Assert.NotEqual(corLibA, corLibB);

            var field = comp.GetMember<FieldSymbol>("S2.X");
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (10,12): warning CS8618: Non-nullable field 'F1' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S1() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S1").WithArguments("field", "F1").WithLocation(10, 12),
                // (10,12): error CS0171: Field 'S1.F1' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S1() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S1").WithArguments("S1.F1", "11.0").WithLocation(10, 12),
                // (16,5): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     S2(object? obj) { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S2").WithArguments("field", "F2").WithLocation(16, 5),
                // (16,5): error CS0171: Field 'S2.F2' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     S2(object? obj) { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S2").WithArguments("S2.F2", "11.0").WithLocation(16, 5),
                // (21,12): warning CS8618: Non-nullable field 'F3' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S3() { F3 = GetValue(); }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S3").WithArguments("field", "F3").WithLocation(21, 12),
                // (21,24): warning CS8601: Possible null reference assignment.
                //     public S3() { F3 = GetValue(); }
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "GetValue()").WithLocation(21, 24));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (10,12): warning CS8618: Non-nullable field 'F1' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S1() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S1").WithArguments("field", "F1").WithLocation(10, 12),
                // (16,5): warning CS8618: Non-nullable field 'F2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     S2(object? obj) { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S2").WithArguments("field", "F2").WithLocation(16, 5),
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
static class Utils
{
    internal static object? GetValue() => null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S0
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S0").WithLocation(2, 8),
                // (4,12): warning CS0169: The field 'S0.F0' is never used
                //     object F0 = Utils.GetValue();
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F0").WithArguments("S0.F0").WithLocation(4, 12));
        }

        [Fact]
        public void NullableAnalysis_03()
        {
            var source =
@"#nullable enable
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
                //     object F1 = Utils.GetValue();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "Utils.GetValue()").WithLocation(4, 17),
                // (9,17): warning CS8601: Possible null reference assignment.
                //     object F2 = Utils.GetValue();
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "Utils.GetValue()").WithLocation(9, 17),
                // (10,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     public S2() : this(null) { }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 24));
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

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (20,12): error CS0171: Field 'S3.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S3() { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S3").WithArguments("S3.X", "11.0").WithLocation(20, 12),
                // (22,15): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // unsafe struct S4
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S4").WithLocation(22, 15),
                // (29,9): warning CS0414: The field 'S5.X' is assigned but its value is never used
                //     int X;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "X").WithArguments("S5.X").WithLocation(29, 9));

            comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (22,15): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // unsafe struct S4
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S4").WithLocation(22, 15),
                // (29,9): warning CS0414: The field 'S5.X' is assigned but its value is never used
                //     int X;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "X").WithArguments("S5.X").WithLocation(29, 9)
            );
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
    static void G5(S1? s = new()) { }
    static void G6(S1? s = new S1()) { }
    static void G7(decimal s = new(1)) { }
    static void G8(decimal s = new decimal(1)) { }
    static void G9(decimal? s = new(1)) { }
    static void G10(decimal? s = new decimal(1)) { }
    static void G11(decimal s = (decimal)1) { }
    static void G12(decimal? s = (decimal)2) { }
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
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(14, 27),
                // (15,24): error CS1770: A value of type 'S1' cannot be used as default parameter for nullable parameter 's' because 'S1' is not a simple type
                //     static void G5(S1? s = new()) { }
                Diagnostic(ErrorCode.ERR_NoConversionForNubDefaultParam, "s").WithArguments("S1", "s").WithLocation(15, 24),
                // (16,24): error CS1770: A value of type 'S1' cannot be used as default parameter for nullable parameter 's' because 'S1' is not a simple type
                //     static void G6(S1? s = new S1()) { }
                Diagnostic(ErrorCode.ERR_NoConversionForNubDefaultParam, "s").WithArguments("S1", "s").WithLocation(16, 24),
                // (17,32): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G7(decimal s = new(1)) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new(1)").WithArguments("s").WithLocation(17, 32),
                // (18,32): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G8(decimal s = new decimal(1)) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new decimal(1)").WithArguments("s").WithLocation(18, 32),
                // (19,33): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G9(decimal? s = new(1)) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new(1)").WithArguments("s").WithLocation(19, 33),
                // (20,34): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G10(decimal? s = new decimal(1)) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new decimal(1)").WithArguments("s").WithLocation(20, 34)
                );
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
                // (1,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(1, 8),
                // (3,12): warning CS0169: The field 'S1.X' is never used
                //     object X = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "X").WithArguments("S1.X").WithLocation(3, 12),
                // (21,27): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     static void G2(S2 s = new()) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(21, 27));
        }

        [Fact]
        public void ParameterDefaultValues_03()
        {
            var source =
@"public struct S1 { }
public class Program
{
    public static void G1(S1 s = new()) { }
    public static void G2(S1 s = new S1()) { }
}";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                var g1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.G1");
                Assert.True(g1.Parameters[0].HasExplicitDefaultValue);
                Assert.Null(g1.Parameters[0].ExplicitDefaultValue);
                Assert.True(g1.Parameters[0].ExplicitDefaultConstantValue.IsNull);

                var g2 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.G2");
                Assert.True(g2.Parameters[0].HasExplicitDefaultValue);
                Assert.Null(g2.Parameters[0].ExplicitDefaultValue);
                Assert.True(g2.Parameters[0].ExplicitDefaultConstantValue.IsNull);
            }
        }

        [Fact]
        public void ParameterDefaultValues_04()
        {
            var source =
@"
public class Program
{
    public static void G1(bool? s = new()) { }
    public static void G2(bool? s = new bool()) { }
}";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                var g1 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.G1");
                Assert.True(g1.Parameters[0].HasExplicitDefaultValue);
                Assert.False((bool)g1.Parameters[0].ExplicitDefaultValue);
                Assert.False(g1.Parameters[0].ExplicitDefaultConstantValue.IsNull);

                var g2 = m.GlobalNamespace.GetMember<MethodSymbol>("Program.G2");
                Assert.True(g2.Parameters[0].HasExplicitDefaultValue);
                Assert.False((bool)g2.Parameters[0].ExplicitDefaultValue);
                Assert.False(g2.Parameters[0].ExplicitDefaultConstantValue.BooleanValue);
            }
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
                // (4,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(4, 8),
                // (6,12): warning CS0169: The field 'S1.X' is never used
                //     object X = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "X").WithArguments("S1.X").WithLocation(6, 12),
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
}
public struct S0
{
}
public struct S1
{
    public S1() { }
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
    }
}";
            comp = CreateCompilationWithMscorlib40(sourceB, references: new[] { refA });
            comp.VerifyEmitDiagnostics(
                // (6,18): error CS1757: Embedded interop struct 'S1' can contain only public instance fields.
                //         var s1 = i.F1();
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "i.F1()").WithArguments("S1").WithLocation(6, 18));
        }

        [WorkItem(60568, "https://github.com/dotnet/roslyn/issues/60568")]
        [Fact]
        public void FieldInitializer_EscapeAnalysis_01()
        {
            var source =
@"using System;

ref struct Example
{
    public Span<byte> Field = stackalloc byte[512];
    public Span<byte> Property { get; } = stackalloc byte[512];
    public Example() {}
}";
            var comp = CreateCompilationWithSpan(source);
            comp.VerifyDiagnostics(
                // (5,31): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(5, 31),
                // (6,43): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Property { get; } = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(6, 43));
        }

        [WorkItem(60568, "https://github.com/dotnet/roslyn/issues/60568")]
        [ConditionalFact(typeof(CoreClrOnly))] // For conversion from Span<T> to ReadOnlySpan<T>.
        public void FieldInitializer_EscapeAnalysis_02()
        {
            var source =
@"using System;

ref struct Example
{
    public ReadOnlySpan<int> Field = stackalloc int[512];
    public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
    public Example() {}
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (5,38): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //     public ReadOnlySpan<int> Field = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[512]").WithArguments("System.Span<int>").WithLocation(5, 38),
                // (6,50): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[512]").WithArguments("System.Span<int>").WithLocation(6, 50));
        }

        [WorkItem(60568, "https://github.com/dotnet/roslyn/issues/60568")]
        [Fact]
        public void FieldInitializer_EscapeAnalysis_03()
        {
            var source =
@"using System;
ref struct Example
{
    public Span<byte> Field;
}
ref struct E2
{
    public Span<byte> Field = new Example { Field = stackalloc byte[512] }.Field;
    public Span<byte> Property { get; } = new Example { Field = stackalloc byte[512] }.Field;
    public E2() { }
}";
            var comp = CreateCompilationWithSpan(source);
            comp.VerifyDiagnostics(
                // (8,45): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Field = new Example { Field = stackalloc byte[512] }.Field;
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "Field = stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(8, 45),
                // (9,57): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Property { get; } = new Example { Field = stackalloc byte[512] }.Field;
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "Field = stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(9, 57));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.Latest)]
        public void FieldInitializer_EscapeAnalysis_04(LanguageVersion languageVersion)
        {
            var source =
@"using System;
delegate Span<T> D<T>();
ref struct Example
{
    public Span<byte> Field = F(() => stackalloc byte[512]);
    public Span<byte> Property { get; } = F(() => stackalloc byte[512]);
    public Example() {}
    static Span<T> F<T>(D<T> d) => d();
}";
            var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (5,39): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Field = F(() => stackalloc byte[512]);
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(5, 39),
                // (6,51): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Property { get; } = F(() => stackalloc byte[512]);
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(6, 51));
        }

        [WorkItem(60568, "https://github.com/dotnet/roslyn/issues/60568")]
        [ConditionalFact(typeof(CoreClrOnly))] // For conversion from Span<T> to ReadOnlySpan<T>.
        public void FieldInitializer_EscapeAnalysis_05()
        {
            var source =
@"using System;
struct Example
{
    public Span<byte> Field = stackalloc byte[512];
    public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
    public Example() {}
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,12): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(4, 12),
                // (4,31): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(4, 31),
                // (5,12): error CS8345: Field or auto-implemented property cannot be of type 'ReadOnlySpan<int>' unless it is an instance member of a ref struct.
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "ReadOnlySpan<int>").WithArguments("System.ReadOnlySpan<int>").WithLocation(5, 12),
                // (5,50): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[512]").WithArguments("System.Span<int>").WithLocation(5, 50));
        }

        [WorkItem(60568, "https://github.com/dotnet/roslyn/issues/60568")]
        [ConditionalFact(typeof(CoreClrOnly))] // For conversion from Span<T> to ReadOnlySpan<T>.
        public void FieldInitializer_EscapeAnalysis_06()
        {
            var source =
@"using System;
record struct Example()
{
    public Span<byte> Field = stackalloc byte[512];
    public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,12): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(4, 12),
                // (4,31): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(4, 31),
                // (5,12): error CS8345: Field or auto-implemented property cannot be of type 'ReadOnlySpan<int>' unless it is an instance member of a ref struct.
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "ReadOnlySpan<int>").WithArguments("System.ReadOnlySpan<int>").WithLocation(5, 12),
                // (5,50): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[512]").WithArguments("System.Span<int>").WithLocation(5, 50));
        }

        [WorkItem(60568, "https://github.com/dotnet/roslyn/issues/60568")]
        [ConditionalFact(typeof(CoreClrOnly))] // For conversion from Span<T> to ReadOnlySpan<T>.
        public void FieldInitializer_EscapeAnalysis_07()
        {
            var source =
@"using System;
class Example
{
    public Span<byte> Field = stackalloc byte[512];
    public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,12): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(4, 12),
                // (4,31): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(4, 31),
                // (5,12): error CS8345: Field or auto-implemented property cannot be of type 'ReadOnlySpan<int>' unless it is an instance member of a ref struct.
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "ReadOnlySpan<int>").WithArguments("System.ReadOnlySpan<int>").WithLocation(5, 12),
                // (5,50): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[512]").WithArguments("System.Span<int>").WithLocation(5, 50));
        }

        [ConditionalFact(typeof(CoreClrOnly))] // For conversion from Span<T> to ReadOnlySpan<T>.
        public void FieldInitializer_EscapeAnalysis_Script()
        {
            var source =
@"using System;
Span<byte> s = stackalloc byte[512];
ReadOnlySpan<int> r = stackalloc int[512];
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Script, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (2,1): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                // Span<byte> s = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(2, 1),
                // (3,1): error CS8345: Field or auto-implemented property cannot be of type 'ReadOnlySpan<int>' unless it is an instance member of a ref struct.
                // ReadOnlySpan<int> r = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "ReadOnlySpan<int>").WithArguments("System.ReadOnlySpan<int>").WithLocation(3, 1));
        }

        [Fact]
        public void ImplicitlyInitializedField_Simple()
        {
            var source = @"
public struct S
{
    public int x;
    public S() { }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular10)
                .VerifyDiagnostics(
                    // (5,12): error CS0171: Field 'S.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                    //     public S() { }
                    Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.x", "11.0").WithLocation(5, 12));

            CreateCompilation(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings), parseOptions: TestOptions.Regular11)
                .VerifyDiagnostics(
                    // (5,12): warning CS9021: Control is returned to caller before field 'S.x' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S() { }
                    Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.x").WithLocation(5, 12));

            CreateCompilation(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(GetIdForErrorCode(ErrorCode.WRN_UnassignedThisSupportedVersion), ReportDiagnostic.Error), parseOptions: TestOptions.Regular11)
                .VerifyDiagnostics(
                // (5,12): error CS9021: Control is returned to caller before field 'S.x' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S() { }
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.x").WithLocation(5, 12).WithWarningAsError(true));

            var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("S..ctor()", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S.x""
  IL_0007:  ret
}
");
        }

        [Fact]
        public void ImplicitlyInitializedField_Pointer()
        {
            var source = """
using System;

_ = new R();

unsafe struct R
{
    public int* field;

    public R()
    {
        Console.WriteLine("explicit ctor");
    }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugExe);
            comp.VerifyDiagnostics(
                // (7,17): warning CS0649: Field 'R.field' is never assigned to, and will always have its default value
                //     public int* field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("R.field", "").WithLocation(7, 17)
                );
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: "explicit ctor");
            verifier.VerifyIL("R..ctor()", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""int* R.field""
  IL_0007:  initobj    ""int*""
  IL_000d:  ldstr      ""explicit ctor""
  IL_0012:  call       ""void System.Console.WriteLine(string)""
  IL_0017:  nop
  IL_0018:  ret
}");
        }

        [Fact]
        public void ImplicitlyInitializedField_NotOtherStruct()
        {
            var source = @"
public struct S
{
    public int x;
    public S() // 1
    {
        S other;
        other.x.ToString(); // 2

        S other2;
        other2.ToString(); // 3
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular10)
                .VerifyDiagnostics(
                    // (5,12): error CS0171: Field 'S.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                    //     public S() // 1
                    Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.x", "11.0").WithLocation(5, 12),
                    // (8,9): error CS0170: Use of possibly unassigned field 'x'
                    //         other.x.ToString(); // 2
                    Diagnostic(ErrorCode.ERR_UseDefViolationField, "other.x").WithArguments("x").WithLocation(8, 9),
                    // (11,9): error CS0165: Use of unassigned local variable 'other2'
                    //         other2.ToString(); // 3
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "other2").WithArguments("other2").WithLocation(11, 9));

            CreateCompilation(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings), parseOptions: TestOptions.Regular11)
                .VerifyDiagnostics(
                    // (5,12): warning CS9021: Control is returned to caller before field 'S.x' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S() // 1
                    Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.x").WithLocation(5, 12),
                    // (8,9): error CS0170: Use of possibly unassigned field 'x'
                    //         other.x.ToString(); // 2
                    Diagnostic(ErrorCode.ERR_UseDefViolationField, "other.x").WithArguments("x").WithLocation(8, 9),
                    // (11,9): error CS0165: Use of unassigned local variable 'other2'
                    //         other2.ToString(); // 3
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "other2").WithArguments("other2").WithLocation(11, 9));
        }

        [Fact]
        public void ImplicitlyInitializedField_ExplicitReturn()
        {
            var source = @"
public struct S
{
    public int x;
    public S()
    {
        return;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,9): error CS0171: Field 'S.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //         return;
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "return;").WithArguments("S.x", "11.0").WithLocation(7, 9));

            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings), parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics(
                // (7,9): warning CS9021: Control is returned to caller before field 'S.x' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //         return;
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "return;").WithArguments("S.x").WithLocation(7, 9));

            verifier.VerifyIL("S..ctor", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  stfld      ""int S.x""
  IL_0008:  br.s       IL_000a
  IL_000a:  ret
}
");
        }

        [Fact]
        public void ImplicitlyInitializedField_FieldLikeEvent()
        {
            var source = @"
using System;

public struct S
{
    public event Action E;
    public S()
    {
        E?.Invoke();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,12): error CS0171: Field 'S.E' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S()
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.E", "11.0").WithLocation(7, 12),
                // (9,9): error CS9015: Use of possibly unassigned field 'E'. Consider updating to language version '11.0' to auto-default the field.
                //         E?.Invoke();
                Diagnostic(ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion, "E").WithArguments("E", "11.0").WithLocation(9, 9));

            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings), parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics(
                // (7,12): warning CS9021: Control is returned to caller before field 'S.E' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S()
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.E").WithLocation(7, 12),
                // (9,9): warning CS9018: Field 'E' is read before being explicitly assigned, causing a preceding implicit assignment of 'default'.
                //         E?.Invoke();
                Diagnostic(ErrorCode.WRN_UseDefViolationFieldSupportedVersion, "E").WithArguments("E").WithLocation(9, 9));

            verifier.VerifyIL("S..ctor", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  stfld      ""System.Action S.E""
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""System.Action S.E""
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  pop
  IL_0012:  br.s       IL_001a
  IL_0014:  callvirt   ""void System.Action.Invoke()""
  IL_0019:  nop
  IL_001a:  ret
}
");
        }

        [Fact, WorkItem(66046, "https://github.com/dotnet/roslyn/issues/66046")]
        public void ImplicitlyInitializedField_ConstructorInitializer_01()
        {
            var source = """
public struct S1
{
    public int F;

    S1(int x) {}

    public S1() : this(F) {}
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,24): error CS0120: An object reference is required for the non-static field, method, or property 'S1.F'
                //     public S1() : this(F) {}
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F").WithArguments("S1.F").WithLocation(7, 24));
        }

        [Fact, WorkItem(66046, "https://github.com/dotnet/roslyn/issues/66046")]
        public void ImplicitlyInitializedField_ConstructorInitializer_02()
        {
            var source = """
public struct S1
{
    public int F;

    S1(int x) {}

    public static int M(int y) => y;

    public S1() : this(M(F)) {}
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,26): error CS0120: An object reference is required for the non-static field, method, or property 'S1.F'
                //     public S1() : this(M(F)) {}
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F").WithArguments("S1.F").WithLocation(9, 26));
        }

        [Fact, WorkItem(66046, "https://github.com/dotnet/roslyn/issues/66046")]
        public void ImplicitlyInitializedField_ConstructorInitializer_03()
        {
            var source = """
public struct S1
{
    public int F;

    S1(int x) {}

    public S1() : base(F) {}
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,12): error CS0522: 'S1': structs cannot call base class constructors
                //     public S1() : base(F) {}
                Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "S1").WithArguments("S1").WithLocation(7, 12),
                // (7,24): error CS0120: An object reference is required for the non-static field, method, or property 'S1.F'
                //     public S1() : base(F) {}
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F").WithArguments("S1.F").WithLocation(7, 24));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ImplicitlyInitializedFields_EmptyStruct(LanguageVersion languageVersion)
        {
            var source = @"
public struct S
{
    public S()
    {
    }
}";
            var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("S..ctor", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
");
        }

        [Fact]
        public void ImplicitlyInitializedFields_Nested_FullyInitialized_01()
        {
            var source = @"
public struct S1
{
    public int X, Y;
}

public struct S2
{
    public S1 S1;

    public S2()
    {
        S1.X = 42;
        S1.Y = 43;
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("S2..ctor", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""S1 S2.S1""
  IL_0007:  ldc.i4.s   42
  IL_0009:  stfld      ""int S1.X""
  IL_000e:  ldarg.0
  IL_000f:  ldflda     ""S1 S2.S1""
  IL_0014:  ldc.i4.s   43
  IL_0016:  stfld      ""int S1.Y""
  IL_001b:  ret
}
");
        }

        [Fact]
        public void ImplicitlyInitializedFields_Nested_FullyInitialized_02()
        {
            var source = @"
public struct S1
{
    public int X, Y;
}

public struct S2
{
    public S1 S1;

    public S2()
    {
        this = default;
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("S2..ctor", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  initobj    ""S2""
  IL_0008:  ret
}
");
        }

        [Fact]
        public void ImplicitlyInitializedFields_Nested_FullyInitialized_03()
        {
            var source = @"
public struct S1
{
    public int X, Y;
}

public struct S2
{
    public S1 S1;

    public S2()
    {
        S1 = default;
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("S2..ctor", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""S1 S2.S1""
  IL_0007:  initobj    ""S1""
  IL_000d:  ret
}
");
        }

        [Fact]
        [WorkItem(59890, "https://github.com/dotnet/roslyn/issues/59890")]
        public void ImplicitlyInitializedFields_Nested_PartiallyInitialized()
        {
            var source = @"
public struct S1
{
    public int X, Y;
}

public struct S2
{
    public S1 S1;

    public S2()
    {
        S1.X = 42;
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics(
                // (11,12): warning CS9022: Control is returned to caller before field 'S2.S1' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S2()
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S2").WithArguments("S2.S1").WithLocation(11, 12));

            verifier.VerifyIL("S2..ctor", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""S1 S2.S1""
  IL_0007:  initobj    ""S1""
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""S1 S2.S1""
  IL_0013:  ldc.i4.s   42
  IL_0015:  stfld      ""int S1.X""
  IL_001a:  ret
}
");
        }

        [Fact]
        [WorkItem(59890, "https://github.com/dotnet/roslyn/issues/59890")]
        public void ImplicitlyInitializedFields_Conditional_01()
        {
            var source = @"
public struct S
{
    public int X;

    public S(bool b)
    {
        if (b)
        {
            X = 42;
        }
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics(
                // (6,12): warning CS9022: Control is returned to caller before field 'S.X' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S(bool b)
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.X").WithLocation(6, 12));

            verifier.VerifyIL("S..ctor", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  stfld      ""int S.X""
  IL_0008:  ldarg.1
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  brfalse.s  IL_0017
  IL_000d:  nop
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   42
  IL_0011:  stfld      ""int S.X""
  IL_0016:  nop
  IL_0017:  ret
}
");
        }

        [Fact]
        public void ImplicitlyInitializedFields_Conditional_02()
        {
            var source = @"
public struct S1
{
    public int X, Y;
}

public struct S2
{
    public S1 S1;

    public S2(bool b)
    {
        if (b)
        {
            this = default;
        }
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics(
                // (11,12): warning CS9022: Control is returned to caller before field 'S2.S1' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S2(bool b)
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S2").WithArguments("S2.S1").WithLocation(11, 12));

            verifier.VerifyIL("S2..ctor", @"
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""S1 S2.S1""
  IL_0007:  initobj    ""S1""
  IL_000d:  ldarg.1
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  brfalse.s  IL_001b
  IL_0012:  nop
  IL_0013:  ldarg.0
  IL_0014:  initobj    ""S2""
  IL_001a:  nop
  IL_001b:  ret
}
");
        }

        [Fact]
        public void ImplicitlyInitializedFields_SequencePoints()
        {
            // note: our testing is relatively limited here because:
            // - there are no iterator constructors or async constructors
            // - the implicit initializations can only occur on constructors which lack constructor initializers
            var source = @"
public struct S
{
    public int X;

    public S()
    {
        X.ToString();
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics(
                // (6,12): warning CS9022: Control is returned to caller before field 'S.X' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S(string s!!)
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.X").WithLocation(6, 12),
                // (8,9): warning CS9019: Field 'X' is read before being explicitly assigned, causing a preceding implicit assignment of 'default'.
                //         X.ToString();
                Diagnostic(ErrorCode.WRN_UseDefViolationFieldSupportedVersion, "X").WithArguments("X").WithLocation(8, 9));

            verifier.VerifyIL("S..ctor", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  // sequence point: {
  IL_0000:  nop
  // sequence point: <hidden>
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  stfld      ""int S.X""
  // sequence point: X.ToString();
  IL_0008:  ldarg.0
  IL_0009:  ldflda     ""int S.X""
  IL_000e:  call       ""string int.ToString()""
  IL_0013:  pop
  // sequence point: }
  IL_0014:  ret
}
",
                sequencePoints: "S..ctor",
                source: source);
        }

        [Fact]
        public void ImplicitlyInitializedFields_SequencePoints_ExpressionBody()
        {
            var source = @"
public struct S
{
    public int X;

    public S()
        => X.ToString();
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics(
                // (6,12): warning CS9022: Control is returned to caller before field 'S.X' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S()
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.X").WithLocation(6, 12),
                // (7,12): warning CS9019: Field 'X' is read before being explicitly assigned, causing a preceding implicit assignment of 'default'.
                //         => X.ToString();
                Diagnostic(ErrorCode.WRN_UseDefViolationFieldSupportedVersion, "X").WithArguments("X").WithLocation(7, 12));

            verifier.VerifyIL("S..ctor", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S.X""
  // sequence point: X.ToString()
  IL_0007:  ldarg.0
  IL_0008:  ldflda     ""int S.X""
  IL_000d:  call       ""string int.ToString()""
  IL_0012:  pop
  IL_0013:  ret
}
",
                sequencePoints: "S..ctor",
                source: source);
        }

        [Fact]
        public void ImplicitlyInitializedFields_PragmaRestore()
        {
            var source = @"
public struct S
{
    public int X;
#pragma warning restore CS9022
    public S()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            comp.VerifyDiagnostics(
                // (6,12): warning CS9022: Control is returned to caller before field 'S.X' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S()
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S.X").WithLocation(6, 12));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitlyInitializedFields_PragmaDisable()
        {
            var source = @"
public struct S
{
    public int X;
#pragma warning disable CS9022
    public S()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitlyInitializedFields_AssignDefault()
        {
            var source = @"
#nullable enable

public struct SParameterless
{
    public string Field;
    public SParameterless() { Field = ""a""; }
}

public struct SEmpty
{
}

public struct S<T>
{
    public string AutoProp { get; set; }
    public T TField;
    public SParameterless Parameterless;
    public SEmpty Empty;

    public S()
    {
        AutoProp = default;
        TField = default;
        Parameterless = default;
        Empty = default;
    }

    public S(bool unused)
    {
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings));
            verifier.VerifyDiagnostics(
                // (21,12): warning CS8618: Non-nullable field 'TField' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "TField").WithLocation(21, 12),
                // (21,12): warning CS8618: Non-nullable property 'AutoProp' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public S()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("property", "AutoProp").WithLocation(21, 12),
                // (23,20): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         AutoProp = default;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default").WithLocation(23, 20),
                // (24,18): warning CS8601: Possible null reference assignment.
                //         TField = default;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "default").WithLocation(24, 18),
                // (29,12): warning CS8618: Non-nullable field 'TField' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "TField").WithLocation(29, 12),
                // (29,12): warning CS8618: Non-nullable property 'AutoProp' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("property", "AutoProp").WithLocation(29, 12),
                // (29,12): warning CS9022: Control is returned to caller before field 'S<T>.TField' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S<T>.TField").WithLocation(29, 12),
                // (29,12): warning CS9022: Control is returned to caller before field 'S<T>.Parameterless' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S").WithArguments("S<T>.Parameterless").WithLocation(29, 12),
                // (29,12): warning CS9021: Control is returned to caller before auto-implemented property 'S<T>.AutoProp' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion, "S").WithArguments("S<T>.AutoProp").WithLocation(29, 12));
            verifyIL();

            verifier = CompileAndVerify(source, options: TestOptions.DebugDll);
            verifier.VerifyDiagnostics(
                // (21,12): warning CS8618: Non-nullable field 'TField' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "TField").WithLocation(21, 12),
                // (21,12): warning CS8618: Non-nullable property 'AutoProp' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public S()
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("property", "AutoProp").WithLocation(21, 12),
                // (23,20): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         AutoProp = default;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default").WithLocation(23, 20),
                // (24,18): warning CS8601: Possible null reference assignment.
                //         TField = default;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "default").WithLocation(24, 18),
                // (29,12): warning CS8618: Non-nullable field 'TField' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "TField").WithLocation(29, 12),
                // (29,12): warning CS8618: Non-nullable property 'AutoProp' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("property", "AutoProp").WithLocation(29, 12));
            verifyIL();

            void verifyIL()
            {
                verifier.VerifyIL("S<T>..ctor()", @"
{
  // Code size       46 (0x2e)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  call       ""void S<T>.AutoProp.set""
  IL_0008:  nop
  IL_0009:  ldarg.0
  IL_000a:  ldflda     ""T S<T>.TField""
  IL_000f:  initobj    ""T""
  IL_0015:  ldarg.0
  IL_0016:  ldflda     ""SParameterless S<T>.Parameterless""
  IL_001b:  initobj    ""SParameterless""
  IL_0021:  ldarg.0
  IL_0022:  ldflda     ""SEmpty S<T>.Empty""
  IL_0027:  initobj    ""SEmpty""
  IL_002d:  ret
}
");

                verifier.VerifyIL("S<T>..ctor(bool)", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  stfld      ""string S<T>.<AutoProp>k__BackingField""
  IL_0008:  ldarg.0
  IL_0009:  ldflda     ""T S<T>.TField""
  IL_000e:  initobj    ""T""
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""SParameterless S<T>.Parameterless""
  IL_001a:  initobj    ""SParameterless""
  IL_0020:  ret
}
");
            }
        }

        [Fact]
        public void NonNullableReferenceTypeField()
        {
            var source =
@"public struct S
{
    public string Item;
    public S(bool unused)
    {
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (4,12): warning CS8618: Non-nullable field 'Item' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "Item").WithLocation(4, 12),
                // (4,12): error CS0171: Field 'S.Item' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S(bool unused)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.Item", "11.0").WithLocation(4, 12)
                );

            var verifier = CompileAndVerify(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS8618: Non-nullable field 'Item' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "Item").WithLocation(4, 12));
            verifier.VerifyIL("S..ctor", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""string S.Item""
  IL_0007:  ret
}
");
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Struct_ExplicitThisConstructorInitializer_01(LanguageVersion languageVersion)
        {
            var source =
@"public struct S
{
    public string Item;
    public S(bool unused) : this()
    {
    }
}";
            var verifier = CompileAndVerify(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            verifier.VerifyDiagnostics(
                // (4,12): warning CS8618: Non-nullable field 'Item' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     public S(bool unused)
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "Item").WithLocation(4, 12)
                );

            verifier.VerifyIL("S..ctor", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""S""
  IL_0007:  ret
}
");
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Struct_ExplicitThisConstructorInitializer_02(LanguageVersion languageVersion)
        {
            var source =
@"public struct S
{
    public string Item;
    public S(bool unused) : this()
    {
    }
    public S() { Item = ""a""; }
}";
            var verifier = CompileAndVerify(new[] { source }, options: WithNullableEnable(), parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("S..ctor(bool)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S..ctor()""
  IL_0006:  ret
}
");

            verifier.VerifyIL("S..ctor()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""a""
  IL_0006:  stfld      ""string S.Item""
  IL_000b:  ret
}
");
        }
    }
}
