// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.StackAllocInitializer)]
    public class StackAllocInitializerTests : CompilingTestBase
    {
        [Fact]
        public void TestUnused()
        {
            var text = @"
using System;

static unsafe class C
{
    static byte Method()
    {
        return 42;
    }

    static void Main()
    {
        byte* p = stackalloc byte[3] { 42, Method(), 42 };
    }
}
";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                verify: Verification.Passes).VerifyIL("C.Main",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  pop
  IL_0003:  call       ""byte C.Method()""
  IL_0008:  pop
  IL_0009:  ret
}");
        }

        [Fact]
        public void TestIdenticalBytes()
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
  // Code size       21 (0x15)
  .maxstack  4
  IL_0000:  ldc.i4.s   12
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.E429CCA3F703A39CC5954A6572FEC9086135B34E""
  IL_000b:  ldc.i4.s   12
  IL_000d:  cpblk
  IL_000f:  call       ""void C.Print(int*)""
  IL_0014:  ret
}");
        }

        [Fact]
        public void TestInt64Pointer()
        {
            Test("System.Int64",
@"{
  // Code size       21 (0x15)
  .maxstack  4
  IL_0000:  ldc.i4.s   24
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24 <PrivateImplementationDetails>.E2D1839ED1706F7D470D87F8C48A5584CAFA5A12""
  IL_000b:  ldc.i4.s   24
  IL_000d:  cpblk
  IL_000f:  call       ""void C.Print(long*)""
  IL_0014:  ret
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
  // Code size       31 (0x1f)
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
  IL_0009:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.E429CCA3F703A39CC5954A6572FEC9086135B34E""
  IL_000e:  ldc.i4.s   12
  IL_0010:  cpblk
  IL_0012:  ldloc.0
  IL_0013:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_0018:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.op_Implicit(System.Span<int>)""
  IL_001d:  pop
  IL_001e:  ret
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
