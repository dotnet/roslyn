// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        #region Miscellaneous

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
                // (14,18): error CS0656: Missing compiler required member 'System.Nullable`1.get_Value'
                //             case int i: break;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_Value").WithLocation(14, 18),
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
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init (int V_0, //index
                bool V_1,
                int? V_2)
  IL_0000:  nop
  IL_0001:  call       ""int? C.TryGet()""
  IL_0006:  stloc.2
  IL_0007:  ldloca.s   V_2
  IL_0009:  call       ""bool int?.HasValue.get""
  IL_000e:  brfalse.s  IL_001b
  IL_0010:  ldloca.s   V_2
  IL_0012:  call       ""int int?.GetValueOrDefault()""
  IL_0017:  stloc.0
  IL_0018:  ldc.i4.1
  IL_0019:  br.s       IL_001c
  IL_001b:  ldc.i4.0
  IL_001c:  stloc.1
  IL_001d:  ldloc.1
  IL_001e:  brfalse.s  IL_0029
  IL_0020:  nop
  IL_0021:  ldloc.0
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  nop
  IL_0028:  nop
  IL_0029:  ret
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("X<T>.Y<U>",
@"{
  // Code size       61 (0x3d)
  .maxstack  3
  .locals init (U V_0, //u
                bool V_1,
                T V_2)
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
  IL_0024:  brfalse.s  IL_0035
  IL_0026:  ldloc.2
  IL_0027:  box        ""T""
  IL_002c:  unbox.any  ""U""
  IL_0031:  stloc.0
  IL_0032:  ldc.i4.1
  IL_0033:  br.s       IL_0036
  IL_0035:  ldc.i4.0
  IL_0036:  stloc.1
  IL_0037:  ldloc.1
  IL_0038:  brfalse.s  IL_003c
  IL_003a:  nop
  IL_003b:  nop
  IL_003c:  ret
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
True
True
True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Program.Main",
@"{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (Base<object> V_0, //x
                Derived V_1, //y
                Derived V_2, //z
                Base<object> V_3,
                Base<object> V_4)
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
  IL_001c:  stloc.1
  IL_001d:  ldloc.1
  IL_001e:  ldnull
  IL_001f:  cgt.un
  IL_0021:  call       ""void System.Console.WriteLine(bool)""
  IL_0026:  nop
  IL_0027:  ldloc.0
  IL_0028:  stloc.s    V_4
  IL_002a:  ldloc.s    V_4
  IL_002c:  stloc.3
  IL_002d:  ldloc.3
  IL_002e:  isinst     ""Derived""
  IL_0033:  stloc.2
  IL_0034:  ldloc.2
  IL_0035:  brtrue.s   IL_0039
  IL_0037:  br.s       IL_0044
  IL_0039:  br.s       IL_003b
  IL_003b:  ldc.i4.1
  IL_003c:  call       ""void System.Console.WriteLine(bool)""
  IL_0041:  nop
  IL_0042:  br.s       IL_0044
  IL_0044:  ldloc.0
  IL_0045:  isinst     ""Derived""
  IL_004a:  ldnull
  IL_004b:  cgt.un
  IL_004d:  call       ""void System.Console.WriteLine(bool)""
  IL_0052:  nop
  IL_0053:  ret
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
  IL_0000:  ldarg.0
  IL_0001:  ldc.r8     3.14
  IL_000a:  ceq
  IL_000c:  ret
}");
            compVerifier.VerifyIL("Program.P4",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.r4     3.14
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
  IL_0015:  ldc.r8     3.14
  IL_001e:  beq.s      IL_0055
  IL_0020:  ldloc.1
  IL_0021:  call       ""bool double.IsNaN(double)""
  IL_0026:  brtrue.s   IL_004b
  IL_0028:  br.s       IL_005f
  IL_002a:  ldloc.0
  IL_002b:  isinst     ""float""
  IL_0030:  brfalse.s  IL_005f
  IL_0032:  ldloc.0
  IL_0033:  unbox.any  ""float""
  IL_0038:  stloc.2
  IL_0039:  ldloc.2
  IL_003a:  ldc.r4     3.14
  IL_003f:  beq.s      IL_005a
  IL_0041:  ldloc.2
  IL_0042:  call       ""bool float.IsNaN(float)""
  IL_0047:  brtrue.s   IL_0050
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
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (string V_0) //a
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""string""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0011
  IL_000a:  ldarg.1
  IL_000b:  brfalse.s  IL_000f
  IL_000d:  ldc.i4.1
  IL_000e:  ret
  IL_000f:  ldc.i4.2
  IL_0010:  ret
  IL_0011:  ldc.i4.3
  IL_0012:  ret
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
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  ceq
  IL_0005:  ret
}");
        }

        [Fact, WorkItem(40403, "https://github.com/dotnet/roslyn/issues/40403")]
        public void RefParameter_StoreToTemp()
        {
            var source =
@"public class C
{
    static bool M1(ref object x)
    {
        return x is 42;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  isinst     ""int""
  IL_0009:  brfalse.s  IL_0016
  IL_000b:  ldloc.0
  IL_000c:  unbox.any  ""int""
  IL_0011:  ldc.i4.s   42
  IL_0013:  ceq
  IL_0015:  ret
  IL_0016:  ldc.i4.0
  IL_0017:  ret
}");
        }

        [Fact, WorkItem(40403, "https://github.com/dotnet/roslyn/issues/40403")]
        public void RefLocal_StoreToTemp()
        {
            var source =
@"public class C
{
    static bool M1(bool b, ref object x, ref object y)
    {
        ref object z = ref b ? ref x : ref y;
        return z is 42;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldarg.2
  IL_0004:  br.s       IL_0007
  IL_0006:  ldarg.1
  IL_0007:  ldind.ref
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  isinst     ""int""
  IL_000f:  brfalse.s  IL_001c
  IL_0011:  ldloc.0
  IL_0012:  unbox.any  ""int""
  IL_0017:  ldc.i4.s   42
  IL_0019:  ceq
  IL_001b:  ret
  IL_001c:  ldc.i4.0
  IL_001d:  ret
}");
        }

        [Fact, WorkItem(40403, "https://github.com/dotnet/roslyn/issues/40403")]
        public void InParameter_StoreToTemp()
        {
            var source =
@"public class C
{
    static bool M1(in object x)
    {
        return x is 42;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  isinst     ""int""
  IL_0009:  brfalse.s  IL_0016
  IL_000b:  ldloc.0
  IL_000c:  unbox.any  ""int""
  IL_0011:  ldc.i4.s   42
  IL_0013:  ceq
  IL_0015:  ret
  IL_0016:  ldc.i4.0
  IL_0017:  ret
}");
        }

        [Fact, WorkItem(40403, "https://github.com/dotnet/roslyn/issues/40403")]
        public void RefReadonlyLocal_StoreToTemp()
        {
            var source =
@"public class C
{
    static bool M1(bool b, in object x, in object y)
    {
        ref readonly object z = ref b ? ref x : ref y;
        return z is 42;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldarg.2
  IL_0004:  br.s       IL_0007
  IL_0006:  ldarg.1
  IL_0007:  ldind.ref
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  isinst     ""int""
  IL_000f:  brfalse.s  IL_001c
  IL_0011:  ldloc.0
  IL_0012:  unbox.any  ""int""
  IL_0017:  ldc.i4.s   42
  IL_0019:  ceq
  IL_001b:  ret
  IL_001c:  ldc.i4.0
  IL_001d:  ret
}");
        }

        [Fact, WorkItem(40403, "https://github.com/dotnet/roslyn/issues/40403")]
        public void OutParameter_StoreToTemp()
        {
            var source =
@"public class C
{
    static bool M1(out object x)
    {
        x = null;
        return x is 42;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1",
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stind.ref
  IL_0003:  ldarg.0
  IL_0004:  ldind.ref
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  isinst     ""int""
  IL_000c:  brfalse.s  IL_0019
  IL_000e:  ldloc.0
  IL_000f:  unbox.any  ""int""
  IL_0014:  ldc.i4.s   42
  IL_0016:  ceq
  IL_0018:  ret
  IL_0019:  ldc.i4.0
  IL_001a:  ret
}");
        }

        [Fact, WorkItem(22654, "https://github.com/dotnet/roslyn/issues/22654")]
        public void NoRedundantTypeCheck()
        {
            var source =
@"using System;
public class C
{
    public void SwitchBasedPatternMatching(object o)
    {
        switch (o)
        {
            case int n when n == 1:
                Console.WriteLine(""1""); break;
            case string s:
                Console.WriteLine(""s""); break;
            case int n when n == 2:
                Console.WriteLine(""2""); break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.SwitchBasedPatternMatching",
@"{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (int V_0, //n
                object V_1)
  IL_0000:  ldarg.1
  IL_0001:  stloc.1
  IL_0002:  ldloc.1
  IL_0003:  isinst     ""int""
  IL_0008:  brfalse.s  IL_0013
  IL_000a:  ldloc.1
  IL_000b:  unbox.any  ""int""
  IL_0010:  stloc.0
  IL_0011:  br.s       IL_001c
  IL_0013:  ldloc.1
  IL_0014:  isinst     ""string""
  IL_0019:  brtrue.s   IL_002b
  IL_001b:  ret
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.1
  IL_001e:  bne.un.s   IL_0036
  IL_0020:  ldstr      ""1""
  IL_0025:  call       ""void System.Console.WriteLine(string)""
  IL_002a:  ret
  IL_002b:  ldstr      ""s""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  ret
  IL_0036:  ldloc.0
  IL_0037:  ldc.i4.2
  IL_0038:  bne.un.s   IL_0044
  IL_003a:  ldstr      ""2""
  IL_003f:  call       ""void System.Console.WriteLine(string)""
  IL_0044:  ret
}");
        }

        [Fact, WorkItem(15437, "https://github.com/dotnet/roslyn/issues/15437")]
        public void IsTypeDiscard()
        {
            var source =
@"public class C
{
    public bool IsString(object o)
    {
        return o is string _;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.IsString",
@"{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""string""
  IL_0006:  ldnull
  IL_0007:  cgt.un
  IL_0009:  ret
}");
        }

        [Fact, WorkItem(19150, "https://github.com/dotnet/roslyn/issues/19150")]
        public void RedundantHasValue()
        {
            var source =
@"using System;
public class C
{
    public static void M(int? x)
    {
        switch (x)
        {
            case int i:
                Console.Write(i);
                break;
            case null:
                Console.Write(""null"");
                break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       33 (0x21)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brfalse.s  IL_0016
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""int int?.GetValueOrDefault()""
  IL_0010:  call       ""void System.Console.Write(int)""
  IL_0015:  ret
  IL_0016:  ldstr      ""null""
  IL_001b:  call       ""void System.Console.Write(string)""
  IL_0020:  ret
}");
        }

        [Fact, WorkItem(19153, "https://github.com/dotnet/roslyn/issues/19153")]
        public void RedundantBox()
        {
            var source = @"using System;
public class C
{
    public static void M<T, U>(U x) where T : U
    {
        // when T is not known to be a reference type, there is an unboxing conversion from
        // a type parameter U to T, provided T depends on U.
        switch (x)
        {
            case T i:
                Console.Write(i);
                break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M<T, U>(U)",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""U""
  IL_0006:  isinst     ""T""
  IL_000b:  brfalse.s  IL_0022
  IL_000d:  ldarg.0
  IL_000e:  box        ""U""
  IL_0013:  unbox.any  ""T""
  IL_0018:  box        ""T""
  IL_001d:  call       ""void System.Console.Write(object)""
  IL_0022:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead01()
        {
            var source = @"using System;

public class Person {
    public string Name { get; set; }
}

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: ""Bill"" }:
                Console.WriteLine(""Hey Bill!"");
            break;
            case { Name: ""Bob"" }:
                Console.WriteLine(""Hey Bob!"");
            break;
            case { Name: var name }:
                Console.WriteLine($""Hello {name}!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (string V_0) //name
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0051
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldstr      ""Bill""
  IL_0010:  call       ""bool string.op_Equality(string, string)""
  IL_0015:  brtrue.s   IL_0026
  IL_0017:  ldloc.0
  IL_0018:  ldstr      ""Bob""
  IL_001d:  call       ""bool string.op_Equality(string, string)""
  IL_0022:  brtrue.s   IL_0031
  IL_0024:  br.s       IL_003c
  IL_0026:  ldstr      ""Hey Bill!""
  IL_002b:  call       ""void System.Console.WriteLine(string)""
  IL_0030:  ret
  IL_0031:  ldstr      ""Hey Bob!""
  IL_0036:  call       ""void System.Console.WriteLine(string)""
  IL_003b:  ret
  IL_003c:  ldstr      ""Hello ""
  IL_0041:  ldloc.0
  IL_0042:  ldstr      ""!""
  IL_0047:  call       ""string string.Concat(string, string, string)""
  IL_004c:  call       ""void System.Console.WriteLine(string)""
  IL_0051:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead02()
        {
            var source = @"using System;

public class Person {
    public string Name { get; set; }
    public int Age;
}

public class C {
    public void M(Person p) {
        switch (p) {
            case Person { Name: var name, Age: 0}:
                Console.WriteLine($""Hello baby { name }!"");
            break;
            case Person { Name: var name }:
                Console.WriteLine($""Hello { name }!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       64 (0x40)
  .maxstack  3
  .locals init (string V_0, //name
                string V_1) //name
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_003f
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  stloc.0
  IL_000a:  ldarg.1
  IL_000b:  ldfld      ""int Person.Age""
  IL_0010:  brtrue.s   IL_0028
  IL_0012:  ldstr      ""Hello baby ""
  IL_0017:  ldloc.0
  IL_0018:  ldstr      ""!""
  IL_001d:  call       ""string string.Concat(string, string, string)""
  IL_0022:  call       ""void System.Console.WriteLine(string)""
  IL_0027:  ret
  IL_0028:  ldloc.0
  IL_0029:  stloc.1
  IL_002a:  ldstr      ""Hello ""
  IL_002f:  ldloc.1
  IL_0030:  ldstr      ""!""
  IL_0035:  call       ""string string.Concat(string, string, string)""
  IL_003a:  call       ""void System.Console.WriteLine(string)""
  IL_003f:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead03()
        {
            var source = @"using System;

public class Person {
    public string Name { get; set; }
}

public class Student : Person { }

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: ""Bill"" }:
                Console.WriteLine(""Hey Bill!"");
            break;
            case Student { Name: var name }:
                Console.WriteLine($""Hello student { name}!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       65 (0x41)
  .maxstack  3
  .locals init (string V_0) //name
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0040
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldstr      ""Bill""
  IL_0010:  call       ""bool string.op_Equality(string, string)""
  IL_0015:  brtrue.s   IL_0020
  IL_0017:  ldarg.1
  IL_0018:  isinst     ""Student""
  IL_001d:  brtrue.s   IL_002b
  IL_001f:  ret
  IL_0020:  ldstr      ""Hey Bill!""
  IL_0025:  call       ""void System.Console.WriteLine(string)""
  IL_002a:  ret
  IL_002b:  ldstr      ""Hello student ""
  IL_0030:  ldloc.0
  IL_0031:  ldstr      ""!""
  IL_0036:  call       ""string string.Concat(string, string, string)""
  IL_003b:  call       ""void System.Console.WriteLine(string)""
  IL_0040:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead04()
        {
            // Cannot combine the evaluations of name here, since we first check if p is Teacher,
            // and only if that fails check if p is null. 
            // Combining the evaluations would mean first checking if p is null, then evaluating name, then checking if p is Teacher.
            // This would not necessarily be more performant.
            var source = @"using System;

public class Person {
    public string Name { get; set; }
}

public class Teacher : Person { }

public class C {
    public void M(Person p) {
        switch (p) {
            case Teacher { Name: var name }:
                Console.WriteLine($""Hello teacher { name}!"");
            break;
            case Person { Name: var name }:
                Console.WriteLine($""Hello { name}!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       75 (0x4b)
  .maxstack  3
  .locals init (string V_0, //name
                string V_1) //name
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""Teacher""
  IL_0006:  brfalse.s  IL_0011
  IL_0008:  ldarg.1
  IL_0009:  callvirt   ""string Person.Name.get""
  IL_000e:  stloc.0
  IL_000f:  br.s       IL_001d
  IL_0011:  ldarg.1
  IL_0012:  brfalse.s  IL_004a
  IL_0014:  ldarg.1
  IL_0015:  callvirt   ""string Person.Name.get""
  IL_001a:  stloc.0
  IL_001b:  br.s       IL_0033
  IL_001d:  ldstr      ""Hello teacher ""
  IL_0022:  ldloc.0
  IL_0023:  ldstr      ""!""
  IL_0028:  call       ""string string.Concat(string, string, string)""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ret
  IL_0033:  ldloc.0
  IL_0034:  stloc.1
  IL_0035:  ldstr      ""Hello ""
  IL_003a:  ldloc.1
  IL_003b:  ldstr      ""!""
  IL_0040:  call       ""string string.Concat(string, string, string)""
  IL_0045:  call       ""void System.Console.WriteLine(string)""
  IL_004a:  ret
}
");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead05()
        {
            // Cannot combine the evaluations of name here, since we first check if p is Teacher,
            // and only if that fails check if p is Student. 
            // Combining the evaluations would mean first checking if p is null, 
            // then evaluating name, 
            // then checking if p is Teacher, 
            // then checking if p is Student.
            // This would not necessarily be more performant.
            var source = @"using System;

public class Person {
    public string Name { get; set; }
}

public class Student : Person { }

public class Teacher : Person { }

public class C {
    public void M(Person p) {
        switch (p) {
            case Teacher { Name: var name }:
                Console.WriteLine($""Hello teacher { name}!"");
            break;
            case Student { Name: var name }:
                Console.WriteLine($""Hello student { name}!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       80 (0x50)
  .maxstack  3
  .locals init (string V_0, //name
                string V_1) //name
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""Teacher""
  IL_0006:  brfalse.s  IL_0011
  IL_0008:  ldarg.1
  IL_0009:  callvirt   ""string Person.Name.get""
  IL_000e:  stloc.0
  IL_000f:  br.s       IL_0022
  IL_0011:  ldarg.1
  IL_0012:  isinst     ""Student""
  IL_0017:  brfalse.s  IL_004f
  IL_0019:  ldarg.1
  IL_001a:  callvirt   ""string Person.Name.get""
  IL_001f:  stloc.0
  IL_0020:  br.s       IL_0038
  IL_0022:  ldstr      ""Hello teacher ""
  IL_0027:  ldloc.0
  IL_0028:  ldstr      ""!""
  IL_002d:  call       ""string string.Concat(string, string, string)""
  IL_0032:  call       ""void System.Console.WriteLine(string)""
  IL_0037:  ret
  IL_0038:  ldloc.0
  IL_0039:  stloc.1
  IL_003a:  ldstr      ""Hello student ""
  IL_003f:  ldloc.1
  IL_0040:  ldstr      ""!""
  IL_0045:  call       ""string string.Concat(string, string, string)""
  IL_004a:  call       ""void System.Console.WriteLine(string)""
  IL_004f:  ret
}
");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead06()
        {
            var source = @"using System;

public class Person {
    public string Name { get; set; }
}

public class Student : Person { }

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: { Length: 0 } }:
                Console.WriteLine(""Hello!"");
            break;
            case Student { Name: { Length : 1 } }:
                Console.WriteLine($""Hello student!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (string V_0,
                int V_1)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0039
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  brfalse.s  IL_0039
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""int string.Length.get""
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_0024
  IL_0017:  ldarg.1
  IL_0018:  isinst     ""Student""
  IL_001d:  brfalse.s  IL_0039
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  beq.s      IL_002f
  IL_0023:  ret
  IL_0024:  ldstr      ""Hello!""
  IL_0029:  call       ""void System.Console.WriteLine(string)""
  IL_002e:  ret
  IL_002f:  ldstr      ""Hello student!""
  IL_0034:  call       ""void System.Console.WriteLine(string)""
  IL_0039:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead07()
        {
            // Cannot combine the evaluations of name here, since we first check if name is MemoryStream,
            // and only if that fails check if name is null. 
            // Combining the evaluations would mean first checking if name is null, then evaluating name, then checking if p is Teacher.
            // This would not necessarily be more performant.
            var source = @"using System;
using System.IO;

public class Person {
    public Stream Name { get; set; }
}

public class C {
    public void M(Person p) {
        switch (p) {
            case Person { Name: MemoryStream { Length: 1 } }:
                Console.WriteLine(""Your Names A MemoryStream!"");
            break;
            case Person { Name: { Length : var x } }:
                Console.WriteLine(""Your Names A Stream!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       66 (0x42)
  .maxstack  2
  .locals init (System.IO.Stream V_0,
                System.IO.MemoryStream V_1)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0041
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""System.IO.Stream Person.Name.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  isinst     ""System.IO.MemoryStream""
  IL_0010:  stloc.1
  IL_0011:  ldloc.1
  IL_0012:  brfalse.s  IL_0020
  IL_0014:  ldloc.1
  IL_0015:  callvirt   ""long System.IO.Stream.Length.get""
  IL_001a:  ldc.i4.1
  IL_001b:  conv.i8
  IL_001c:  beq.s      IL_002c
  IL_001e:  br.s       IL_0023
  IL_0020:  ldloc.0
  IL_0021:  brfalse.s  IL_0041
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""long System.IO.Stream.Length.get""
  IL_0029:  pop
  IL_002a:  br.s       IL_0037
  IL_002c:  ldstr      ""Your Names A MemoryStream!""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ret
  IL_0037:  ldstr      ""Your Names A Stream!""
  IL_003c:  call       ""void System.Console.WriteLine(string)""
  IL_0041:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead08()
        {
            var source = @"
public class Person {
    public string Name { get; set; }
}

public class Student : Person { }

public class C {
    public string M(Person p) {
        return p switch  {
            { Name: ""Bill"" } => ""Hey Bill!"",
            Student { Name: var name } => ""Hello student { name}!"",
            _ => null,
        };
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       51 (0x33)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_002f
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  ldstr      ""Bill""
  IL_000e:  call       ""bool string.op_Equality(string, string)""
  IL_0013:  brtrue.s   IL_001f
  IL_0015:  ldarg.1
  IL_0016:  isinst     ""Student""
  IL_001b:  brtrue.s   IL_0027
  IL_001d:  br.s       IL_002f
  IL_001f:  ldstr      ""Hey Bill!""
  IL_0024:  stloc.0
  IL_0025:  br.s       IL_0031
  IL_0027:  ldstr      ""Hello student { name}!""
  IL_002c:  stloc.0
  IL_002d:  br.s       IL_0031
  IL_002f:  ldnull
  IL_0030:  stloc.0
  IL_0031:  ldloc.0
  IL_0032:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead09()
        {
            var source = @"using System;

public class Person {
    public virtual string Name { get; set; }
}

public class Student : Person { }

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: ""Bill"" }:
                Console.WriteLine(""Hey Bill!"");
            break;
            case Student { Name: var name }:
                Console.WriteLine($""Hello student { name}!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       65 (0x41)
  .maxstack  3
  .locals init (string V_0) //name
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0040
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldstr      ""Bill""
  IL_0010:  call       ""bool string.op_Equality(string, string)""
  IL_0015:  brtrue.s   IL_0020
  IL_0017:  ldarg.1
  IL_0018:  isinst     ""Student""
  IL_001d:  brtrue.s   IL_002b
  IL_001f:  ret
  IL_0020:  ldstr      ""Hey Bill!""
  IL_0025:  call       ""void System.Console.WriteLine(string)""
  IL_002a:  ret
  IL_002b:  ldstr      ""Hello student ""
  IL_0030:  ldloc.0
  IL_0031:  ldstr      ""!""
  IL_0036:  call       ""string string.Concat(string, string, string)""
  IL_003b:  call       ""void System.Console.WriteLine(string)""
  IL_0040:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void NoRedundantPropertyRead10()
        {
            // Currently we don't combine these redundant property reads.
            // However we could do so in the future.

            var source = @"using System;

public abstract class Person {
    public abstract string Name { get; set; }
}

public class Student : Person { 
    public override string Name { get; set; }
}

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: ""Bill"" }:
                Console.WriteLine(""Hey Bill!"");
            break;
            case Student { Name: var name }:
                Console.WriteLine($""Hello student { name}!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (string V_0, //name
                Student V_1)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0048
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  ldstr      ""Bill""
  IL_000e:  call       ""bool string.op_Equality(string, string)""
  IL_0013:  brtrue.s   IL_0028
  IL_0015:  ldarg.1
  IL_0016:  isinst     ""Student""
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  brfalse.s  IL_0048
  IL_001f:  ldloc.1
  IL_0020:  callvirt   ""string Person.Name.get""
  IL_0025:  stloc.0
  IL_0026:  br.s       IL_0033
  IL_0028:  ldstr      ""Hey Bill!""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ret
  IL_0033:  ldstr      ""Hello student ""
  IL_0038:  ldloc.0
  IL_0039:  ldstr      ""!""
  IL_003e:  call       ""string string.Concat(string, string, string)""
  IL_0043:  call       ""void System.Console.WriteLine(string)""
  IL_0048:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void CombiningRedundantPropertyReadsDoesNotChangeNullabilityAnalysis01()
        {
            // Currently we don't combine the redundant property reads at all.
            // However this could be improved in the future so this test is important

            var source = @"using System;

#nullable enable

public class Person {
    public virtual string? Name { get; }
}

public class Student : Person {
    public override string Name { get => base.Name ?? """"; }
}

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: ""Bill""}:
                Console.WriteLine(""Hey Bill!"");
            break;
            case Student { Name: var name }:
                Console.WriteLine($""Student has name of length { name.Length }!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (string V_0, //name
                Student V_1)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_004d
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  ldstr      ""Bill""
  IL_000e:  call       ""bool string.op_Equality(string, string)""
  IL_0013:  brtrue.s   IL_0028
  IL_0015:  ldarg.1
  IL_0016:  isinst     ""Student""
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  brfalse.s  IL_004d
  IL_001f:  ldloc.1
  IL_0020:  callvirt   ""string Person.Name.get""
  IL_0025:  stloc.0
  IL_0026:  br.s       IL_0033
  IL_0028:  ldstr      ""Hey Bill!""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ret
  IL_0033:  ldstr      ""Student has name of length {0}!""
  IL_0038:  ldloc.0
  IL_0039:  callvirt   ""int string.Length.get""
  IL_003e:  box        ""int""
  IL_0043:  call       ""string string.Format(string, object)""
  IL_0048:  call       ""void System.Console.WriteLine(string)""
  IL_004d:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void CombiningRedundantPropertyReadsDoesNotChangeNullabilityAnalysis02()
        {
            var source = @"using System;

#nullable enable

public class Person {
    public string? Name { get;}
}

public class Student : Person {
}

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: var name } when name is null:
                Console.WriteLine($""Hey { name }"");
            break;
            case Student { Name: var name } when name != null:
                Console.WriteLine($""Student has name of length { name.Length }!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (string V_0, //name
                string V_1, //name
                Person V_2)
  IL_0000:  ldarg.1
  IL_0001:  stloc.2
  IL_0002:  ldloc.2
  IL_0003:  brfalse.s  IL_004a
  IL_0005:  ldloc.2
  IL_0006:  callvirt   ""string Person.Name.get""
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_0017
  IL_000e:  ldloc.2
  IL_000f:  isinst     ""Student""
  IL_0014:  brtrue.s   IL_002b
  IL_0016:  ret
  IL_0017:  ldloc.0
  IL_0018:  brtrue.s   IL_000e
  IL_001a:  ldstr      ""Hey ""
  IL_001f:  ldloc.0
  IL_0020:  call       ""string string.Concat(string, string)""
  IL_0025:  call       ""void System.Console.WriteLine(string)""
  IL_002a:  ret
  IL_002b:  ldloc.0
  IL_002c:  stloc.1
  IL_002d:  ldloc.1
  IL_002e:  brfalse.s  IL_004a
  IL_0030:  ldstr      ""Student has name of length {0}!""
  IL_0035:  ldloc.1
  IL_0036:  callvirt   ""int string.Length.get""
  IL_003b:  box        ""int""
  IL_0040:  call       ""string string.Format(string, object)""
  IL_0045:  call       ""void System.Console.WriteLine(string)""
  IL_004a:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void CombiningRedundantPropertyReadsDoesNotChangeNullabilityAnalysis03()
        {
            var source = @"using System;

#nullable enable

public class Person {
    public string? Name { get; }
}

public class Student : Person {
}

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: string name }:
                Console.WriteLine($""Hey {name}"");
            break;
            case Student { Name: var name }:
                Console.WriteLine($""Student has name of length { name.Length }!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (19,66): warning CS8602: Dereference of a possibly null reference.
                //                 Console.WriteLine($"Student has name of length { name.Length }!");
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "name").WithLocation(19, 66));
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (string V_0, //name
                string V_1) //name
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0043
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  brtrue.s   IL_0016
  IL_000d:  ldarg.1
  IL_000e:  isinst     ""Student""
  IL_0013:  brtrue.s   IL_0027
  IL_0015:  ret
  IL_0016:  ldstr      ""Hey ""
  IL_001b:  ldloc.0
  IL_001c:  call       ""string string.Concat(string, string)""
  IL_0021:  call       ""void System.Console.WriteLine(string)""
  IL_0026:  ret
  IL_0027:  ldloc.0
  IL_0028:  stloc.1
  IL_0029:  ldstr      ""Student has name of length {0}!""
  IL_002e:  ldloc.1
  IL_002f:  callvirt   ""int string.Length.get""
  IL_0034:  box        ""int""
  IL_0039:  call       ""string string.Format(string, object)""
  IL_003e:  call       ""void System.Console.WriteLine(string)""
  IL_0043:  ret
}");
        }

        [Fact, WorkItem(34933, "https://github.com/dotnet/roslyn/issues/34933")]
        public void DoNotCombineDifferentPropertyReadsWithSameName()
        {
            var source = @"using System;

public class Person {
    public string Name { get; set; }
}

public class Student : Person {
    public new string Name { get; set; }
}

public class C {
    public void M(Person p) {
        switch (p) {
            case { Name: ""Bill"" }:
                Console.WriteLine(""Hey Bill!"");
            break;
            case Student { Name: var name }:
                Console.WriteLine($""Hello student { name}!"");
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (string V_0, //name
                Student V_1)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0048
  IL_0003:  ldarg.1
  IL_0004:  callvirt   ""string Person.Name.get""
  IL_0009:  ldstr      ""Bill""
  IL_000e:  call       ""bool string.op_Equality(string, string)""
  IL_0013:  brtrue.s   IL_0028
  IL_0015:  ldarg.1
  IL_0016:  isinst     ""Student""
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  brfalse.s  IL_0048
  IL_001f:  ldloc.1
  IL_0020:  callvirt   ""string Student.Name.get""
  IL_0025:  stloc.0
  IL_0026:  br.s       IL_0033
  IL_0028:  ldstr      ""Hey Bill!""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ret
  IL_0033:  ldstr      ""Hello student ""
  IL_0038:  ldloc.0
  IL_0039:  ldstr      ""!""
  IL_003e:  call       ""string string.Concat(string, string, string)""
  IL_0043:  call       ""void System.Console.WriteLine(string)""
  IL_0048:  ret
}");
        }

        [Fact, WorkItem(20641, "https://github.com/dotnet/roslyn/issues/20641")]
        public void PatternsVsAs01()
        {
            var source = @"using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main() { }

    internal static bool TryGetCount1<T>(IEnumerable<T> source, out int count)
    {
        ICollection nonGeneric = source as ICollection;
        if (nonGeneric != null)
        {
            count = nonGeneric.Count;
            return true;
        }

        ICollection<T> generic = source as ICollection<T>;
        if (generic != null)
        {
            count = generic.Count;
            return true;
        }

        count = -1;
        return false;
    }

    internal static bool TryGetCount2<T>(IEnumerable<T> source, out int count)
    {
        switch (source)
        {
            case ICollection nonGeneric:
                count = nonGeneric.Count;
                return true;

            case ICollection<T> generic:
                count = generic.Count;
                return true;

            default:
                count = -1;
                return false;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("Program.TryGetCount1<T>",
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (System.Collections.ICollection V_0, //nonGeneric
                System.Collections.Generic.ICollection<T> V_1) //generic
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""System.Collections.ICollection""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  ldarg.1
  IL_000b:  ldloc.0
  IL_000c:  callvirt   ""int System.Collections.ICollection.Count.get""
  IL_0011:  stind.i4
  IL_0012:  ldc.i4.1
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  isinst     ""System.Collections.Generic.ICollection<T>""
  IL_001a:  stloc.1
  IL_001b:  ldloc.1
  IL_001c:  brfalse.s  IL_0028
  IL_001e:  ldarg.1
  IL_001f:  ldloc.1
  IL_0020:  callvirt   ""int System.Collections.Generic.ICollection<T>.Count.get""
  IL_0025:  stind.i4
  IL_0026:  ldc.i4.1
  IL_0027:  ret
  IL_0028:  ldarg.1
  IL_0029:  ldc.i4.m1
  IL_002a:  stind.i4
  IL_002b:  ldc.i4.0
  IL_002c:  ret
}");
            compVerifier.VerifyIL("Program.TryGetCount2<T>",
@"{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (System.Collections.ICollection V_0, //nonGeneric
                System.Collections.Generic.ICollection<T> V_1) //generic
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""System.Collections.ICollection""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_0016
  IL_000a:  ldarg.0
  IL_000b:  isinst     ""System.Collections.Generic.ICollection<T>""
  IL_0010:  stloc.1
  IL_0011:  ldloc.1
  IL_0012:  brtrue.s   IL_0020
  IL_0014:  br.s       IL_002a
  IL_0016:  ldarg.1
  IL_0017:  ldloc.0
  IL_0018:  callvirt   ""int System.Collections.ICollection.Count.get""
  IL_001d:  stind.i4
  IL_001e:  ldc.i4.1
  IL_001f:  ret
  IL_0020:  ldarg.1
  IL_0021:  ldloc.1
  IL_0022:  callvirt   ""int System.Collections.Generic.ICollection<T>.Count.get""
  IL_0027:  stind.i4
  IL_0028:  ldc.i4.1
  IL_0029:  ret
  IL_002a:  ldarg.1
  IL_002b:  ldc.i4.m1
  IL_002c:  stind.i4
  IL_002d:  ldc.i4.0
  IL_002e:  ret
}");
        }

        [Fact, WorkItem(20641, "https://github.com/dotnet/roslyn/issues/20641")]
        public void PatternsVsAs02()
        {
            var source = @"using System.Collections;
class Program
{
    static void Main() { }

    internal static bool IsEmpty1(IEnumerable source)
    {
        var c = source as ICollection;
        return c != null && c.Count > 0;
    }

    internal static bool IsEmpty2(IEnumerable source)
    {
        return source is ICollection c && c.Count > 0;
    }

}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("Program.IsEmpty1",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.Collections.ICollection V_0) //c
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""System.Collections.ICollection""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  ldloc.0
  IL_000b:  callvirt   ""int System.Collections.ICollection.Count.get""
  IL_0010:  ldc.i4.0
  IL_0011:  cgt
  IL_0013:  ret
  IL_0014:  ldc.i4.0
  IL_0015:  ret
}");
            compVerifier.VerifyIL("Program.IsEmpty2",
@"{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.Collections.ICollection V_0) //c
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""System.Collections.ICollection""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  ldloc.0
  IL_000b:  callvirt   ""int System.Collections.ICollection.Count.get""
  IL_0010:  ldc.i4.0
  IL_0011:  cgt
  IL_0013:  ret
  IL_0014:  ldc.i4.0
  IL_0015:  ret
}");
        }

        [Fact]
        [WorkItem(20641, "https://github.com/dotnet/roslyn/issues/20641")]
        [WorkItem(1395, "https://github.com/dotnet/csharplang/issues/1395")]
        public void TupleSwitch01()
        {
            var source = @"using System;

public class Door
{
    public DoorState State;

    public enum DoorState { Opened, Closed, Locked }

    public enum Action { Open, Close, Lock, Unlock }

    public void Act0(Action action, bool haveKey = false)
    {
        Console.Write($""{State} {action}{(haveKey ? "" withKey"" : null)}"");
        State = ChangeState0(State, action, haveKey);
        Console.WriteLine($"" -> {State}"");
    }

    public void Act1(Action action, bool haveKey = false)
    {
        Console.Write($""{State} {action}{(haveKey ? "" withKey"" : null)}"");
        State = ChangeState1(State, action, haveKey);
        Console.WriteLine($"" -> {State}"");
    }

    public static DoorState ChangeState0(DoorState state, Action action, bool haveKey = false)
    {
        switch (state, action)
        {
            case (DoorState.Opened, Action.Close):
                return DoorState.Closed;
            case (DoorState.Closed, Action.Open):
                return DoorState.Opened;
            case (DoorState.Closed, Action.Lock) when haveKey:
                return DoorState.Locked;
            case (DoorState.Locked, Action.Unlock) when haveKey:
                return DoorState.Closed;
            case var (oldState, _):
                return oldState;
        }
    }

    public static DoorState ChangeState1(DoorState state, Action action, bool haveKey = false) =>
        (state, action) switch {
            (DoorState.Opened, Action.Close) => DoorState.Closed,
            (DoorState.Closed, Action.Open) => DoorState.Opened,
            (DoorState.Closed, Action.Lock) when haveKey => DoorState.Locked,
            (DoorState.Locked, Action.Unlock) when haveKey => DoorState.Closed,
            _ => state };
}

class Program
{
    static void Main(string[] args)
    {
        var door = new Door();
        door.Act0(Door.Action.Close);
        door.Act0(Door.Action.Lock);
        door.Act0(Door.Action.Lock, true);
        door.Act0(Door.Action.Open);
        door.Act0(Door.Action.Unlock);
        door.Act0(Door.Action.Unlock, true);
        door.Act0(Door.Action.Open);
        Console.WriteLine();

        door = new Door();
        door.Act1(Door.Action.Close);
        door.Act1(Door.Action.Lock);
        door.Act1(Door.Action.Lock, true);
        door.Act1(Door.Action.Open);
        door.Act1(Door.Action.Unlock);
        door.Act1(Door.Action.Unlock, true);
        door.Act1(Door.Action.Open);
    }
}";
            var expectedOutput =
@"Opened Close -> Closed
Closed Lock -> Closed
Closed Lock withKey -> Locked
Locked Open -> Locked
Locked Unlock -> Locked
Locked Unlock withKey -> Closed
Closed Open -> Opened

Opened Close -> Closed
Closed Lock -> Closed
Closed Lock withKey -> Locked
Locked Open -> Locked
Locked Unlock -> Locked
Locked Unlock withKey -> Closed
Closed Open -> Opened
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Door.ChangeState0",
@"{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (Door.DoorState V_0, //oldState
                Door.Action V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  switch    (
        IL_0018,
        IL_001e,
        IL_0027)
  IL_0016:  br.s       IL_003b
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  beq.s      IL_002d
  IL_001c:  br.s       IL_003b
  IL_001e:  ldloc.1
  IL_001f:  brfalse.s  IL_002f
  IL_0021:  ldloc.1
  IL_0022:  ldc.i4.2
  IL_0023:  beq.s      IL_0031
  IL_0025:  br.s       IL_003b
  IL_0027:  ldloc.1
  IL_0028:  ldc.i4.3
  IL_0029:  beq.s      IL_0036
  IL_002b:  br.s       IL_003b
  IL_002d:  ldc.i4.1
  IL_002e:  ret
  IL_002f:  ldc.i4.0
  IL_0030:  ret
  IL_0031:  ldarg.2
  IL_0032:  brfalse.s  IL_003b
  IL_0034:  ldc.i4.2
  IL_0035:  ret
  IL_0036:  ldarg.2
  IL_0037:  brfalse.s  IL_003b
  IL_0039:  ldc.i4.1
  IL_003a:  ret
  IL_003b:  ldloc.0
  IL_003c:  ret
}");
            compVerifier.VerifyIL("Door.ChangeState1",
@"{
  // Code size       71 (0x47)
  .maxstack  2
  .locals init (Door.DoorState V_0,
                Door.DoorState V_1,
                Door.Action V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldarg.1
  IL_0003:  stloc.2
  IL_0004:  ldloc.1
  IL_0005:  switch    (
        IL_0018,
        IL_001e,
        IL_0027)
  IL_0016:  br.s       IL_0043
  IL_0018:  ldloc.2
  IL_0019:  ldc.i4.1
  IL_001a:  beq.s      IL_002d
  IL_001c:  br.s       IL_0043
  IL_001e:  ldloc.2
  IL_001f:  brfalse.s  IL_0031
  IL_0021:  ldloc.2
  IL_0022:  ldc.i4.2
  IL_0023:  beq.s      IL_0035
  IL_0025:  br.s       IL_0043
  IL_0027:  ldloc.2
  IL_0028:  ldc.i4.3
  IL_0029:  beq.s      IL_003c
  IL_002b:  br.s       IL_0043
  IL_002d:  ldc.i4.1
  IL_002e:  stloc.0
  IL_002f:  br.s       IL_0045
  IL_0031:  ldc.i4.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_0045
  IL_0035:  ldarg.2
  IL_0036:  brfalse.s  IL_0043
  IL_0038:  ldc.i4.2
  IL_0039:  stloc.0
  IL_003a:  br.s       IL_0045
  IL_003c:  ldarg.2
  IL_003d:  brfalse.s  IL_0043
  IL_003f:  ldc.i4.1
  IL_0040:  stloc.0
  IL_0041:  br.s       IL_0045
  IL_0043:  ldarg.0
  IL_0044:  stloc.0
  IL_0045:  ldloc.0
  IL_0046:  ret
}");
        }

        [Fact]
        [WorkItem(20641, "https://github.com/dotnet/roslyn/issues/20641")]
        [WorkItem(1395, "https://github.com/dotnet/csharplang/issues/1395")]
        public void SharingTemps01()
        {
            var source =
@"class Program
{
    static void Main(string[] args) { }
    void M1(string x)
    {
        switch (x)
        {
            case ""a"":
            case ""b"" when Mutate(ref x): // prevents sharing temps
            case ""c"":
                break;
        }
    }
    void M2(string x)
    {
        switch (x)
        {
            case ""a"":
            case ""b"" when Pure(x):
            case ""c"":
                break;
        }
    }
    static bool Mutate(ref string x) { x = null; return false; }
    static bool Pure(string x) { return false; }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithoutRecursivePatterns);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("Program.M1",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldstr      ""a""
  IL_0008:  call       ""bool string.op_Equality(string, string)""
  IL_000d:  brtrue.s   IL_0031
  IL_000f:  ldloc.0
  IL_0010:  ldstr      ""b""
  IL_0015:  call       ""bool string.op_Equality(string, string)""
  IL_001a:  brtrue.s   IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""c""
  IL_0022:  call       ""bool string.op_Equality(string, string)""
  IL_0027:  pop
  IL_0028:  ret
  IL_0029:  ldarga.s   V_1
  IL_002b:  call       ""bool Program.Mutate(ref string)""
  IL_0030:  pop
  IL_0031:  ret
}");
            compVerifier.VerifyIL("Program.M2",
@"{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldstr      ""a""
  IL_0008:  call       ""bool string.op_Equality(string, string)""
  IL_000d:  brtrue.s   IL_0030
  IL_000f:  ldloc.0
  IL_0010:  ldstr      ""b""
  IL_0015:  call       ""bool string.op_Equality(string, string)""
  IL_001a:  brtrue.s   IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""c""
  IL_0022:  call       ""bool string.op_Equality(string, string)""
  IL_0027:  pop
  IL_0028:  ret
  IL_0029:  ldarg.1
  IL_002a:  call       ""bool Program.Pure(string)""
  IL_002f:  pop
  IL_0030:  ret
}");
        }

        [Fact, WorkItem(17266, "https://github.com/dotnet/roslyn/issues/17266")]
        public void IrrefutablePatternInIs01()
        {
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        if (Get() is int index) { }
        Console.WriteLine(index);
    }

    public static int Get()
    {
        Console.WriteLine(""eval"");
        return 1;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"eval
1";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(17266, "https://github.com/dotnet/roslyn/issues/17266")]
        public void IrrefutablePatternInIs02()
        {
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        if (Get() is Assignment(int left, var right)) { }
        Console.WriteLine(left);
        Console.WriteLine(right);
    }

    public static Assignment Get()
    {
        Console.WriteLine(""eval"");
        return new Assignment(1, 2);
    }
}
public struct Assignment
{
    public int Left, Right;
    public Assignment(int left, int right) => (Left, Right) = (left, right);
    public void Deconstruct(out int left, out int right) => (left, right) = (Left, Right);
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"eval
1
2";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void MissingNullCheck01()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        string s = null;
        System.Console.WriteLine(s is string { Length: 3 });
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(24550, "https://github.com/dotnet/roslyn/issues/24550")]
        [WorkItem(1284, "https://github.com/dotnet/csharplang/issues/1284")]
        public void ConstantPatternVsUnconstrainedTypeParameter01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine(Test1<object>(null));
        Console.WriteLine(Test1<int>(1));
        Console.WriteLine(Test1<int?>(null));
        Console.WriteLine(Test1<int?>(1));

        Console.WriteLine(Test2<object>(0));
        Console.WriteLine(Test2<int>(1));
        Console.WriteLine(Test2<int?>(0));
        Console.WriteLine(Test2<string>(""frog""));

        Console.WriteLine(Test3<object>(""frog""));
        Console.WriteLine(Test3<int>(1));
        Console.WriteLine(Test3<string>(""frog""));
        Console.WriteLine(Test3<int?>(1));
    }

    public static bool Test1<T>(T t)
    {
        return t is null;
    }
    public static bool Test2<T>(T t)
    {
        return t is 0;
    }
    public static bool Test3<T>(T t)
    {
        return t is ""frog"";
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
False
True
False
True
False
True
False
True
False
True
False
";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Program.Test1<T>(T)",
@"{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  ldnull
  IL_0007:  ceq
  IL_0009:  ret
}");
            compVerifier.VerifyIL("Program.Test2<T>(T)",
@"{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  isinst     ""int""
  IL_000b:  brfalse.s  IL_0021
  IL_000d:  ldarg.0
  IL_000e:  box        ""T""
  IL_0013:  isinst     ""int""
  IL_0018:  unbox.any  ""int""
  IL_001d:  ldc.i4.0
  IL_001e:  ceq
  IL_0020:  ret
  IL_0021:  ldc.i4.0
  IL_0022:  ret
}
");
            compVerifier.VerifyIL("Program.Test3<T>(T)",
@"{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  isinst     ""string""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brfalse.s  IL_001b
  IL_000f:  ldloc.0
  IL_0010:  ldstr      ""frog""
  IL_0015:  call       ""bool string.op_Equality(string, string)""
  IL_001a:  ret
  IL_001b:  ldc.i4.0
  IL_001c:  ret
}");
        }

        [Fact]
        [WorkItem(24550, "https://github.com/dotnet/roslyn/issues/24550")]
        [WorkItem(1284, "https://github.com/dotnet/csharplang/issues/1284")]
        public void ConstantPatternVsUnconstrainedTypeParameter02()
        {
            var source =
@"class C<T>
{
    internal struct S { }
    static bool Test(S s)
    {
        return s is 1;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C<T>.Test(C<T>.S)",
@"{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""C<T>.S""
  IL_0006:  isinst     ""int""
  IL_000b:  brfalse.s  IL_0021
  IL_000d:  ldarg.0
  IL_000e:  box        ""C<T>.S""
  IL_0013:  isinst     ""int""
  IL_0018:  unbox.any  ""int""
  IL_001d:  ldc.i4.1
  IL_001e:  ceq
  IL_0020:  ret
  IL_0021:  ldc.i4.0
  IL_0022:  ret
}
");
        }

        [Fact]
        [WorkItem(26274, "https://github.com/dotnet/roslyn/issues/26274")]
        public void VariablesInSwitchExpressionArms()
        {
            var source =
@"class C
{
    public override bool Equals(object obj) =>
        obj switch
        {
            C x1 when x1 is var x2 => x2 is var x3 && x3 is {},
            _ => false
        };
    public override int GetHashCode() => 1;
    public static void Main()
    {
        C c = new C();
        System.Console.Write(c.Equals(new C()));
        System.Console.Write(c.Equals(new object()));
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "TrueFalse");
            compVerifier.VerifyIL("C.Equals(object)",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (C V_0, //x1
                C V_1, //x2
                C V_2, //x3
                bool V_3)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""C""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0015
  IL_000a:  ldloc.0
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  stloc.2
  IL_000e:  ldloc.2
  IL_000f:  ldnull
  IL_0010:  cgt.un
  IL_0012:  stloc.3
  IL_0013:  br.s       IL_0017
  IL_0015:  ldc.i4.0
  IL_0016:  stloc.3
  IL_0017:  ldloc.3
  IL_0018:  ret
}");
        }

        [Fact, WorkItem(26387, "https://github.com/dotnet/roslyn/issues/26387")]
        public void ValueTypeArgument01()
        {
            var source =
@"using System;

class TestHelper
{
    static void Main()
    {
        Console.WriteLine(IsValueTypeT0(new S()));
        Console.WriteLine(IsValueTypeT1(new S()));
        Console.WriteLine(IsValueTypeT2(new S()));
    }

    static bool IsValueTypeT0<T>(T result)
    {
        return result is T;
    }
    static bool IsValueTypeT1<T>(T result)
    {
        return result is T v;
    }
    static bool IsValueTypeT2<T>(T result)
    {
        return result is T _;
    }
}

struct S { }
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
True
True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("TestHelper.IsValueTypeT0<T>(T)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  box        ""T""
  IL_0007:  ldnull
  IL_0008:  cgt.un
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_000d
  IL_000d:  ldloc.0
  IL_000e:  ret
}");
            compVerifier.VerifyIL("TestHelper.IsValueTypeT1<T>(T)",
@"{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (T V_0, //v
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  box        ""T""
  IL_0007:  brfalse.s  IL_000e
  IL_0009:  ldarg.0
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  br.s       IL_000f
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.1
  IL_0010:  br.s       IL_0012
  IL_0012:  ldloc.1
  IL_0013:  ret
}");
            compVerifier.VerifyIL("TestHelper.IsValueTypeT2<T>(T)",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  box        ""T""
  IL_0007:  ldnull
  IL_0008:  cgt.un
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_000d
  IL_000d:  ldloc.0
  IL_000e:  ret
}");
        }

        [Fact, WorkItem(26387, "https://github.com/dotnet/roslyn/issues/26387")]
        public void ValueTypeArgument02()
        {
            var source =
@"namespace ConsoleApp1
{
    public class TestHelper
    {
        public static void Main()
        {
            IsValueTypeT(new Result<(Cls, IInt)>((new Cls(), new Int())));
            System.Console.WriteLine(""done"");
        }

        public static void IsValueTypeT<T>(Result<T> result)
        {
            if (!(result.Value is T v))
                throw new NotPossibleException();
        }
    }

    public class Result<T>
    {
        public T Value { get; }

        public Result(T value)
        {
            Value = value;
        }
    }

    public class Cls { }
    public interface IInt { }

    public class Int : IInt { }

    public class NotPossibleException : System.Exception { }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"done";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("ConsoleApp1.TestHelper.IsValueTypeT<T>(ConsoleApp1.Result<T>)",
@"{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (T V_0, //v
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""T ConsoleApp1.Result<T>.Value.get""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  box        ""T""
  IL_000e:  ldnull
  IL_000f:  cgt.un
  IL_0011:  ldc.i4.0
  IL_0012:  ceq
  IL_0014:  stloc.1
  IL_0015:  ldloc.1
  IL_0016:  brfalse.s  IL_001e
  IL_0018:  newobj     ""ConsoleApp1.NotPossibleException..ctor()""
  IL_001d:  throw
  IL_001e:  ret
}");
        }

        [Fact]
        public void DeconstructNullableTuple_01()
        {
            var source =
@"
class C {
    static int i = 3;
    static (int,int)? GetNullableTuple() => (i++, i++);

    static void Main() {
        if (GetNullableTuple() is (int x1, int y1) tupl1)
        {
            System.Console.WriteLine($""x = {x1}, y = {y1}"");
        }
        if (GetNullableTuple() is (int x2, int y2) _)
        {
            System.Console.WriteLine($""x = {x2}, y = {y2}"");
        }
        switch (GetNullableTuple())
        {
            case (int x3, int y3) s:
                System.Console.WriteLine($""x = {x3}, y = {y3}"");
                break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"x = 3, y = 4
x = 5, y = 6
x = 7, y = 8";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DeconstructNullable_01()
        {
            var source =
@"
class C {
    static int i = 3;
    static S? GetNullableTuple() => new S(i++, i++);

    static void Main() {
        if (GetNullableTuple() is (int x1, int y1) tupl1)
        {
            System.Console.WriteLine($""x = {x1}, y = {y1}"");
        }
        if (GetNullableTuple() is (int x2, int y2) _)
        {
            System.Console.WriteLine($""x = {x2}, y = {y2}"");
        }
        switch (GetNullableTuple())
        {
            case (int x3, int y3) s:
                System.Console.WriteLine($""x = {x3}, y = {y3}"");
                break;
        }
    }
}
struct S
{
    int x, y;
    public S(int X, int Y) => (this.x, this.y) = (X, Y);
    public void Deconstruct(out int X, out int Y) => (X, Y) = (x, y);
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"x = 3, y = 4
x = 5, y = 6
x = 7, y = 8";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(28632, "https://github.com/dotnet/roslyn/issues/28632")]
        public void UnboxNullableInRecursivePattern01()
        {
            var source =
@"
static class Program
{
    public static int M1(int? x)
    {
        return x is int y ? y : 1;
    }
    public static int M2(int? x)
    {
        return x is { } y ? y : 2;
    }
    public static void Main()
    {
        System.Console.WriteLine(M1(null));
        System.Console.WriteLine(M2(null));
        System.Console.WriteLine(M1(3));
        System.Console.WriteLine(M2(4));
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"1
2
3
4";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Program.M1",
@"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (int V_0) //y
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brfalse.s  IL_0013
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""int int?.GetValueOrDefault()""
  IL_0010:  stloc.0
  IL_0011:  br.s       IL_0015
  IL_0013:  ldc.i4.1
  IL_0014:  ret
  IL_0015:  ldloc.0
  IL_0016:  ret
}");
            compVerifier.VerifyIL("Program.M2",
@"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (int V_0) //y
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brfalse.s  IL_0013
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""int int?.GetValueOrDefault()""
  IL_0010:  stloc.0
  IL_0011:  br.s       IL_0015
  IL_0013:  ldc.i4.2
  IL_0014:  ret
  IL_0015:  ldloc.0
  IL_0016:  ret
}");
        }

        [Fact]
        public void DoNotShareInputForMutatingWhenClause()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.Write(M1(1));
        Console.Write(M2(1));
        Console.Write(M3(1));
        Console.Write(M4(1));
        Console.Write(M5(1));
        Console.Write(M6(1));
        Console.Write(M7(1));
        Console.Write(M8(1));
        Console.Write(M9(1));
    }
    public static int M1(int x)
    {
        return x switch { _ when (++x) == 5 => 1, 1 => 2, _ => 3 };
    }
    public static int M2(int x)
    {
        return x switch { _ when (x+=1) == 5 => 1, 1 => 2, _ => 3 };
    }
    public static int M3(int x)
    {
        return x switch { _ when ((x, _) = (5, 6)) == (0, 0) => 1, 1 => 2, _ => 3 };
    }
    public static int M4(int x)
    {
        dynamic d = new Program();
        return x switch { _ when d.M(ref x) => 1, 1 => 2, _ => 3 };
    }
    bool M(ref int x) { x = 100; return false; }
    public static int M5(int x)
    {
        return x switch { _ when new Program(ref x).P => 1, 1 => 2, _ => 3 };
    }
    public static int M6(int x)
    {
        dynamic d = x;
        return x switch { _ when new Program(d, ref x).P => 1, 1 => 2, _ => 3 };
    }
    public static int M7(int x)
    {
        return x switch { _ when new Program(ref x).P && new Program().P => 1, 1 => 2, _ => 3 };
    }
    public static int M8(int x)
    {
        dynamic d = x;
        return x switch { _ when new Program(d, ref x).P && new Program().P => 1, 1 => 2, _ => 3 };
    }
    public static int M9(int x)
    {
        return x switch { _ when (x=100) == 1 => 1, 1 => 2, _ => 3 };
    }
    Program() { }
    Program(ref int x) { x = 100; }
    Program(int a, ref int x) { x = 100; }
    bool P => false;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, references: new[] { CSharpRef });
            compilation.VerifyDiagnostics();
            var expectedOutput = @"222222222";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void GenerateStringHashOnlyOnce()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.Write(M1(string.Empty));
        Console.Write(M2(string.Empty));
    }
    public static int M1(string s)
    {
        return s switch { ""a""=>1, ""b""=>2, ""c""=>3, ""d""=>4, ""e""=>5, ""f""=>6, ""g""=>7, ""h""=>8, _ => 9 };
    }
    public static int M2(string s)
    {
        return s switch { ""a""=>1, ""b""=>2, ""c""=>3, ""d""=>4, ""e""=>5, ""f""=>6, ""g""=>7, ""h""=>8, _ => 9 };
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"99";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void BindVariablesInWhenClause()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var t = (1, 2);
        switch (t)
        {
            case var (x, y) when x+1 == y:
                Console.Write(1);
                break;
        }
        Console.Write(t switch
        {
            var (x, y) when x+1 == y => 1,
            _ => 2
        });
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"11";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void MissingExceptions_01()
        {
            var source = @"namespace System {
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
static class C {
    public static bool M(int i) => i switch { 1 => true };
}
";
            var compilation = CreateEmptyCompilation(source, options: TestOptions.ReleaseDll);
            compilation.GetDiagnostics().Verify(
                // (9,38): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '0' is not covered.
                //     public static bool M(int i) => i switch { 1 => true };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("0").WithLocation(9, 38)
                );
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (9,36): error CS0656: Missing compiler required member 'System.InvalidOperationException..ctor'
                //     public static bool M(int i) => i switch { 1 => true };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "i switch { 1 => true }").WithArguments("System.InvalidOperationException", ".ctor").WithLocation(9, 36),
                // (9,38): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '0' is not covered.
                //     public static bool M(int i) => i switch { 1 => true };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("0").WithLocation(9, 38)
                );
        }

        [Fact, WorkItem(32774, "https://github.com/dotnet/roslyn/issues/32774")]
        public void BadCode_32774()
        {
            var source = @"
public class Class1
{
    static void Main()
    {
        System.Console.WriteLine(SwitchCaseThatFails(12.123));
    }

    public static bool SwitchCaseThatFails(object someObject)
    {
        switch (someObject)
        {
            case IObject x when x.SubObject != null:
                return false;
            case IOtherObject x:
                return false;
            case double x:
                return true;
            default:
                return false;
        }
    }

    public interface IObject
    {
        IObject SubObject { get; }
    }

    public interface IOtherObject { }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Class1.SwitchCaseThatFails",
@"{
  // Code size       97 (0x61)
  .maxstack  1
  .locals init (Class1.IObject V_0, //x
                Class1.IOtherObject V_1, //x
                double V_2, //x
                object V_3,
                object V_4,
                bool V_5)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_4
  IL_0004:  ldloc.s    V_4
  IL_0006:  stloc.3
  IL_0007:  ldloc.3
  IL_0008:  isinst     ""Class1.IObject""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  brtrue.s   IL_003c
  IL_0011:  br.s       IL_001f
  IL_0013:  ldloc.3
  IL_0014:  isinst     ""Class1.IOtherObject""
  IL_0019:  stloc.1
  IL_001a:  ldloc.1
  IL_001b:  brtrue.s   IL_004b
  IL_001d:  br.s       IL_0059
  IL_001f:  ldloc.3
  IL_0020:  isinst     ""Class1.IOtherObject""
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  brtrue.s   IL_004b
  IL_0029:  br.s       IL_002b
  IL_002b:  ldloc.3
  IL_002c:  isinst     ""double""
  IL_0031:  brfalse.s  IL_0059
  IL_0033:  ldloc.3
  IL_0034:  unbox.any  ""double""
  IL_0039:  stloc.2
  IL_003a:  br.s       IL_0052
  IL_003c:  ldloc.0
  IL_003d:  callvirt   ""Class1.IObject Class1.IObject.SubObject.get""
  IL_0042:  brtrue.s   IL_0046
  IL_0044:  br.s       IL_0013
  IL_0046:  ldc.i4.0
  IL_0047:  stloc.s    V_5
  IL_0049:  br.s       IL_005e
  IL_004b:  br.s       IL_004d
  IL_004d:  ldc.i4.0
  IL_004e:  stloc.s    V_5
  IL_0050:  br.s       IL_005e
  IL_0052:  br.s       IL_0054
  IL_0054:  ldc.i4.1
  IL_0055:  stloc.s    V_5
  IL_0057:  br.s       IL_005e
  IL_0059:  ldc.i4.0
  IL_005a:  stloc.s    V_5
  IL_005c:  br.s       IL_005e
  IL_005e:  ldloc.s    V_5
  IL_0060:  ret
}");
        }

        // Possible test helper bug on Linux; see https://github.com/dotnet/roslyn/issues/33356
        [ConditionalFact(typeof(WindowsOnly))]
        public void SwitchExpressionSequencePoints()
        {
            string source = @"
public class Program
{
    public static void Main()
    {
        int i = 0;
        var y = (i switch
        {
            0 => new Program(),
            1 => new Program(),
            _ => new Program(),
        }).Chain();
        y.Chain2();
    }
    public Program Chain() => this;
    public Program Chain2() => this;
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugExe);
            v.VerifyIL(qualifiedMethodName: "Program.Main", @"
    {
      // Code size       61 (0x3d)
      .maxstack  2
      .locals init (int V_0, //i
                    Program V_1, //y
                    Program V_2)
      // sequence point: {
      IL_0000:  nop
      // sequence point: int i = 0;
      IL_0001:  ldc.i4.0
      IL_0002:  stloc.0
      // sequence point: var y = (i s ...   }).Chain()
      IL_0003:  ldc.i4.1
      IL_0004:  brtrue.s   IL_0007
      // sequence point: switch ...         }
      IL_0006:  nop
      // sequence point: <hidden>
      IL_0007:  ldloc.0
      IL_0008:  brfalse.s  IL_0012
      IL_000a:  br.s       IL_000c
      IL_000c:  ldloc.0
      IL_000d:  ldc.i4.1
      IL_000e:  beq.s      IL_001a
      IL_0010:  br.s       IL_0022
      // sequence point: new Program()
      IL_0012:  newobj     ""Program..ctor()""
      IL_0017:  stloc.2
      IL_0018:  br.s       IL_002a
      // sequence point: new Program()
      IL_001a:  newobj     ""Program..ctor()""
      IL_001f:  stloc.2
      IL_0020:  br.s       IL_002a
      // sequence point: new Program()
      IL_0022:  newobj     ""Program..ctor()""
      IL_0027:  stloc.2
      IL_0028:  br.s       IL_002a
      // sequence point: <hidden>
      IL_002a:  ldc.i4.1
      IL_002b:  brtrue.s   IL_002e
      // sequence point: var y = (i s ...   }).Chain()
      IL_002d:  nop
      // sequence point: <hidden>
      IL_002e:  ldloc.2
      IL_002f:  callvirt   ""Program Program.Chain()""
      IL_0034:  stloc.1
      // sequence point: y.Chain2();
      IL_0035:  ldloc.1
      IL_0036:  callvirt   ""Program Program.Chain2()""
      IL_003b:  pop
      // sequence point: }
      IL_003c:  ret
    }
", sequencePoints: "Program.Main", source: source);
        }

        [Fact, WorkItem(33675, "https://github.com/dotnet/roslyn/issues/33675")]
        public void ParsingParenthesizedExpressionAsPatternOfExpressionSwitch()
        {
            var source = @"
public class Class1
{
    static void Main()
    {
        System.Console.Write(M(42));
        System.Console.Write(M(41));
    }

    static bool M(object o)
    {
        const int X = 42;
        return o switch { (X) => true, _ => false };
    }
}";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var compilation = CreateCompilation(source, options: options);
                compilation.VerifyDiagnostics();
                var expectedOutput = @"TrueFalse";
                var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
                if (options.OptimizationLevel == OptimizationLevel.Debug)
                {
                    compVerifier.VerifyIL("Class1.M", @"
    {
      // Code size       45 (0x2d)
      .maxstack  2
      .locals init (bool V_0,
                    int V_1,
                    bool V_2)
      IL_0000:  nop
      IL_0001:  ldc.i4.1
      IL_0002:  brtrue.s   IL_0005
      IL_0004:  nop
      IL_0005:  ldarg.0
      IL_0006:  isinst     ""int""
      IL_000b:  brfalse.s  IL_001f
      IL_000d:  ldarg.0
      IL_000e:  unbox.any  ""int""
      IL_0013:  stloc.1
      IL_0014:  ldloc.1
      IL_0015:  ldc.i4.s   42
      IL_0017:  beq.s      IL_001b
      IL_0019:  br.s       IL_001f
      IL_001b:  ldc.i4.1
      IL_001c:  stloc.0
      IL_001d:  br.s       IL_0023
      IL_001f:  ldc.i4.0
      IL_0020:  stloc.0
      IL_0021:  br.s       IL_0023
      IL_0023:  ldc.i4.1
      IL_0024:  brtrue.s   IL_0027
      IL_0026:  nop
      IL_0027:  ldloc.0
      IL_0028:  stloc.2
      IL_0029:  br.s       IL_002b
      IL_002b:  ldloc.2
      IL_002c:  ret
    }
");
                }
                else
                {
                    compVerifier.VerifyIL("Class1.M",
@"{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""int""
  IL_0006:  brfalse.s  IL_0016
  IL_0008:  ldarg.0
  IL_0009:  unbox.any  ""int""
  IL_000e:  ldc.i4.s   42
  IL_0010:  bne.un.s   IL_0016
  IL_0012:  ldc.i4.1
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0018
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  ret
}");
                }
            }
        }

        [Fact, WorkItem(35584, "https://github.com/dotnet/roslyn/issues/35584")]
        public void MatchToTypeParameterUnbox_01()
        {
            var source = @"
class Program
{
    public static void Main() => System.Console.WriteLine(P<int>(0));
    public static string P<T>(T t) => (t is object o) ? o.ToString() : string.Empty;
}
";
            var expectedOutput = @"0";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var compilation = CreateCompilation(source, options: options);
                compilation.VerifyDiagnostics();
                var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
                if (options.OptimizationLevel == OptimizationLevel.Debug)
                {
                    compVerifier.VerifyIL("Program.P<T>",
@"{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_0011
  IL_000a:  ldsfld     ""string string.Empty""
  IL_000f:  br.s       IL_0017
  IL_0011:  ldloc.0
  IL_0012:  callvirt   ""string object.ToString()""
  IL_0017:  ret
}
");
                }
                else
                {
                    compVerifier.VerifyIL("Program.P<T>",
@"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_0010
  IL_000a:  ldsfld     ""string string.Empty""
  IL_000f:  ret
  IL_0010:  ldloc.0
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  ret
}
");
                }
            }
        }

        [Fact, WorkItem(35584, "https://github.com/dotnet/roslyn/issues/35584")]
        public void MatchToTypeParameterUnbox_02()
        {
            var source =
@"using System;
 class Program
{
    public static void Main()
    {
        var generic = new Generic<int>(0);
    }
}
public class Generic<T>
{
    public Generic(T value)
    {
        if (value is object obj && obj == null)
        {
            throw new Exception(""Kaboom!"");
        }
    }
}
";
            var expectedOutput = @"";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var compilation = CreateCompilation(source, options: options);
                compilation.VerifyDiagnostics();
                var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
                if (options.OptimizationLevel == OptimizationLevel.Debug)
                {
                    compVerifier.VerifyIL("Generic<T>..ctor(T)",
@"{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (object V_0, //obj
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldarg.1
  IL_0009:  box        ""T""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  brfalse.s  IL_0018
  IL_0012:  ldloc.0
  IL_0013:  ldnull
  IL_0014:  ceq
  IL_0016:  br.s       IL_0019
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.1
  IL_001a:  ldloc.1
  IL_001b:  brfalse.s  IL_0029
  IL_001d:  nop
  IL_001e:  ldstr      ""Kaboom!""
  IL_0023:  newobj     ""System.Exception..ctor(string)""
  IL_0028:  throw
  IL_0029:  ret
}
");
                }
                else
                {
                    compVerifier.VerifyIL("Generic<T>..ctor(T)",
@"{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (object V_0) //obj
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.1
  IL_0007:  box        ""T""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  brfalse.s  IL_001e
  IL_0010:  ldloc.0
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldstr      ""Kaboom!""
  IL_0018:  newobj     ""System.Exception..ctor(string)""
  IL_001d:  throw
  IL_001e:  ret
}
");
                }
            }
        }

        [Fact, WorkItem(35584, "https://github.com/dotnet/roslyn/issues/35584")]
        public void MatchToTypeParameterUnbox_03()
        {
            var source =
@"using System;
class Program
{
    public static void Main() => System.Console.WriteLine(P<Enum>(null));
    public static string P<T>(T t) where T: Enum => (t is ValueType o) ? o.ToString() : ""1"";
}
";
            var expectedOutput = @"1";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var compilation = CreateCompilation(source, options: options);
                compilation.VerifyDiagnostics();
                var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
                if (options.OptimizationLevel == OptimizationLevel.Debug)
                {
                    compVerifier.VerifyIL("Program.P<T>",
@"{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (System.ValueType V_0) //o
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_0011
  IL_000a:  ldstr      ""1""
  IL_000f:  br.s       IL_0017
  IL_0011:  ldloc.0
  IL_0012:  callvirt   ""string object.ToString()""
  IL_0017:  ret
}
");
                }
                else
                {
                    compVerifier.VerifyIL("Program.P<T>",
@"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (System.ValueType V_0) //o
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_0010
  IL_000a:  ldstr      ""1""
  IL_000f:  ret
  IL_0010:  ldloc.0
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  ret
}
");
                }
            }
        }

        [Fact]
        public void CompileTimeRuntimeInstanceofMismatch_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        M(new byte[1]);
        M(new sbyte[1]);
        M(new Ebyte[1]);
        M(new Esbyte[1]);

        M(new short[1]);
        M(new ushort[1]);
        M(new Eshort[1]);
        M(new Eushort[1]);

        M(new int[1]);
        M(new uint[1]);
        M(new Eint[1]);
        M(new Euint[1]);

        M(new int[1][]);
        M(new uint[1][]);
        M(new Eint[1][]);
        M(new Euint[1][]);

        M(new long[1]);
        M(new ulong[1]);
        M(new Elong[1]);
        M(new Eulong[1]);

        M(new IntPtr[1]);
        M(new UIntPtr[1]);

        M(new IntPtr[1][]);
        M(new UIntPtr[1][]);
    }

    static void M(object o)
    {
        switch (o)
        {
            case byte[] _ when o.GetType() == typeof(byte[]):
                Console.WriteLine(""byte[]"");
                break;
            case sbyte[] _ when o.GetType() == typeof(sbyte[]):
                Console.WriteLine(""sbyte[]"");
                break;
            case Ebyte[] _ when o.GetType() == typeof(Ebyte[]):
                Console.WriteLine(""Ebyte[]"");
                break;
            case Esbyte[] _ when o.GetType() == typeof(Esbyte[]):
                Console.WriteLine(""Esbyte[]"");
                break;

            case short[] _ when o.GetType() == typeof(short[]):
                Console.WriteLine(""short[]"");
                break;
            case ushort[] _ when o.GetType() == typeof(ushort[]):
                Console.WriteLine(""ushort[]"");
                break;
            case Eshort[] _ when o.GetType() == typeof(Eshort[]):
                Console.WriteLine(""Eshort[]"");
                break;
            case Eushort[] _ when o.GetType() == typeof(Eushort[]):
                Console.WriteLine(""Eushort[]"");
                break;

            case int[] _ when o.GetType() == typeof(int[]):
                Console.WriteLine(""int[]"");
                break;
            case uint[] _ when o.GetType() == typeof(uint[]):
                Console.WriteLine(""uint[]"");
                break;
            case Eint[] _ when o.GetType() == typeof(Eint[]):
                Console.WriteLine(""Eint[]"");
                break;
            case Euint[] _ when o.GetType() == typeof(Euint[]):
                Console.WriteLine(""Euint[]"");
                break;

            case int[][] _ when o.GetType() == typeof(int[][]):
                Console.WriteLine(""int[][]"");
                break;
            case uint[][] _ when o.GetType() == typeof(uint[][]):
                Console.WriteLine(""uint[][]"");
                break;
            case Eint[][] _ when o.GetType() == typeof(Eint[][]):
                Console.WriteLine(""Eint[][]"");
                break;
            case Euint[][] _ when o.GetType() == typeof(Euint[][]):
                Console.WriteLine(""Euint[][]"");
                break;

            case long[] _ when o.GetType() == typeof(long[]):
                Console.WriteLine(""long[]"");
                break;
            case ulong[] _ when o.GetType() == typeof(ulong[]):
                Console.WriteLine(""ulong[]"");
                break;
            case Elong[] _ when o.GetType() == typeof(Elong[]):
                Console.WriteLine(""Elong[]"");
                break;
            case Eulong[] _ when o.GetType() == typeof(Eulong[]):
                Console.WriteLine(""Eulong[]"");
                break;

            case IntPtr[] _ when o.GetType() == typeof(IntPtr[]):
                Console.WriteLine(""IntPtr[]"");
                break;
            case UIntPtr[] _ when o.GetType() == typeof(UIntPtr[]):
                Console.WriteLine(""UIntPtr[]"");
                break;

            case IntPtr[][] _ when o.GetType() == typeof(IntPtr[][]):
                Console.WriteLine(""IntPtr[][]"");
                break;
            case UIntPtr[][] _ when o.GetType() == typeof(UIntPtr[][]):
                Console.WriteLine(""UIntPtr[][]"");
                break;

            default:
                Console.WriteLine(""oops: "" + o.GetType());
                break;
        }
    }
}
enum Ebyte : byte {}
enum Esbyte : sbyte {}
enum Eshort : short {}
enum Eushort : ushort {}
enum Eint : int {}
enum Euint : uint {}
enum Elong : long {}
enum Eulong : ulong {}
";
            var expectedOutput =
@"byte[]
sbyte[]
Ebyte[]
Esbyte[]
short[]
ushort[]
Eshort[]
Eushort[]
int[]
uint[]
Eint[]
Euint[]
int[][]
uint[][]
Eint[][]
Euint[][]
long[]
ulong[]
Elong[]
Eulong[]
IntPtr[]
UIntPtr[]
IntPtr[][]
UIntPtr[][]
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void CompileTimeRuntimeInstanceofMismatch_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        M(new byte[1]);
        M(new sbyte[1]);
    }
    static void M(object o)
    {
        switch (o)
        {
            case byte[] _:
                Console.WriteLine(""byte[]"");
                break;
            case sbyte[] _: // not subsumed, even though it will never occur due to CLR behavior
                Console.WriteLine(""sbyte[]"");
                break;
        }
    }
}
";
            var expectedOutput =
@"byte[]
byte[]
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(36496, "https://github.com/dotnet/roslyn/issues/36496")]
        public void EmptyVarPatternVsDeconstruct()
        {
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        Console.Write(M(new C()));
        Console.Write(M(null));
    }
    public static bool M(C c)
    {
        return c is var ();
    }
    public void Deconstruct() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "TrueFalse");
            compVerifier.VerifyIL("C.M(C)",
@"
{
    // Code size        5 (0x5)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldnull
    IL_0002:  cgt.un
    IL_0004:  ret
}
");
        }

        [Fact]
        [WorkItem(36496, "https://github.com/dotnet/roslyn/issues/36496")]
        public void EmptyPositionalPatternVsDeconstruct()
        {
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        Console.Write(M(new C()));
        Console.Write(M(null));
    }
    public static bool M(C c)
    {
        return c is ();
    }
    public void Deconstruct() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "TrueFalse");
            compVerifier.VerifyIL("C.M(C)",
@"
{
    // Code size        5 (0x5)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldnull
    IL_0002:  cgt.un
    IL_0004:  ret
}
");
        }

        [Fact, WorkItem(31494, "https://github.com/dotnet/roslyn/issues/31494")]
        public void NoRedundantNullCheckForStringConstantPattern_01()
        {
            var source =
@"public class C
{
    public static bool M1(string s) => s is ""Frog"";
    public static bool M2(string s) => s == ""Frog"";
    public static bool M3(string s) => s switch { ""Frog"" => true, _ => false };
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1(string)", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""Frog""
  IL_0006:  call       ""bool string.op_Equality(string, string)""
  IL_000b:  ret
}
");
            compVerifier.VerifyIL("C.M2(string)", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""Frog""
  IL_0006:  call       ""bool string.op_Equality(string, string)""
  IL_000b:  ret
}
");
            compVerifier.VerifyIL("C.M3(string)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""Frog""
  IL_0006:  call       ""bool string.op_Equality(string, string)""
  IL_000b:  brfalse.s  IL_0011
  IL_000d:  ldc.i4.1
  IL_000e:  stloc.0
  IL_000f:  br.s       IL_0013
  IL_0011:  ldc.i4.0
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ret
}
");
        }

        [Fact, WorkItem(31494, "https://github.com/dotnet/roslyn/issues/31494")]
        public void NoRedundantNullCheckForStringConstantPattern_02()
        {
            var source =
@"public class C
{
    public static bool M1(string s) => s switch { ""Frog"" => true, ""Newt"" => true, _ => false };
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1(string)", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""Frog""
  IL_0006:  call       ""bool string.op_Equality(string, string)""
  IL_000b:  brtrue.s   IL_001c
  IL_000d:  ldarg.0
  IL_000e:  ldstr      ""Newt""
  IL_0013:  call       ""bool string.op_Equality(string, string)""
  IL_0018:  brtrue.s   IL_0020
  IL_001a:  br.s       IL_0024
  IL_001c:  ldc.i4.1
  IL_001d:  stloc.0
  IL_001e:  br.s       IL_0026
  IL_0020:  ldc.i4.1
  IL_0021:  stloc.0
  IL_0022:  br.s       IL_0026
  IL_0024:  ldc.i4.0
  IL_0025:  stloc.0
  IL_0026:  ldloc.0
  IL_0027:  ret
}
");
        }

        [Fact, WorkItem(31494, "https://github.com/dotnet/roslyn/issues/31494")]
        public void NoRedundantNullCheckForStringConstantPattern_03()
        {
            var source =
@"public class C
{
    public static bool M1(System.Type x) => x is { Name: ""Program"" };
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1(System.Type)", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0014
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_0009:  ldstr      ""Program""
  IL_000e:  call       ""bool string.op_Equality(string, string)""
  IL_0013:  ret
  IL_0014:  ldc.i4.0
  IL_0015:  ret
}
");
        }

        [Fact]
        [WorkItem(42912, "https://github.com/dotnet/roslyn/issues/42912")]
        [WorkItem(31494, "https://github.com/dotnet/roslyn/issues/31494")]
        public void NoRedundantNullCheckForNullableConstantPattern_04()
        {
            // Note that we do not produce the same code for `x is 1` and `x == 1`.
            // The latter has been optimized to avoid branches.

            var source =
@"public class C
{
    public static bool M1(int? x) => x is 1;
    public static bool M2(int? x) => x == 1;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("C.M1(int?)", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brfalse.s  IL_0014
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""int int?.GetValueOrDefault()""
  IL_0010:  ldc.i4.1
  IL_0011:  ceq
  IL_0013:  ret
  IL_0014:  ldc.i4.0
  IL_0015:  ret
}
");
            compVerifier.VerifyIL("C.M2(int?)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (int? V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""int int?.GetValueOrDefault()""
  IL_000b:  ldloc.1
  IL_000c:  ceq
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""bool int?.HasValue.get""
  IL_0015:  and
  IL_0016:  ret
}
");
        }

        [Fact]
        public void SwitchExpressionAsExceptionFilter_01()
        {
            var source = @"
using System;
class C
{
    const string K1 = ""frog"";
    const string K2 = ""toad"";
    public static void M(string msg)
    {
        try
        {
            T(msg);
        }
        catch (Exception e) when (e.Message switch
            {
                K1 => true,
                K2 => true,
                _ => false,
            })
        {
            throw new Exception(e.Message);
        }
        catch
        {
        }
    }
    static void T(string msg)
    {
        throw new Exception(msg);
    }
    static void Main()
    {
        Try(K1);
        Try(K2);
        Try(""fox"");
    }
    static void Try(string s)
    {
        try { M(s); } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
}
";
            var expectedOutput =
@"frog
toad";
            foreach (var compilationOptions in new[] { TestOptions.ReleaseExe, TestOptions.DebugExe })
            {
                var compilation = CreateCompilation(source, options: compilationOptions);
                compilation.VerifyDiagnostics();
                var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
                if (compilationOptions.OptimizationLevel == OptimizationLevel.Debug)
                {
                    compVerifier.VerifyIL("C.M(string)", @"
{
  // Code size      108 (0x6c)
  .maxstack  2
  .locals init (System.Exception V_0, //e
                bool V_1,
                string V_2,
                bool V_3)
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  ldarg.0
    IL_0003:  call       ""void C.T(string)""
    IL_0008:  nop
    IL_0009:  nop
    IL_000a:  leave.s    IL_006b
  }
  filter
  {
    IL_000c:  isinst     ""System.Exception""
    IL_0011:  dup
    IL_0012:  brtrue.s   IL_0018
    IL_0014:  pop
    IL_0015:  ldc.i4.0
    IL_0016:  br.s       IL_0056
    IL_0018:  stloc.0
    IL_0019:  ldloc.0
    IL_001a:  callvirt   ""string System.Exception.Message.get""
    IL_001f:  stloc.2
    IL_0020:  ldc.i4.1
    IL_0021:  brtrue.s   IL_0024
    IL_0023:  nop
    IL_0024:  ldloc.2
    IL_0025:  ldstr      ""frog""
    IL_002a:  call       ""bool string.op_Equality(string, string)""
    IL_002f:  brtrue.s   IL_0040
    IL_0031:  ldloc.2
    IL_0032:  ldstr      ""toad""
    IL_0037:  call       ""bool string.op_Equality(string, string)""
    IL_003c:  brtrue.s   IL_0044
    IL_003e:  br.s       IL_0048
    IL_0040:  ldc.i4.1
    IL_0041:  stloc.1
    IL_0042:  br.s       IL_004c
    IL_0044:  ldc.i4.1
    IL_0045:  stloc.1
    IL_0046:  br.s       IL_004c
    IL_0048:  ldc.i4.0
    IL_0049:  stloc.1
    IL_004a:  br.s       IL_004c
    IL_004c:  ldc.i4.1
    IL_004d:  brtrue.s   IL_0050
    IL_004f:  nop
    IL_0050:  ldloc.1
    IL_0051:  stloc.3
    IL_0052:  ldloc.3
    IL_0053:  ldc.i4.0
    IL_0054:  cgt.un
    IL_0056:  endfilter
  }  // end filter
  {  // handler
    IL_0058:  pop
    IL_0059:  nop
    IL_005a:  ldloc.0
    IL_005b:  callvirt   ""string System.Exception.Message.get""
    IL_0060:  newobj     ""System.Exception..ctor(string)""
    IL_0065:  throw
  }
  catch object
  {
    IL_0066:  pop
    IL_0067:  nop
    IL_0068:  nop
    IL_0069:  leave.s    IL_006b
  }
  IL_006b:  ret
}
");
                }
                else
                {
                    compVerifier.VerifyIL("C.M(string)", @"
{
  // Code size       89 (0x59)
  .maxstack  2
  .locals init (System.Exception V_0, //e
                bool V_1,
                string V_2)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  call       ""void C.T(string)""
    IL_0006:  leave.s    IL_0058
  }
  filter
  {
    IL_0008:  isinst     ""System.Exception""
    IL_000d:  dup
    IL_000e:  brtrue.s   IL_0014
    IL_0010:  pop
    IL_0011:  ldc.i4.0
    IL_0012:  br.s       IL_0046
    IL_0014:  stloc.0
    IL_0015:  ldloc.0
    IL_0016:  callvirt   ""string System.Exception.Message.get""
    IL_001b:  stloc.2
    IL_001c:  ldloc.2
    IL_001d:  ldstr      ""frog""
    IL_0022:  call       ""bool string.op_Equality(string, string)""
    IL_0027:  brtrue.s   IL_0038
    IL_0029:  ldloc.2
    IL_002a:  ldstr      ""toad""
    IL_002f:  call       ""bool string.op_Equality(string, string)""
    IL_0034:  brtrue.s   IL_003c
    IL_0036:  br.s       IL_0040
    IL_0038:  ldc.i4.1
    IL_0039:  stloc.1
    IL_003a:  br.s       IL_0042
    IL_003c:  ldc.i4.1
    IL_003d:  stloc.1
    IL_003e:  br.s       IL_0042
    IL_0040:  ldc.i4.0
    IL_0041:  stloc.1
    IL_0042:  ldloc.1
    IL_0043:  ldc.i4.0
    IL_0044:  cgt.un
    IL_0046:  endfilter
  }  // end filter
  {  // handler
    IL_0048:  pop
    IL_0049:  ldloc.0
    IL_004a:  callvirt   ""string System.Exception.Message.get""
    IL_004f:  newobj     ""System.Exception..ctor(string)""
    IL_0054:  throw
  }
  catch object
  {
    IL_0055:  pop
    IL_0056:  leave.s    IL_0058
  }
  IL_0058:  ret
}
");
                }
            }
        }

        [Fact]
        public void SwitchExpressionAsExceptionFilter_02()
        {
            var source = @"
using System;
class C
{
    public static void Main()
    {
        try
        {
            throw new Exception();
        }
        catch when ((3 is int i) switch { true when M(() => i) => true, _ => false })
        {
            Console.WriteLine(""correct"");
        }
    }
    static bool M(Func<int> func)
    {
        func();
        return true;
    }
}
";
            var expectedOutput = @"correct";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        #endregion Miscellaneous

        #region Target Typed Switch

        [Fact]
        public void TargetTypedSwitch_Assignment()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        Console.Write(M(false));
        Console.Write(M(true));
    }
    static object M(bool b)
    {
        int result = b switch { false => new A(), true => new B() };
        return result;
    }
}
class A
{
    public static implicit operator int(A a) => throw null;
    public static implicit operator B(A a) => new B();
}
class B
{
    public static implicit operator int(B b) => (b == null) ? throw null : 2;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"22";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Return()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        Console.Write(M(false));
        Console.Write(M(true));
    }
    static long M(bool b)
    {
        return b switch { false => new A(), true => new B() };
    }
}
class A
{
    public static implicit operator int(A a) => throw null;
    public static implicit operator B(A a) => new B();
}
class B
{
    public static implicit operator int(B b) => (b == null) ? throw null : 2;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"22";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Argument_01()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        Console.Write(M1(false));
        Console.Write(M1(true));
    }
    static object M1(bool b)
    {
        return M2(b switch { false => new A(), true => new B() });
    }
    static Exception M2(Exception ex) => ex;
    static int M2(int i) => i;
    static int M2(string s) => s.Length;
}
class A : Exception
{
    public static implicit operator int(A a) => throw null;
    public static implicit operator B(A a) => throw null;
}
class B : Exception
{
    public static implicit operator int(B b) => throw null;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                    // (12,16): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M2(Exception)' and 'Program.M2(int)'
                    //         return M2(b switch { false => new A(), true => new B() });
                    Diagnostic(ErrorCode.ERR_AmbigCall, "M2").WithArguments("Program.M2(System.Exception)", "Program.M2(int)").WithLocation(12, 16)
                );
        }

        [Fact]
        public void TargetTypedSwitch_Argument_02()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        Console.Write(M1(false));
        Console.Write(M1(true));
    }
    static object M1(bool b)
    {
        return M2(b switch { false => new A(), true => new B() });
    }
    // static Exception M2(Exception ex) => ex;
    static int M2(int i) => i;
    static int M2(string s) => s.Length;
}
class A : Exception
{
    public static implicit operator int(A a) => throw null;
    public static implicit operator B(A a) => new B();
}
class B : Exception
{
    public static implicit operator int(B b) => (b == null) ? throw null : 2;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"22";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void TargetTypedSwitch_Arglist()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        Console.WriteLine(M1(false));
        Console.WriteLine(M1(true));
    }
    static object M1(bool b)
    {
        return M2(__arglist(b switch { false => new A(), true => new B() }));
    }
    static int M2(__arglist) => 1;
}
class A
{
    public A() { Console.Write(""new A; ""); }
    public static implicit operator B(A a) { Console.Write(""A->""); return new B(); }
}
class B
{
    public B() { Console.Write(""new B; ""); }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"new A; A->new B; 1
new B; 1";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_StackallocSize()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        M1(false);
        M1(true);
    }
    static void M1(bool b)
    {
        Span<int> s = stackalloc int[b switch { false => new A(), true => new B() }];
        Console.WriteLine(s.Length);
    }
}
class A
{
    public A() { Console.Write(""new A; ""); }
    public static implicit operator int(A a) { Console.Write(""A->int; ""); return 4; }
}
class B
{
    public B() { Console.Write(""new B; ""); }
    public static implicit operator int(B b) { Console.Write(""B->int; ""); return 2; }
}
";
            var compilation = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"new A; A->int; 4
new B; B->int; 2";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void TargetTypedSwitch_Attribute()
        {
            var source = @"
using System;
class Program
{
    [My(1 switch { 1 => 1, _ => 2 })]
    public static void M1() { }

    [My(1 switch { 1 => new A(), _ => new B() })]
    public static void M2() { }

    [My(1 switch { 1 => 1, _ => string.Empty })]
    public static void M3() { }
}
public class MyAttribute : Attribute
{
    public MyAttribute(int Value) { }
}
public class A
{
    public static implicit operator int(A a) => 4;
}
public class B
{
    public static implicit operator int(B b) => 2;
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (5,9): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                    //     [My(1 switch { 1 => 1, _ => 2 })]
                    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "1 switch { 1 => 1, _ => 2 }").WithLocation(5, 9),
                    // (8,9): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                    //     [My(1 switch { 1 => new A(), _ => new B() })]
                    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "1 switch { 1 => new A(), _ => new B() }").WithLocation(8, 9),
                    // (11,9): error CS1503: Argument 1: cannot convert from '<switch expression>' to 'int'
                    //     [My(1 switch { 1 => 1, _ => string.Empty })]
                    Diagnostic(ErrorCode.ERR_BadArgType, "1 switch { 1 => 1, _ => string.Empty }").WithArguments("1", "<switch expression>", "int").WithLocation(11, 9)
                );
        }

        [Fact]
        public void TargetTypedSwitch_As()
        {
            var source = @"
class Program
{
    public static void M(int i, string s)
    {
        // we do not target-type the left-hand-side of an as expression
        _ = i switch { 1 => i, _ => s } as object;
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,15): error CS8506: No best type was found for the switch expression.
                //         _ = i switch { 1 => i, _ => s } as object;
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(7, 15)
                );
        }

        [Fact]
        public void TargetTypedSwitch_DoubleConversion()
        {
            var source = @"using System;
class Program
{
    public static void Main(string[] args)
    {
        M(false);
        M(true);
    }
    public static void M(bool b)
    {
        C c = b switch { false => new A(), true => new B() };
        Console.WriteLine(""."");
    }
}
class A
{
    public A()
    {
        Console.Write(""new A; "");
    }
    public static implicit operator B(A a)
    {
        Console.Write(""A->"");
        return new B();
    }
}
class B
{
    public B()
    {
        Console.Write(""new B; "");
    }
    public static implicit operator C(B a)
    {
        Console.Write(""B->"");
        return new C();
    }
}
class C
{
    public C()
    {
        Console.Write(""new C; "");
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"new A; A->new B; B->new C; .
new B; B->new C; .";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_StringInsert()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        Console.Write($""{false switch { false => new A(), true => new B() }}"");
        Console.Write($""{true switch { false => new A(), true => new B() }}"");
    }
}
class A
{
}
class B
{
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "AB");
        }

        #endregion Target Typed Switch

        #region Pattern Combinators

        [Fact]
        public void IsPatternDisjunct_01()
        {
            var source = @"
using System;
class C
{
    static bool M1(object o) => o is int or long;
    static bool M2(object o) => o is int || o is long;
    public static void Main()
    {
        Console.Write(M1(1));
        Console.Write(M1(string.Empty));
        Console.Write(M1(1L));
        Console.Write(M1(1UL));
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"TrueFalseTrueFalse";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            var code = @"
    {
      // Code size       21 (0x15)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""int""
      IL_0006:  brtrue.s   IL_0013
      IL_0008:  ldarg.0
      IL_0009:  isinst     ""long""
      IL_000e:  ldnull
      IL_000f:  cgt.un
      IL_0011:  br.s       IL_0014
      IL_0013:  ldc.i4.1
      IL_0014:  ret
    }
";
            compVerifier.VerifyIL("C.M1", code);
            compVerifier.VerifyIL("C.M2", code);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            code = @"
    {
      // Code size       20 (0x14)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""int""
      IL_0006:  brtrue.s   IL_0012
      IL_0008:  ldarg.0
      IL_0009:  isinst     ""long""
      IL_000e:  ldnull
      IL_000f:  cgt.un
      IL_0011:  ret
      IL_0012:  ldc.i4.1
      IL_0013:  ret
    }
";
            compVerifier.VerifyIL("C.M1", code);
            compVerifier.VerifyIL("C.M2", code);
        }

        [Fact]
        public void IsPatternDisjunct_02()
        {
            var source = @"
using System;
class C
{
    static string M1(object o) => (o is int or long) ? ""True"" : ""False"";
    static string M2(object o) => (o is int || o is long) ? ""True"" : ""False"";
    public static void Main()
    {
        Console.Write(M1(1));
        Console.Write(M1(string.Empty));
        Console.Write(M1(1L));
        Console.Write(M1(1UL));
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"TrueFalseTrueFalse";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            var code = @"
    {
      // Code size       29 (0x1d)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""int""
      IL_0006:  brtrue.s   IL_0017
      IL_0008:  ldarg.0
      IL_0009:  isinst     ""long""
      IL_000e:  brtrue.s   IL_0017
      IL_0010:  ldstr      ""False""
      IL_0015:  br.s       IL_001c
      IL_0017:  ldstr      ""True""
      IL_001c:  ret
    }
";
            compVerifier.VerifyIL("C.M1", code);
            compVerifier.VerifyIL("C.M2", code);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            code = @"
    {
      // Code size       28 (0x1c)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""int""
      IL_0006:  brtrue.s   IL_0016
      IL_0008:  ldarg.0
      IL_0009:  isinst     ""long""
      IL_000e:  brtrue.s   IL_0016
      IL_0010:  ldstr      ""False""
      IL_0015:  ret
      IL_0016:  ldstr      ""True""
      IL_001b:  ret
    }
";
            compVerifier.VerifyIL("C.M1", code);
            compVerifier.VerifyIL("C.M2", code);
        }

        [Fact]
        public void IsPatternDisjunct_03()
        {
            var source = @"
using System;
class C
{
    static bool M1(object o) => o is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    static bool M2(object o) => o is char c && (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z');
    public static void Main()
    {
        Console.Write(M1('A'));
        Console.Write(M1('0'));
        Console.Write(M1('q'));
        Console.Write(M1(2));
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"TrueFalseTrueFalse";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1", @"
    {
      // Code size       47 (0x2f)
      .maxstack  2
      .locals init (char V_0,
                    bool V_1)
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""char""
      IL_0006:  brfalse.s  IL_002b
      IL_0008:  ldarg.0
      IL_0009:  unbox.any  ""char""
      IL_000e:  stloc.0
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.s   97
      IL_0012:  blt.s      IL_001b
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.s   122
      IL_0017:  ble.s      IL_0027
      IL_0019:  br.s       IL_002b
      IL_001b:  ldloc.0
      IL_001c:  ldc.i4.s   65
      IL_001e:  blt.s      IL_002b
      IL_0020:  ldloc.0
      IL_0021:  ldc.i4.s   90
      IL_0023:  ble.s      IL_0027
      IL_0025:  br.s       IL_002b
      IL_0027:  ldc.i4.1
      IL_0028:  stloc.1
      IL_0029:  br.s       IL_002d
      IL_002b:  ldc.i4.0
      IL_002c:  stloc.1
      IL_002d:  ldloc.1
      IL_002e:  ret
    }
");
            compVerifier.VerifyIL("C.M2", @"
    {
      // Code size       48 (0x30)
      .maxstack  2
      .locals init (char V_0) //c
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""char""
      IL_0006:  brfalse.s  IL_002e
      IL_0008:  ldarg.0
      IL_0009:  unbox.any  ""char""
      IL_000e:  stloc.0
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.s   65
      IL_0012:  blt.s      IL_0019
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.s   90
      IL_0017:  ble.s      IL_002b
      IL_0019:  ldloc.0
      IL_001a:  ldc.i4.s   97
      IL_001c:  blt.s      IL_0028
      IL_001e:  ldloc.0
      IL_001f:  ldc.i4.s   122
      IL_0021:  cgt
      IL_0023:  ldc.i4.0
      IL_0024:  ceq
      IL_0026:  br.s       IL_0029
      IL_0028:  ldc.i4.0
      IL_0029:  br.s       IL_002c
      IL_002b:  ldc.i4.1
      IL_002c:  br.s       IL_002f
      IL_002e:  ldc.i4.0
      IL_002f:  ret
    }
");

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1", @"
    {
      // Code size       45 (0x2d)
      .maxstack  2
      .locals init (char V_0,
                    bool V_1)
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""char""
      IL_0006:  brfalse.s  IL_0029
      IL_0008:  ldarg.0
      IL_0009:  unbox.any  ""char""
      IL_000e:  stloc.0
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.s   97
      IL_0012:  blt.s      IL_001b
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.s   122
      IL_0017:  ble.s      IL_0025
      IL_0019:  br.s       IL_0029
      IL_001b:  ldloc.0
      IL_001c:  ldc.i4.s   65
      IL_001e:  blt.s      IL_0029
      IL_0020:  ldloc.0
      IL_0021:  ldc.i4.s   90
      IL_0023:  bgt.s      IL_0029
      IL_0025:  ldc.i4.1
      IL_0026:  stloc.1
      IL_0027:  br.s       IL_002b
      IL_0029:  ldc.i4.0
      IL_002a:  stloc.1
      IL_002b:  ldloc.1
      IL_002c:  ret
    }
");
            compVerifier.VerifyIL("C.M2", @"
    {
      // Code size       45 (0x2d)
      .maxstack  2
      .locals init (char V_0) //c
      IL_0000:  ldarg.0
      IL_0001:  isinst     ""char""
      IL_0006:  brfalse.s  IL_002b
      IL_0008:  ldarg.0
      IL_0009:  unbox.any  ""char""
      IL_000e:  stloc.0
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.s   65
      IL_0012:  blt.s      IL_0019
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.s   90
      IL_0017:  ble.s      IL_0029
      IL_0019:  ldloc.0
      IL_001a:  ldc.i4.s   97
      IL_001c:  blt.s      IL_0027
      IL_001e:  ldloc.0
      IL_001f:  ldc.i4.s   122
      IL_0021:  cgt
      IL_0023:  ldc.i4.0
      IL_0024:  ceq
      IL_0026:  ret
      IL_0027:  ldc.i4.0
      IL_0028:  ret
      IL_0029:  ldc.i4.1
      IL_002a:  ret
      IL_002b:  ldc.i4.0
      IL_002c:  ret
    }
");
        }

        [Fact]
        public void IsPatternDisjunct_04()
        {
            var source = @"
using System;
class C
{
    static int M1(char c) => (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') ? 1 : 0;
    static int M2(char c) => (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z') ? 1 : 0;
    public static void Main()
    {
        Console.Write(M1('A'));
        Console.Write(M1('0'));
        Console.Write(M1('q'));
        Console.Write(M1(' '));
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1010";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1", @"
    {
      // Code size       38 (0x26)
      .maxstack  2
      .locals init (bool V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   97
      IL_0003:  blt.s      IL_000c
      IL_0005:  ldarg.0
      IL_0006:  ldc.i4.s   122
      IL_0008:  ble.s      IL_0018
      IL_000a:  br.s       IL_001c
      IL_000c:  ldarg.0
      IL_000d:  ldc.i4.s   65
      IL_000f:  blt.s      IL_001c
      IL_0011:  ldarg.0
      IL_0012:  ldc.i4.s   90
      IL_0014:  ble.s      IL_0018
      IL_0016:  br.s       IL_001c
      IL_0018:  ldc.i4.1
      IL_0019:  stloc.0
      IL_001a:  br.s       IL_001e
      IL_001c:  ldc.i4.0
      IL_001d:  stloc.0
      IL_001e:  ldloc.0
      IL_001f:  brtrue.s   IL_0024
      IL_0021:  ldc.i4.0
      IL_0022:  br.s       IL_0025
      IL_0024:  ldc.i4.1
      IL_0025:  ret
    }
");
            compVerifier.VerifyIL("C.M2", @"
    {
      // Code size       25 (0x19)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   65
      IL_0003:  blt.s      IL_000a
      IL_0005:  ldarg.0
      IL_0006:  ldc.i4.s   90
      IL_0008:  ble.s      IL_0017
      IL_000a:  ldarg.0
      IL_000b:  ldc.i4.s   97
      IL_000d:  blt.s      IL_0014
      IL_000f:  ldarg.0
      IL_0010:  ldc.i4.s   122
      IL_0012:  ble.s      IL_0017
      IL_0014:  ldc.i4.0
      IL_0015:  br.s       IL_0018
      IL_0017:  ldc.i4.1
      IL_0018:  ret
    }
");

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1", @"
    {
      // Code size       35 (0x23)
      .maxstack  2
      .locals init (bool V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   97
      IL_0003:  blt.s      IL_000c
      IL_0005:  ldarg.0
      IL_0006:  ldc.i4.s   122
      IL_0008:  ble.s      IL_0016
      IL_000a:  br.s       IL_001a
      IL_000c:  ldarg.0
      IL_000d:  ldc.i4.s   65
      IL_000f:  blt.s      IL_001a
      IL_0011:  ldarg.0
      IL_0012:  ldc.i4.s   90
      IL_0014:  bgt.s      IL_001a
      IL_0016:  ldc.i4.1
      IL_0017:  stloc.0
      IL_0018:  br.s       IL_001c
      IL_001a:  ldc.i4.0
      IL_001b:  stloc.0
      IL_001c:  ldloc.0
      IL_001d:  brtrue.s   IL_0021
      IL_001f:  ldc.i4.0
      IL_0020:  ret
      IL_0021:  ldc.i4.1
      IL_0022:  ret
    }
");
            compVerifier.VerifyIL("C.M2", @"
    {
      // Code size       24 (0x18)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   65
      IL_0003:  blt.s      IL_000a
      IL_0005:  ldarg.0
      IL_0006:  ldc.i4.s   90
      IL_0008:  ble.s      IL_0016
      IL_000a:  ldarg.0
      IL_000b:  ldc.i4.s   97
      IL_000d:  blt.s      IL_0014
      IL_000f:  ldarg.0
      IL_0010:  ldc.i4.s   122
      IL_0012:  ble.s      IL_0016
      IL_0014:  ldc.i4.0
      IL_0015:  ret
      IL_0016:  ldc.i4.1
      IL_0017:  ret
    }
");
        }

        [Fact]
        public void IsPatternDisjunct_05()
        {
            var source = @"
using System;
class C
{
    static int M1(char c)
    {
        if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            return 1;
        else
            return 0;
    }
    static int M2(char c)
    {
        if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
            return 1;
        else
            return 0;
    }
    public static void Main()
    {
        Console.Write(M1('A'));
        Console.Write(M1('0'));
        Console.Write(M1('q'));
        Console.Write(M1(' '));
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1010";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1", @"
    {
      // Code size       46 (0x2e)
      .maxstack  2
      .locals init (bool V_0,
                    bool V_1,
                    int V_2)
      IL_0000:  nop
      IL_0001:  ldarg.0
      IL_0002:  ldc.i4.s   97
      IL_0004:  blt.s      IL_000d
      IL_0006:  ldarg.0
      IL_0007:  ldc.i4.s   122
      IL_0009:  ble.s      IL_0019
      IL_000b:  br.s       IL_001d
      IL_000d:  ldarg.0
      IL_000e:  ldc.i4.s   65
      IL_0010:  blt.s      IL_001d
      IL_0012:  ldarg.0
      IL_0013:  ldc.i4.s   90
      IL_0015:  ble.s      IL_0019
      IL_0017:  br.s       IL_001d
      IL_0019:  ldc.i4.1
      IL_001a:  stloc.0
      IL_001b:  br.s       IL_001f
      IL_001d:  ldc.i4.0
      IL_001e:  stloc.0
      IL_001f:  ldloc.0
      IL_0020:  stloc.1
      IL_0021:  ldloc.1
      IL_0022:  brfalse.s  IL_0028
      IL_0024:  ldc.i4.1
      IL_0025:  stloc.2
      IL_0026:  br.s       IL_002c
      IL_0028:  ldc.i4.0
      IL_0029:  stloc.2
      IL_002a:  br.s       IL_002c
      IL_002c:  ldloc.2
      IL_002d:  ret
    }
");
            compVerifier.VerifyIL("C.M2", @"
    {
      // Code size       44 (0x2c)
      .maxstack  2
      .locals init (bool V_0,
                    int V_1)
      IL_0000:  nop
      IL_0001:  ldarg.0
      IL_0002:  ldc.i4.s   65
      IL_0004:  blt.s      IL_000b
      IL_0006:  ldarg.0
      IL_0007:  ldc.i4.s   90
      IL_0009:  ble.s      IL_001d
      IL_000b:  ldarg.0
      IL_000c:  ldc.i4.s   97
      IL_000e:  blt.s      IL_001a
      IL_0010:  ldarg.0
      IL_0011:  ldc.i4.s   122
      IL_0013:  cgt
      IL_0015:  ldc.i4.0
      IL_0016:  ceq
      IL_0018:  br.s       IL_001b
      IL_001a:  ldc.i4.0
      IL_001b:  br.s       IL_001e
      IL_001d:  ldc.i4.1
      IL_001e:  stloc.0
      IL_001f:  ldloc.0
      IL_0020:  brfalse.s  IL_0026
      IL_0022:  ldc.i4.1
      IL_0023:  stloc.1
      IL_0024:  br.s       IL_002a
      IL_0026:  ldc.i4.0
      IL_0027:  stloc.1
      IL_0028:  br.s       IL_002a
      IL_002a:  ldloc.1
      IL_002b:  ret
    }
");

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics();
            compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M1", @"
    {
      // Code size       35 (0x23)
      .maxstack  2
      .locals init (bool V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   97
      IL_0003:  blt.s      IL_000c
      IL_0005:  ldarg.0
      IL_0006:  ldc.i4.s   122
      IL_0008:  ble.s      IL_0016
      IL_000a:  br.s       IL_001a
      IL_000c:  ldarg.0
      IL_000d:  ldc.i4.s   65
      IL_000f:  blt.s      IL_001a
      IL_0011:  ldarg.0
      IL_0012:  ldc.i4.s   90
      IL_0014:  bgt.s      IL_001a
      IL_0016:  ldc.i4.1
      IL_0017:  stloc.0
      IL_0018:  br.s       IL_001c
      IL_001a:  ldc.i4.0
      IL_001b:  stloc.0
      IL_001c:  ldloc.0
      IL_001d:  brfalse.s  IL_0021
      IL_001f:  ldc.i4.1
      IL_0020:  ret
      IL_0021:  ldc.i4.0
      IL_0022:  ret
    }
");
            compVerifier.VerifyIL("C.M2", @"
    {
      // Code size       24 (0x18)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   65
      IL_0003:  blt.s      IL_000a
      IL_0005:  ldarg.0
      IL_0006:  ldc.i4.s   90
      IL_0008:  ble.s      IL_0014
      IL_000a:  ldarg.0
      IL_000b:  ldc.i4.s   97
      IL_000d:  blt.s      IL_0016
      IL_000f:  ldarg.0
      IL_0010:  ldc.i4.s   122
      IL_0012:  bgt.s      IL_0016
      IL_0014:  ldc.i4.1
      IL_0015:  ret
      IL_0016:  ldc.i4.0
      IL_0017:  ret
    }
");
        }

        #endregion Pattern Combinators
    }
}
