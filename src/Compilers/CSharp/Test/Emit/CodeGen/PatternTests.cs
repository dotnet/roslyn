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
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
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
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
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
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (12,9): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //         switch (x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"switch (x)
        {
            case int i: break;
        }").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(12, 9),
                // (12,9): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         switch (x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"switch (x)
        {
            case int i: break;
        }").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(12, 9),
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(17, 36),
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
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (12,9): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //         switch (x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"switch (x)
        {
            case int i: break;
        }").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(12, 9),
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(17, 36),
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.get_Value'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_Value").WithLocation(17, 36)
                );
        }

        [Fact(Skip = "PROTOTYPE(patterns2): code quality"), WorkItem(17266, "https://github.com/dotnet/roslyn/issues/17266")]
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
            var compilation = CreateStandardCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"eval";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.Main",
@"{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init (int V_0, //index
                bool V_1,
                int? V_2)
  IL_0000:  nop
  IL_0001:  call       ""int? C.TryGet()""
  IL_0006:  stloc.2
  IL_0007:  ldloca.s   V_2
  IL_0009:  call       ""int int?.GetValueOrDefault()""
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_2
  IL_0011:  call       ""bool int?.HasValue.get""
  IL_0016:  stloc.1
  IL_0017:  ldloc.1
  IL_0018:  brfalse.s  IL_0023
  IL_001a:  nop
  IL_001b:  ldloc.0
  IL_001c:  call       ""void System.Console.WriteLine(int)""
  IL_0021:  nop
  IL_0022:  nop
  IL_0023:  ret
}");
        }

        [Fact(Skip = "PROTOTYPE(patterns2): code quality"), WorkItem(19122, "https://github.com/dotnet/roslyn/issues/19122")]
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
            var compilation = CreateStandardCompilation(source, options: TestOptions.DebugDll, references: new[] { LinqAssemblyRef });
            compilation.VerifyDiagnostics();
            var compVerifier = CompileAndVerify(compilation);
            compVerifier.VerifyIL("X<T>.Y<U>",
@"{
  // Code size       67 (0x43)
  .maxstack  3
  .locals init (U V_0, //u
                bool V_1,
                object V_2,
                U V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""System.Collections.Generic.IEnumerable<T> X<T>.GetT()""
  IL_0007:  ldarg.0
  IL_0008:  ldftn      ""bool X<T>.<Y>b__1_0<U>(T)""
  IL_000e:  newobj     ""System.Func<T, bool>..ctor(object, System.IntPtr)""
  IL_0013:  call       ""T System.Linq.Enumerable.FirstOrDefault<T>(System.Collections.Generic.IEnumerable<T>, System.Func<T, bool>)""
  IL_0018:  box        ""T""
  IL_001d:  stloc.2
  IL_001e:  ldloc.2
  IL_001f:  isinst     ""U""
  IL_0024:  ldnull
  IL_0025:  cgt.un
  IL_0027:  dup
  IL_0028:  brtrue.s   IL_0035
  IL_002a:  ldloca.s   V_3
  IL_002c:  initobj    ""U""
  IL_0032:  ldloc.3
  IL_0033:  br.s       IL_003b
  IL_0035:  ldloc.2
  IL_0036:  unbox.any  ""U""
  IL_003b:  stloc.0
  IL_003c:  stloc.1
  IL_003d:  ldloc.1
  IL_003e:  brfalse.s  IL_0042
  IL_0040:  nop
  IL_0041:  nop
  IL_0042:  ret
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
            var compilation = CreateStandardCompilation(source, options: TestOptions.DebugExe);
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
  // Code size      162 (0xa2)
  .maxstack  2
  .locals init (object V_0,
                double V_1,
                float V_2,
                object V_3,
                object V_4,
                bool V_5)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.3
  IL_0003:  ldloc.3
  IL_0004:  stloc.0
  IL_0005:  ldloc.0
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  br.s       IL_0084
  IL_000a:  ldloc.0
  IL_000b:  stloc.s    V_4
  IL_000d:  ldloc.s    V_4
  IL_000f:  isinst     ""double""
  IL_0014:  ldnull
  IL_0015:  cgt.un
  IL_0017:  dup
  IL_0018:  brtrue.s   IL_0025
  IL_001a:  ldc.r8     0
  IL_0023:  br.s       IL_002c
  IL_0025:  ldloc.s    V_4
  IL_0027:  unbox.any  ""double""
  IL_002c:  stloc.1
  IL_002d:  brfalse.s  IL_004b
  IL_002f:  ldloc.1
  IL_0030:  call       ""bool double.IsNaN(double)""
  IL_0035:  brtrue.s   IL_0039
  IL_0037:  br.s       IL_003b
  IL_0039:  br.s       IL_0086
  IL_003b:  ldc.r8     3.14
  IL_0044:  ldloc.1
  IL_0045:  beq.s      IL_0049
  IL_0047:  br.s       IL_004b
  IL_0049:  br.s       IL_0090
  IL_004b:  ldloc.0
  IL_004c:  stloc.s    V_4
  IL_004e:  ldloc.s    V_4
  IL_0050:  isinst     ""float""
  IL_0055:  ldnull
  IL_0056:  cgt.un
  IL_0058:  dup
  IL_0059:  brtrue.s   IL_0062
  IL_005b:  ldc.r4     0
  IL_0060:  br.s       IL_0069
  IL_0062:  ldloc.s    V_4
  IL_0064:  unbox.any  ""float""
  IL_0069:  stloc.2
  IL_006a:  brfalse.s  IL_0084
  IL_006c:  ldloc.2
  IL_006d:  call       ""bool float.IsNaN(float)""
  IL_0072:  brtrue.s   IL_0076
  IL_0074:  br.s       IL_0078
  IL_0076:  br.s       IL_008b
  IL_0078:  ldc.r4     3.14
  IL_007d:  ldloc.2
  IL_007e:  beq.s      IL_0082
  IL_0080:  br.s       IL_0084
  IL_0082:  br.s       IL_0095
  IL_0084:  br.s       IL_009a
  IL_0086:  ldc.i4.1
  IL_0087:  stloc.s    V_5
  IL_0089:  br.s       IL_009f
  IL_008b:  ldc.i4.1
  IL_008c:  stloc.s    V_5
  IL_008e:  br.s       IL_009f
  IL_0090:  ldc.i4.1
  IL_0091:  stloc.s    V_5
  IL_0093:  br.s       IL_009f
  IL_0095:  ldc.i4.1
  IL_0096:  stloc.s    V_5
  IL_0098:  br.s       IL_009f
  IL_009a:  ldc.i4.0
  IL_009b:  stloc.s    V_5
  IL_009d:  br.s       IL_009f
  IL_009f:  ldloc.s    V_5
  IL_00a1:  ret
}");
        }
    }
}
