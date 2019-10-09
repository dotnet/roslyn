// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.StackAllocInitializer)]
    public class CodeGenStackAllocInitializerTests : CompilingTestBase
    {
        [Fact]
        [WorkItem(29092, "https://github.com/dotnet/roslyn/issues/29092")]
        public void TestMixedWithInitBlock()
        {

            var text = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        MakeBlock(1, 2, 3);
    }

    static unsafe void MakeBlock(int a, int b, int c)
    {
        int* ptr = stackalloc int[]
        {
           0, 0, 0, a, b, c
        };
        PrintBytes(ptr, 6);
    }

    static unsafe void PrintBytes(int* ptr, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Console.Write(ptr[i]);
        }
    }
}";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                expectedOutput: "000123",
                verify: Verification.Fails).VerifyIL("Program.MakeBlock",
@"{
  // Code size       42 (0x2a)
  .maxstack  4
  IL_0000:  ldc.i4.s   24
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.s   24
  IL_0009:  initblk
  IL_000b:  dup
  IL_000c:  ldc.i4.3
  IL_000d:  conv.i
  IL_000e:  ldc.i4.4
  IL_000f:  mul
  IL_0010:  add
  IL_0011:  ldarg.0
  IL_0012:  stind.i4
  IL_0013:  dup
  IL_0014:  ldc.i4.4
  IL_0015:  conv.i
  IL_0016:  ldc.i4.4
  IL_0017:  mul
  IL_0018:  add
  IL_0019:  ldarg.1
  IL_001a:  stind.i4
  IL_001b:  dup
  IL_001c:  ldc.i4.5
  IL_001d:  conv.i
  IL_001e:  ldc.i4.4
  IL_001f:  mul
  IL_0020:  add
  IL_0021:  ldarg.2
  IL_0022:  stind.i4
  IL_0023:  ldc.i4.6
  IL_0024:  call       ""void Program.PrintBytes(int*, int)""
  IL_0029:  ret
}");
        }

        [Fact]
        public void TestUnmanaged_Pointer()
        {
            var text = @"
using System;
unsafe class Test
{
    static void Print<T>(T* p) where T : unmanaged
    {
        for (int i = 0; i < 3; i++)
            Console.Write(p[i]);
    }

    static void M<T>(T arg) where T : unmanaged
    {
        var obj1 = stackalloc T[3] { arg, arg, arg };
        Print<T>(obj1);
        var obj2 = stackalloc T[ ] { arg, arg, arg };
        Print<T>(obj2);
        var obj3 = stackalloc  [ ] { arg, arg, arg };
        Print<T>(obj3);
    }

    public static void Main()
    {
        M(42);
    }
}";
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeReleaseExe,
                expectedOutput: "424242424242424242",
                verify: Verification.Fails).VerifyIL("Test.M<T>(T)",
@"{
  // Code size      163 (0xa3)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  sizeof     ""T""
  IL_0008:  mul.ovf.un
  IL_0009:  localloc
  IL_000b:  dup
  IL_000c:  ldarg.0
  IL_000d:  stobj      ""T""
  IL_0012:  dup
  IL_0013:  sizeof     ""T""
  IL_0019:  add
  IL_001a:  ldarg.0
  IL_001b:  stobj      ""T""
  IL_0020:  dup
  IL_0021:  ldc.i4.2
  IL_0022:  conv.i
  IL_0023:  sizeof     ""T""
  IL_0029:  mul
  IL_002a:  add
  IL_002b:  ldarg.0
  IL_002c:  stobj      ""T""
  IL_0031:  call       ""void Test.Print<T>(T*)""
  IL_0036:  ldc.i4.3
  IL_0037:  conv.u
  IL_0038:  sizeof     ""T""
  IL_003e:  mul.ovf.un
  IL_003f:  localloc
  IL_0041:  dup
  IL_0042:  ldarg.0
  IL_0043:  stobj      ""T""
  IL_0048:  dup
  IL_0049:  sizeof     ""T""
  IL_004f:  add
  IL_0050:  ldarg.0
  IL_0051:  stobj      ""T""
  IL_0056:  dup
  IL_0057:  ldc.i4.2
  IL_0058:  conv.i
  IL_0059:  sizeof     ""T""
  IL_005f:  mul
  IL_0060:  add
  IL_0061:  ldarg.0
  IL_0062:  stobj      ""T""
  IL_0067:  call       ""void Test.Print<T>(T*)""
  IL_006c:  ldc.i4.3
  IL_006d:  conv.u
  IL_006e:  sizeof     ""T""
  IL_0074:  mul.ovf.un
  IL_0075:  localloc
  IL_0077:  dup
  IL_0078:  ldarg.0
  IL_0079:  stobj      ""T""
  IL_007e:  dup
  IL_007f:  sizeof     ""T""
  IL_0085:  add
  IL_0086:  ldarg.0
  IL_0087:  stobj      ""T""
  IL_008c:  dup
  IL_008d:  ldc.i4.2
  IL_008e:  conv.i
  IL_008f:  sizeof     ""T""
  IL_0095:  mul
  IL_0096:  add
  IL_0097:  ldarg.0
  IL_0098:  stobj      ""T""
  IL_009d:  call       ""void Test.Print<T>(T*)""
  IL_00a2:  ret
}");
        }

        [Fact]
        public void TestUnmanaged_Span()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M<T>(T arg) where T : unmanaged
    {
        Span<T> obj1 = stackalloc T[3] { arg, arg, arg };
        Span<T> obj2 = stackalloc T[ ] { arg, arg, arg };
        Span<T> obj3 = stackalloc  [ ] { arg, arg, arg };
    }
}
", options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3));

            CompileAndVerify(comp, verify: Verification.Fails).VerifyIL("Test.M<T>(T)",
@"{
  // Code size      169 (0xa9)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  sizeof     ""T""
  IL_0008:  mul.ovf.un
  IL_0009:  localloc
  IL_000b:  dup
  IL_000c:  ldarg.1
  IL_000d:  stobj      ""T""
  IL_0012:  dup
  IL_0013:  sizeof     ""T""
  IL_0019:  add
  IL_001a:  ldarg.1
  IL_001b:  stobj      ""T""
  IL_0020:  dup
  IL_0021:  ldc.i4.2
  IL_0022:  conv.i
  IL_0023:  sizeof     ""T""
  IL_0029:  mul
  IL_002a:  add
  IL_002b:  ldarg.1
  IL_002c:  stobj      ""T""
  IL_0031:  ldc.i4.3
  IL_0032:  newobj     ""System.Span<T>..ctor(void*, int)""
  IL_0037:  pop
  IL_0038:  ldc.i4.3
  IL_0039:  conv.u
  IL_003a:  sizeof     ""T""
  IL_0040:  mul.ovf.un
  IL_0041:  localloc
  IL_0043:  dup
  IL_0044:  ldarg.1
  IL_0045:  stobj      ""T""
  IL_004a:  dup
  IL_004b:  sizeof     ""T""
  IL_0051:  add
  IL_0052:  ldarg.1
  IL_0053:  stobj      ""T""
  IL_0058:  dup
  IL_0059:  ldc.i4.2
  IL_005a:  conv.i
  IL_005b:  sizeof     ""T""
  IL_0061:  mul
  IL_0062:  add
  IL_0063:  ldarg.1
  IL_0064:  stobj      ""T""
  IL_0069:  ldc.i4.3
  IL_006a:  newobj     ""System.Span<T>..ctor(void*, int)""
  IL_006f:  pop
  IL_0070:  ldc.i4.3
  IL_0071:  conv.u
  IL_0072:  sizeof     ""T""
  IL_0078:  mul.ovf.un
  IL_0079:  localloc
  IL_007b:  dup
  IL_007c:  ldarg.1
  IL_007d:  stobj      ""T""
  IL_0082:  dup
  IL_0083:  sizeof     ""T""
  IL_0089:  add
  IL_008a:  ldarg.1
  IL_008b:  stobj      ""T""
  IL_0090:  dup
  IL_0091:  ldc.i4.2
  IL_0092:  conv.i
  IL_0093:  sizeof     ""T""
  IL_0099:  mul
  IL_009a:  add
  IL_009b:  ldarg.1
  IL_009c:  stobj      ""T""
  IL_00a1:  ldc.i4.3
  IL_00a2:  newobj     ""System.Span<T>..ctor(void*, int)""
  IL_00a7:  pop
  IL_00a8:  ret
}");
        }

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
            CompileAndVerify(text,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3),
                options: TestOptions.UnsafeDebugExe,
                expectedOutput: "12",
                verify: Verification.Fails).VerifyIL("C.Main",
@"{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (int* V_0) //p
  IL_0000:  nop
  IL_0001:  ldc.i4.s   16
  IL_0003:  conv.u
  IL_0004:  localloc
  IL_0006:  dup
  IL_0007:  ldc.i4.s   42
  IL_0009:  stind.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.4
  IL_000c:  add
  IL_000d:  ldc.i4.1
  IL_000e:  call       ""byte C.Method(int)""
  IL_0013:  stind.i4
  IL_0014:  dup
  IL_0015:  ldc.i4.2
  IL_0016:  conv.i
  IL_0017:  ldc.i4.4
  IL_0018:  mul
  IL_0019:  add
  IL_001a:  ldc.i4.s   42
  IL_001c:  stind.i4
  IL_001d:  dup
  IL_001e:  ldc.i4.3
  IL_001f:  conv.i
  IL_0020:  ldc.i4.4
  IL_0021:  mul
  IL_0022:  add
  IL_0023:  ldc.i4.2
  IL_0024:  call       ""byte C.Method(int)""
  IL_0029:  stind.i4
  IL_002a:  stloc.0
  IL_002b:  ret
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
  IL_0006:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=9 <PrivateImplementationDetails>.5248358BD96335E3BA4BB5D100E25AD64FAF4ADA8E613568E449FF981304C025""
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
                verify: Verification.Fails, expectedOutput: @"123").VerifyIL("C.Main",
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
  IL_0005:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
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
            var comp = CreateCompilation(@"
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
@"
{
  // Code size       21 (0x15)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  localloc
  IL_0004:  dup
  IL_0005:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
  IL_000a:  ldc.i4.3
  IL_000b:  cpblk
  IL_000d:  ldc.i4.3
  IL_000e:  newobj     ""System.Span<byte>..ctor(void*, int)""
  IL_0013:  pop
  IL_0014:  ret
}
");
        }

        [Fact]
        public void TestReadOnlySpan()
        {
            var comp = CreateCompilation(@"
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
@"
{
  // Code size       34 (0x22)
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
  IL_0015:  ldc.i4.3
  IL_0016:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_001b:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.op_Implicit(System.Span<int>)""
  IL_0020:  pop
  IL_0021:  ret
}
");
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
