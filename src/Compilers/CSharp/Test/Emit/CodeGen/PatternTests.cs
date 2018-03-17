// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class PatternTests : EmitMetadataTestBase
    {
        [Fact, WorkItem(18811, "https://github.com/dotnet/roslyn/issues/18811")]
        public void MissingNullable_01()
        {
            var source = @"namespace System {
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
static class C {
    public static bool M() => ((object)123) is int i;
}
";
            var compilation = CreateEmptyCompilation(source, options: TestOptions.ReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );
        }

        [Fact, WorkItem(18811, "https://github.com/dotnet/roslyn/issues/18811")]
        public void MissingNullable_02()
        {
            var source = @"namespace System {
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Nullable<T> where T : struct { }
}
static class C {
    public static bool M() => ((object)123) is int i;
}
";
            var compilation = CreateEmptyCompilation(source, options: TestOptions.UnsafeReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion)
                );
        }

        [Fact]
        public void MissingNullable_03()
        {
            var source = @"namespace System {
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Nullable<T> where T : struct { }
}
static class C {
    static void M1(int? x)
    {
        switch (x)
        {
            case int i: break;
        }
    }
    static bool M2(int? x) => x is int i;
}
";
            var compilation = CreateEmptyCompilation(source, options: TestOptions.UnsafeReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (14,18): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //             case int i: break;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(14, 18),
                // (14,18): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //             case int i: break;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(14, 18),
                // (12,17): error CS0656: Missing compiler required member 'System.Nullable`1.get_Value'
                //         switch (x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.Nullable`1", "get_Value").WithLocation(12, 17),
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(17, 36),
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(17, 36),
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.get_Value'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_Value").WithLocation(17, 36)
                );
        }

        [Fact]
        public void MissingNullable_04()
        {
            var source = @"namespace System {
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Nullable<T> where T : struct { public T GetValueOrDefault() => default(T); }
}
static class C {
    static void M1(int? x)
    {
        switch (x)
        {
            case int i: break;
        }
    }
    static bool M2(int? x) => x is int i;
}
";
            var compilation = CreateEmptyCompilation(source, options: TestOptions.UnsafeReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (14,18): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //             case int i: break;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(14, 18),
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(17, 36)
                );
        }

        [Fact, WorkItem(17266, "https://github.com/dotnet/roslyn/issues/17266")]
        public void DoubleEvaluation01()
        {
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        if (TryGet() is int index)
        {
            Console.WriteLine(index);
        }
    }

    public static int? TryGet()
    {
        Console.WriteLine(""eval"");
        return null;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"eval";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.Main",
@"{
  // Code size       44 (0x2c)
  .maxstack  1
  .locals init (int V_0, //index
                bool V_1,
                int? V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  call       ""int? C.TryGet()""
  IL_0006:  stloc.2
  IL_0007:  ldloca.s   V_2
  IL_0009:  call       ""bool int?.HasValue.get""
  IL_000e:  brfalse.s  IL_001d
  IL_0010:  ldloca.s   V_2
  IL_0012:  call       ""int int?.GetValueOrDefault()""
  IL_0017:  stloc.3
  IL_0018:  ldloc.3
  IL_0019:  stloc.0
  IL_001a:  ldc.i4.1
  IL_001b:  br.s       IL_001e
  IL_001d:  ldc.i4.0
  IL_001e:  stloc.1
  IL_001f:  ldloc.1
  IL_0020:  brfalse.s  IL_002b
  IL_0022:  nop
  IL_0023:  ldloc.0
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  nop
  IL_002a:  nop
  IL_002b:  ret
}");

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.Main",
@"{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (int V_0, //index
                int? V_1)
  IL_0000:  call       ""int? C.TryGet()""
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       ""bool int?.HasValue.get""
  IL_000d:  brfalse.s  IL_001d
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""int int?.GetValueOrDefault()""
  IL_0016:  stloc.0
  IL_0017:  ldloc.0
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ret
}");
        }

        [Fact, WorkItem(19122, "https://github.com/dotnet/roslyn/issues/19122")]
        public void PatternCrash_01()
        {
            var source = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Class2 : IDisposable
{
    public Class2(bool parameter = false)
    {
    }

    public void Dispose()
    {
    }
}

class X<T>
{
    IdentityAccessor<T> idAccessor = new IdentityAccessor<T>();
    void Y<U>() where U : T
    {
        // BUG: The following line is the problem
        if (GetT().FirstOrDefault(p => idAccessor.GetId(p) == Guid.Empty) is U u)
        {
        }
    }

    IEnumerable<T> GetT()
    {
        yield return default(T);
    }
}
class IdentityAccessor<T>
{
    public Guid GetId(T t)
    {
        return Guid.Empty;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll, references: new[] { LinqAssemblyRef });
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("X<T>.Y<U>",
@"{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (U V_0, //u
                bool V_1,
                T V_2,
                U V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""System.Collections.Generic.IEnumerable<T> X<T>.GetT()""
  IL_0007:  ldarg.0
  IL_0008:  ldftn      ""bool X<T>.<Y>b__1_0<U>(T)""
  IL_000e:  newobj     ""System.Func<T, bool>..ctor(object, System.IntPtr)""
  IL_0013:  call       ""T System.Linq.Enumerable.FirstOrDefault<T>(System.Collections.Generic.IEnumerable<T>, System.Func<T, bool>)""
  IL_0018:  stloc.2
  IL_0019:  ldloc.2
  IL_001a:  box        ""T""
  IL_001f:  isinst     ""U""
  IL_0024:  brfalse.s  IL_0037
  IL_0026:  ldloc.2
  IL_0027:  box        ""T""
  IL_002c:  unbox.any  ""U""
  IL_0031:  stloc.3
  IL_0032:  ldloc.3
  IL_0033:  stloc.0
  IL_0034:  ldc.i4.1
  IL_0035:  br.s       IL_0038
  IL_0037:  ldc.i4.0
  IL_0038:  stloc.1
  IL_0039:  ldloc.1
  IL_003a:  brfalse.s  IL_003e
  IL_003c:  nop
  IL_003d:  nop
  IL_003e:  ret
}");
        }

        [Fact, WorkItem(24522, "https://github.com/dotnet/roslyn/issues/24522")]
        public void IgnoreDeclaredConversion_01()
        {
            var source =
@"class Base<T>
{
    public static implicit operator Derived(Base<T> obj)
    {
        return new Derived();
    }
}

class Derived : Base<object>
{
}

class Program
{
    static void Main(string[] args)
    {
        Base<object> x = new Derived();
        System.Console.WriteLine(x is Derived);
        System.Console.WriteLine(x is Derived y);
        switch (x)
        {
            case Derived z: System.Console.WriteLine(true); break;
        }
        System.Console.WriteLine(null != (x as Derived));
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, references: new[] { LinqAssemblyRef });
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
True
True
True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Program.Main",
@"{
  // Code size      106 (0x6a)
  .maxstack  2
  .locals init (Base<object> V_0, //x
                Derived V_1, //y
                Derived V_2,
                Derived V_3, //z
                Base<object> V_4,
                Derived V_5,
                Base<object> V_6)
  IL_0000:  nop
  IL_0001:  newobj     ""Derived..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  isinst     ""Derived""
  IL_000d:  ldnull
  IL_000e:  cgt.un
  IL_0010:  call       ""void System.Console.WriteLine(bool)""
  IL_0015:  nop
  IL_0016:  ldloc.0
  IL_0017:  isinst     ""Derived""
  IL_001c:  brfalse.s  IL_002a
  IL_001e:  ldloc.0
  IL_001f:  castclass  ""Derived""
  IL_0024:  stloc.2
  IL_0025:  ldloc.2
  IL_0026:  stloc.1
  IL_0027:  ldc.i4.1
  IL_0028:  br.s       IL_002b
  IL_002a:  ldc.i4.0
  IL_002b:  call       ""void System.Console.WriteLine(bool)""
  IL_0030:  nop
  IL_0031:  ldloc.0
  IL_0032:  stloc.s    V_6
  IL_0034:  ldloc.s    V_6
  IL_0036:  stloc.s    V_4
  IL_0038:  ldloc.s    V_4
  IL_003a:  isinst     ""Derived""
  IL_003f:  brfalse.s  IL_005a
  IL_0041:  ldloc.s    V_4
  IL_0043:  castclass  ""Derived""
  IL_0048:  stloc.s    V_5
  IL_004a:  br.s       IL_004c
  IL_004c:  ldloc.s    V_5
  IL_004e:  stloc.3
  IL_004f:  br.s       IL_0051
  IL_0051:  ldc.i4.1
  IL_0052:  call       ""void System.Console.WriteLine(bool)""
  IL_0057:  nop
  IL_0058:  br.s       IL_005a
  IL_005a:  ldloc.0
  IL_005b:  isinst     ""Derived""
  IL_0060:  ldnull
  IL_0061:  cgt.un
  IL_0063:  call       ""void System.Console.WriteLine(bool)""
  IL_0068:  nop
  IL_0069:  ret
}");
        }

        [Fact]
        public void DoublePattern01()
        {
            var source =
@"using System;
class Program
{
    static bool P1(double d) => d is double.NaN;
    static bool P2(float f) => f is float.NaN;
    static bool P3(double d) => d is 3.14d;
    static bool P4(float f) => f is 3.14f;
    static bool P5(object o)
    {
        switch (o)
        {
            case double.NaN: return true;
            case float.NaN: return true;
            case 3.14d: return true;
            case 3.14f: return true;
            default: return false;
        }
    }
    public static void Main(string[] args)
    {
        Console.Write(P1(double.NaN));
        Console.Write(P1(1.0));
        Console.Write(P2(float.NaN));
        Console.Write(P2(1.0f));
        Console.Write(P3(3.14));
        Console.Write(P3(double.NaN));
        Console.Write(P4(3.14f));
        Console.Write(P4(float.NaN));
        Console.Write(P5(double.NaN));
        Console.Write(P5(0.0d));
        Console.Write(P5(float.NaN));
        Console.Write(P5(0.0f));
        Console.Write(P5(3.14d));
        Console.Write(P5(125));
        Console.Write(P5(3.14f));
        Console.Write(P5(1.0f));
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"TrueFalseTrueFalseTrueFalseTrueFalseTrueFalseTrueFalseTrueFalseTrueFalse";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Program.P1",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""bool double.IsNaN(double)""
  IL_0006:  ret
}");
            compVerifier.VerifyIL("Program.P2",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""bool float.IsNaN(float)""
  IL_0006:  ret
}");
            compVerifier.VerifyIL("Program.P3",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.r8     3.14
  IL_0009:  ldarg.0
  IL_000a:  ceq
  IL_000c:  ret
}");
            compVerifier.VerifyIL("Program.P4",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldc.r4     3.14
  IL_0005:  ldarg.0
  IL_0006:  ceq
  IL_0008:  ret
}");
            compVerifier.VerifyIL("Program.P5",
@"{
  // Code size      103 (0x67)
  .maxstack  2
  .locals init (object V_0,
                double V_1,
                float V_2,
                object V_3,
                bool V_4)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.3
  IL_0003:  ldloc.3
  IL_0004:  stloc.0
  IL_0005:  ldloc.0
  IL_0006:  isinst     ""double""
  IL_000b:  brfalse.s  IL_002a
  IL_000d:  ldloc.0
  IL_000e:  unbox.any  ""double""
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  call       ""bool double.IsNaN(double)""
  IL_001a:  brtrue.s   IL_004b
  IL_001c:  ldc.r8     3.14
  IL_0025:  ldloc.1
  IL_0026:  beq.s      IL_0055
  IL_0028:  br.s       IL_005f
  IL_002a:  ldloc.0
  IL_002b:  isinst     ""float""
  IL_0030:  brfalse.s  IL_005f
  IL_0032:  ldloc.0
  IL_0033:  unbox.any  ""float""
  IL_0038:  stloc.2
  IL_0039:  ldloc.2
  IL_003a:  call       ""bool float.IsNaN(float)""
  IL_003f:  brtrue.s   IL_0050
  IL_0041:  ldc.r4     3.14
  IL_0046:  ldloc.2
  IL_0047:  beq.s      IL_005a
  IL_0049:  br.s       IL_005f
  IL_004b:  ldc.i4.1
  IL_004c:  stloc.s    V_4
  IL_004e:  br.s       IL_0064
  IL_0050:  ldc.i4.1
  IL_0051:  stloc.s    V_4
  IL_0053:  br.s       IL_0064
  IL_0055:  ldc.i4.1
  IL_0056:  stloc.s    V_4
  IL_0058:  br.s       IL_0064
  IL_005a:  ldc.i4.1
  IL_005b:  stloc.s    V_4
  IL_005d:  br.s       IL_0064
  IL_005f:  ldc.i4.0
  IL_0060:  stloc.s    V_4
  IL_0062:  br.s       IL_0064
  IL_0064:  ldloc.s    V_4
  IL_0066:  ret
}");
        }

        [Fact]
        public void DecimalEquality()
        {
            // demonstrate that pattern-matching against a decimal constant is
            // at least as efficient as simply using ==
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        Console.Write(M1(1.0m));
        Console.Write(M2(1.0m));
        Console.Write(M1(2.0m));
        Console.Write(M2(2.0m));
    }

    static int M1(decimal d)
    {
        if (M(d) is 1.0m) return 1;
        return 0;
    }

    static int M2(decimal d)
    {
        if (M(d) == 1.0m) return 1;
        return 0;
    }

    public static decimal M(decimal d)
    {
        return d;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1100";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       39 (0x27)
  .maxstack  5
  .locals init (bool V_0,
                decimal V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""decimal C.M(decimal)""
  IL_0007:  stloc.1
  IL_0008:  ldc.i4.s   10
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.1
  IL_000e:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0013:  ldloc.1
  IL_0014:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  brfalse.s  IL_0021
  IL_001d:  ldc.i4.1
  IL_001e:  stloc.2
  IL_001f:  br.s       IL_0025
  IL_0021:  ldc.i4.0
  IL_0022:  stloc.2
  IL_0023:  br.s       IL_0025
  IL_0025:  ldloc.2
  IL_0026:  ret
}");
            compVerifier.VerifyIL("C.M2",
@"{
  // Code size       37 (0x25)
  .maxstack  6
  .locals init (bool V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""decimal C.M(decimal)""
  IL_0007:  ldc.i4.s   10
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.1
  IL_000d:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0012:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  brfalse.s  IL_001f
  IL_001b:  ldc.i4.1
  IL_001c:  stloc.1
  IL_001d:  br.s       IL_0023
  IL_001f:  ldc.i4.0
  IL_0020:  stloc.1
  IL_0021:  br.s       IL_0023
  IL_0023:  ldloc.1
  IL_0024:  ret
}");

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       30 (0x1e)
  .maxstack  5
  .locals init (decimal V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""decimal C.M(decimal)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.s   10
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.1
  IL_000d:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0012:  ldloc.0
  IL_0013:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0018:  brfalse.s  IL_001c
  IL_001a:  ldc.i4.1
  IL_001b:  ret
  IL_001c:  ldc.i4.0
  IL_001d:  ret
}");
            compVerifier.VerifyIL("C.M2",
@"{
  // Code size       28 (0x1c)
  .maxstack  6
  IL_0000:  ldarg.0
  IL_0001:  call       ""decimal C.M(decimal)""
  IL_0006:  ldc.i4.s   10
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.1
  IL_000c:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0011:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0016:  brfalse.s  IL_001a
  IL_0018:  ldc.i4.1
  IL_0019:  ret
  IL_001a:  ldc.i4.0
  IL_001b:  ret
}");
        }

        [Fact, WorkItem(16878, "https://github.com/dotnet/roslyn/issues/16878")]
        public void RedundantNullCheck()
        {
            var source =
@"public class C
{
    static int M1(bool? b1, bool b2)
    {
        switch (b1)
        {
            case null:
                return 1;
            case var _ when b2:
                return 2;
            case true:
                return 3;
            case false:
                return 4;
        }
    }

    static int M2(object o, bool b)
    {
        switch (o)
        {
            case string a when b:
                return 1;
            case string a:
                return 2;
        }
        return 3;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (bool? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool bool?.HasValue.get""
  IL_0009:  brfalse.s  IL_0018
  IL_000b:  br.s       IL_001a
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""bool bool?.GetValueOrDefault()""
  IL_0014:  brtrue.s   IL_001f
  IL_0016:  br.s       IL_0021
  IL_0018:  ldc.i4.1
  IL_0019:  ret
  IL_001a:  ldarg.1
  IL_001b:  brfalse.s  IL_000d
  IL_001d:  ldc.i4.2
  IL_001e:  ret
  IL_001f:  ldc.i4.3
  IL_0020:  ret
  IL_0021:  ldc.i4.4
  IL_0022:  ret
}");
            compVerifier.VerifyIL("C.M2",
@"{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (object V_0,
                string V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  isinst     ""string""
  IL_0008:  brfalse.s  IL_0018
  IL_000a:  ldloc.0
  IL_000b:  castclass  ""string""
  IL_0010:  stloc.1
  IL_0011:  ldarg.1
  IL_0012:  brfalse.s  IL_0016
  IL_0014:  ldc.i4.1
  IL_0015:  ret
  IL_0016:  ldc.i4.2
  IL_0017:  ret
  IL_0018:  ldc.i4.3
  IL_0019:  ret
}");
        }

        [Fact, WorkItem(12813, "https://github.com/dotnet/roslyn/issues/12813")]
        public void NoBoxingOnIntegerConstantPattern()
        {
            var source =
@"public class C
{
    static bool M1(int x)
    {
        return x is 42;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size        6 (0x6)
  .maxstack  2
  IL_0000:  ldc.i4.s   42
  IL_0002:  ldarg.0
  IL_0003:  ceq
  IL_0005:  ret
}");
        }
    }
}
