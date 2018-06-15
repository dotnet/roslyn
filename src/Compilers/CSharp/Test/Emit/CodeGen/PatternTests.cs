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
                // (17,36): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //     static bool M2(int? x) => x is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(17, 36)
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
                // (12,9): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //         switch (x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"switch (x)
        {
            case int i: break;
        }").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(12, 9),
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
  IL_0018:  dup
  IL_0019:  stloc.2
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
  // Code size       94 (0x5e)
  .maxstack  2
  .locals init (Base<object> V_0, //x
                Derived V_1, //y
                Base<object> V_2,
                Derived V_3,
                Derived V_4, //z
                Base<object> V_5)
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
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  ldnull
  IL_001f:  cgt.un
  IL_0021:  call       ""void System.Console.WriteLine(bool)""
  IL_0026:  nop
  IL_0027:  ldloc.0
  IL_0028:  stloc.s    V_5
  IL_002a:  ldloc.s    V_5
  IL_002c:  stloc.2
  IL_002d:  ldloc.2
  IL_002e:  brtrue.s   IL_0032
  IL_0030:  br.s       IL_003e
  IL_0032:  ldloc.2
  IL_0033:  isinst     ""Derived""
  IL_0038:  dup
  IL_0039:  stloc.3
  IL_003a:  brfalse.s  IL_003e
  IL_003c:  br.s       IL_0040
  IL_003e:  br.s       IL_004e
  IL_0040:  ldloc.3
  IL_0041:  stloc.s    V_4
  IL_0043:  br.s       IL_0045
  IL_0045:  ldc.i4.1
  IL_0046:  call       ""void System.Console.WriteLine(bool)""
  IL_004b:  nop
  IL_004c:  br.s       IL_004e
  IL_004e:  ldloc.0
  IL_004f:  isinst     ""Derived""
  IL_0054:  ldnull
  IL_0055:  cgt.un
  IL_0057:  call       ""void System.Console.WriteLine(bool)""
  IL_005c:  nop
  IL_005d:  ret
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
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (T V_0, //v
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  box        ""T""
  IL_0009:  ldnull
  IL_000a:  cgt.un
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.1
  IL_0010:  ret
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
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (T V_0, //v
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""T ConsoleApp1.Result<T>.Value.get""
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  box        ""T""
  IL_000e:  ldnull
  IL_000f:  ceq
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  newobj     ""ConsoleApp1.NotPossibleException..ctor()""
  IL_001a:  throw
  IL_001b:  ret
}");
        }
    }
}
