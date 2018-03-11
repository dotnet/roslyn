// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.StackAllocInitializer)]
    public class CodeGenStackAllocInitializerTests : CompilingTestBase
    {
        [Fact]
        public void TestLambdaCapture()
        {
            var text = @"
using System;
public class C
{
    unsafe public static void Main() 
    {
        // captured by the lambda. 
        byte x = 1;
        byte* b = stackalloc byte[] {((Func<byte>)(() => x++))(), x};
        Console.Write(b[1]);
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                expectedOutput: "2",
                verify: Verification.Fails).VerifyIL("C.Main",
@"{
  // Code size       55 (0x37)
  .maxstack  4
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      ""byte C.<>c__DisplayClass0_0.x""
  IL_000d:  ldc.i4.2
  IL_000e:  conv.u
  IL_000f:  localloc
  IL_0011:  dup
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""byte C.<>c__DisplayClass0_0.<Main>b__0()""
  IL_0019:  newobj     ""System.Func<byte>..ctor(object, System.IntPtr)""
  IL_001e:  callvirt   ""byte System.Func<byte>.Invoke()""
  IL_0023:  stind.i1
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  ldloc.0
  IL_0028:  ldfld      ""byte C.<>c__DisplayClass0_0.x""
  IL_002d:  stind.i1
  IL_002e:  ldc.i4.1
  IL_002f:  add
  IL_0030:  ldind.u1
  IL_0031:  call       ""void System.Console.Write(int)""
  IL_0036:  ret
}");
        }

        [Fact]
        public void TestElementThrow()
        {
            var text = @"
using System;

static unsafe class C
{
    static void Use(int* i) {}
    
    static int M() { return 0; }

    static void Main()
    {
        var p = stackalloc[] { M(), true ? throw null : M(), M() };
        Use(p);
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails).VerifyIL("C.Main",
@"{
  // Code size       17 (0x11)
  .maxstack  4
  IL_0000:  ldc.i4.s   12
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  call       ""int C.M()""
  IL_000b:  stind.i4
  IL_000c:  dup
  IL_000d:  ldc.i4.4
  IL_000e:  add
  IL_000f:  ldnull
  IL_0010:  throw
}");
        }

        [Fact]
        public void TestUnused()
        {
            var text = @"
using System;

static unsafe class C
{
    static byte Method(int i)
    {
        Console.Write(i);
        return 0;
    }

    static void Main()
    {
        var p = stackalloc[] { 42, Method(1), 42, Method(2) };
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                expectedOutput: "12",
                verify: Verification.Passes).VerifyIL("C.Main",
@"{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  ldc.i4.s   16
  IL_0002:  conv.u
  IL_0003:  pop
  IL_0004:  ldc.i4.1
  IL_0005:  call       ""byte C.Method(int)""
  IL_000a:  pop
  IL_000b:  ldc.i4.2
  IL_000c:  call       ""byte C.Method(int)""
  IL_0011:  pop
  IL_0012:  ret
}");
        }

        [Fact]
        public void TestEmpty()
        {
            var text = @"
using System;

static unsafe class C
{
    static void Use(int* p)
    {
    }

    static void Main()
    {
        var p = stackalloc int[] {};
        Use(p);
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails).VerifyIL("C.Main",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  call       ""void C.Use(int*)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void TestIdenticalBytes1()
        {
            var text = @"
using System;

static unsafe class C
{
    static void Print(byte* p)
    {
        for (int i = 0; i < 3; i++)
            Console.Write(p[i]);
    }

    static void Main()
    {
        byte* p = stackalloc byte[3] { 42, 42, 42 };
        Print(p);
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails, expectedOutput: @"424242").VerifyIL("C.Main",
@"{
  // Code size       16 (0x10)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  localloc
  IL_0004:  dup
  IL_0005:  ldc.i4.s   42
  IL_0007:  ldc.i4.3
  IL_0008:  initblk
  IL_000a:  call       ""void C.Print(byte*)""
  IL_000f:  ret
}");
        }

        [Fact]
        public void TestIdenticalBytes2()
        {
            var text = @"
using System;

static unsafe class C
{
    static void Print(uint* p)
    {
        for (int i = 0; i < 3; i++)
            Console.Write(p[i].ToString(""x""));
    }

    static void Main()
    {
        var p = stackalloc[] { 0xffffffff, 0xffffffff, 0xffffffff };
        Print(p);
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails, expectedOutput: @"ffffffffffffffffffffffff").VerifyIL("C.Main",
@"{
  // Code size       21 (0x15)
  .maxstack  4
  IL_0000:  ldc.i4.s   12
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  ldc.i4     0xff
  IL_000b:  ldc.i4.s   12
  IL_000d:  initblk
  IL_000f:  call       ""void C.Print(uint*)""
  IL_0014:  ret
}");
        }

        [Fact]
        public void TestMixedBlockInit()
        {
            var text = @"
using System;

static unsafe class C
{
    static void Print(byte* p)
    {
        for (int i = 0; i < 9; i++)
            Console.Write(p[i]);
    }

    static byte M()
    {
        return 9;
    }

    static void Main()
    {
        byte* p = stackalloc byte[9] { 1, 2, 3, 4, 5, 6, 7, 8, M() };
        Print(p);
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails, expectedOutput: @"123456789").VerifyIL("C.Main",
@"{
  // Code size       30 (0x1e)
  .maxstack  4
  IL_0000:  ldc.i4.s   9
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=9 <PrivateImplementationDetails>.50BE2890A92A315C4BE0FA98C600C8939A260312""
  IL_000b:  ldc.i4.s   9
  IL_000d:  cpblk
  IL_000f:  dup
  IL_0010:  ldc.i4.8
  IL_0011:  add
  IL_0012:  call       ""byte C.M()""
  IL_0017:  stind.i1
  IL_0018:  call       ""void C.Print(byte*)""
  IL_001d:  ret
}");
        }

        [Fact]
        public void TestElementInit()
        {
            var text = @"
using System;

struct S
{
    public int i;
}

static unsafe class C
{
    static void Print(S* p)
    {
        for (int i = 0; i < 3; i++)
            Console.Write(p[i].i);
    }

    static S M(int i)
    {
        return new S { i = i };
    }
    
    static void Main()
    {
        S* p = stackalloc S[3] { M(1), M(2), M(3) };
        Print(p);
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails ,expectedOutput: @"123").VerifyIL("C.Main",
@"{
  // Code size       70 (0x46)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  sizeof     ""S""
  IL_0008:  mul.ovf.un
  IL_0009:  localloc
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  call       ""S C.M(int)""
  IL_0012:  stobj      ""S""
  IL_0017:  dup
  IL_0018:  sizeof     ""S""
  IL_001e:  add
  IL_001f:  ldc.i4.2
  IL_0020:  call       ""S C.M(int)""
  IL_0025:  stobj      ""S""
  IL_002a:  dup
  IL_002b:  ldc.i4.2
  IL_002c:  conv.i
  IL_002d:  sizeof     ""S""
  IL_0033:  mul
  IL_0034:  add
  IL_0035:  ldc.i4.3
  IL_0036:  call       ""S C.M(int)""
  IL_003b:  stobj      ""S""
  IL_0040:  call       ""void C.Print(S*)""
  IL_0045:  ret
}");
        }

        [Fact]
        public void TestBytePointer()
        {
            Test("System.Byte",
@"{
  // Code size       19 (0x13)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  localloc
  IL_0004:  dup
  IL_0005:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.7037807198C22A7D2B0807371D763779A84FDFCF""
  IL_000a:  ldc.i4.3
  IL_000b:  cpblk
  IL_000d:  call       ""void C.Print(byte*)""
  IL_0012:  ret
}");
        }

        [Fact]
        public void TestInt32Pointer()
        {
            Test("System.Int32",
@"{
  // Code size       27 (0x1b)
  .maxstack  4
  IL_0000:  ldc.i4.s   12
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  stind.i4
  IL_0008:  dup
  IL_0009:  ldc.i4.4
  IL_000a:  add
  IL_000b:  ldc.i4.2
  IL_000c:  stind.i4
  IL_000d:  dup
  IL_000e:  ldc.i4.2
  IL_000f:  conv.i
  IL_0010:  ldc.i4.4
  IL_0011:  mul
  IL_0012:  add
  IL_0013:  ldc.i4.3
  IL_0014:  stind.i4
  IL_0015:  call       ""void C.Print(int*)""
  IL_001a:  ret
}");
        }

        [Fact]
        public void TestInt64Pointer()
        {
            Test("System.Int64",
@"{
  // Code size       30 (0x1e)
  .maxstack  4
  IL_0000:  ldc.i4.s   24
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  conv.i8
  IL_0008:  stind.i8
  IL_0009:  dup
  IL_000a:  ldc.i4.8
  IL_000b:  add
  IL_000c:  ldc.i4.2
  IL_000d:  conv.i8
  IL_000e:  stind.i8
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  conv.i
  IL_0012:  ldc.i4.8
  IL_0013:  mul
  IL_0014:  add
  IL_0015:  ldc.i4.3
  IL_0016:  conv.i8
  IL_0017:  stind.i8
  IL_0018:  call       ""void C.Print(long*)""
  IL_001d:  ret
}");
        }

        [Fact]
        public void TestSpan()
        {
            var comp = CreateStandardCompilation(@"
using System;
static class C
{
    static void Main()
    {
        Span<byte> p = stackalloc byte[3] { 1, 2, 3 };
    }
}

namespace System
{
    public ref struct Span<T> {
        public unsafe Span(void* p, int length)
        {
            for (int i = 0; i < 3; i++)
                Console.Write(((byte*)p)[i]);
        }
    }
}
", options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3));
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"123")
                .VerifyIL("C.Main",
@"{
  // Code size       23 (0x17)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  conv.u
  IL_0004:  localloc
  IL_0006:  dup
  IL_0007:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.7037807198C22A7D2B0807371D763779A84FDFCF""
  IL_000c:  ldc.i4.3
  IL_000d:  cpblk
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""System.Span<byte>..ctor(void*, int)""
  IL_0015:  pop
  IL_0016:  ret
}");
        }

        [Fact]
        public void TestReadOnlySpan()
        {
            var comp = CreateStandardCompilation(@"
using System;
static class C
{
    static void Main()
    {
        ReadOnlySpan<int> p = stackalloc int[3] { 1, 2, 3 };
    }
}

namespace System
{
    public ref struct Span<T>
    {
        public unsafe Span(void* p, int length)
        {
            for (int i = 0; i < 3; i++)
                Console.Write(((int*)p)[i]);
        }
    }

    public readonly ref struct ReadOnlySpan<T>
    {
        public static implicit operator System.ReadOnlySpan<T> (System.Span<T> span) => default;
    }
}
", options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3));
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"123")
                .VerifyIL("C.Main",
@"{
  // Code size       37 (0x25)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  conv.u
  IL_0004:  ldc.i4.4
  IL_0005:  mul.ovf.un
  IL_0006:  localloc
  IL_0008:  dup
  IL_0009:  ldc.i4.1
  IL_000a:  stind.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.4
  IL_000d:  add
  IL_000e:  ldc.i4.2
  IL_000f:  stind.i4
  IL_0010:  dup
  IL_0011:  ldc.i4.2
  IL_0012:  conv.i
  IL_0013:  ldc.i4.4
  IL_0014:  mul
  IL_0015:  add
  IL_0016:  ldc.i4.3
  IL_0017:  stind.i4
  IL_0018:  ldloc.0
  IL_0019:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_001e:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.op_Implicit(System.Span<int>)""
  IL_0023:  pop
  IL_0024:  ret
}");
        }

        private static string GetSource(string pointerType) => $@"
using System;
using T = {pointerType};
static unsafe class C
{{
    static void Print(T* p)
    {{
        for (int i = 0; i < 3; i++)
            Console.Write(p[i]);
    }}

    static void Main()
    {{
        T* p = stackalloc T[3] {{ 1, 2, 3 }};
        Print(p);
    }}
}}
";

        private void Test(string pointerType, string il)
            => CompileAndVerify(GetSource(pointerType),
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Fails,
                expectedOutput: @"123").VerifyIL("C.Main", il);
    }
}
