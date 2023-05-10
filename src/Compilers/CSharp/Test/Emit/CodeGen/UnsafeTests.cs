// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class UnsafeTests : EmitMetadataTestBase
    {
        #region AddressOf tests

        [Fact]
        public void AddressOfLocal_Unused()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int x;
        int* p = &x;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes);
            compVerifier.VerifyIL("C.M", @"
{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  pop
  IL_0003:  ret
}
");
        }

        [Fact]
        public void AddressOfLocal_Used()
        {
            var text = @"
unsafe class C
{
    void M(int* param)
    {
        int x;
        int* p = &x;
        M(p);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);
            compVerifier.VerifyIL("C.M", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (int V_0, //x
  int* V_1) //p
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  stloc.1
  IL_0004:  ldarg.0
  IL_0005:  ldloc.1
  IL_0006:  call       ""void C.M(int*)""
  IL_000b:  ret
}
");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67330")]
        public void Invocation_Pointer()
        {
            var text = @"
class C
{
    void M(int* param)
    {
        M(param);
    }
}
";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(int* param)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 12),
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         M(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "M(param)").WithLocation(6, 9),
                // (6,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         M(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "param").WithLocation(6, 11)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67330")]
        public void Invocation_PointerArray()
        {
            var text = @"
class C
{
    void M(int*[] param)
    {
        M(param);
    }
}
";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(int*[] param)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 12),
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         M(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "M(param)").WithLocation(6, 9),
                // (6,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         M(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "param").WithLocation(6, 11)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67330")]
        public void Invocation_PointerArray_Nested()
        {
            var text = @"
class C<T>
{
    void M(C<int*[]>[] param)
    {
        M(param);
    }
}
";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(C<int*[]>[] param)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 14),
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         M(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "M(param)").WithLocation(6, 9),
                // (6,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         M(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "param").WithLocation(6, 11)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67330")]
        public void ClassCreation_Pointer()
        {
            var text = @"
class C
{
    C(int* param)
    {
        new C(param);
    }
}
";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,7): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     C(int* param)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 7),
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new C(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C(param)").WithLocation(6, 9),
                // (6,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new C(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "param").WithLocation(6, 15)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67330")]
        public void ClassCreation_PointerArray()
        {
            var text = @"
class C
{
    C(int*[] param)
    {
        new C(param);
    }
}
";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,7): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     C(int*[] param)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(4, 7),
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new C(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C(param)").WithLocation(6, 9),
                // (6,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new C(param);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "param").WithLocation(6, 15)
                );
        }

        [Fact]
        public void AddressOfParameter_Unused()
        {
            var text = @"
unsafe class C
{
    void M(int x)
    {
        int* p = &x;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes);
            compVerifier.VerifyIL("C.M", @"
{
  // Code size        4 (0x4)
  .maxstack  1
  IL_0000:  ldarga.s   V_1
  IL_0002:  pop
  IL_0003:  ret
}
");
        }

        [Fact]
        public void AddressOfParameter_Used()
        {
            var text = @"
unsafe class C
{
    void M(int x, int* param)
    {
        int* p = &x;
        M(x, p);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);
            compVerifier.VerifyIL("C.M", @"
{
  // Code size       13 (0xd)
  .maxstack  3
  .locals init (int* V_0) //p
  IL_0000:  ldarga.s   V_1
  IL_0002:  conv.u
  IL_0003:  stloc.0
  IL_0004:  ldarg.0
  IL_0005:  ldarg.1
  IL_0006:  ldloc.0
  IL_0007:  call       ""void C.M(int, int*)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void AddressOfStructField()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        S1 s;
        S1* p1 = &s;
        S2* p2 = &s.s;
        int* p3 = &s.s.x;

        Goo(s, p1, p2, p3);
    }

    void Goo(S1 s, S1* p1, S2* p2, int* p3) { }
}

struct S1
{
    public S2 s;
}

struct S2
{
    public int x;
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);
            compVerifier.VerifyIL("C.M", @"
{
  // Code size       38 (0x26)
  .maxstack  5
  .locals init (S1 V_0, //s
  S1* V_1, //p1
  S2* V_2, //p2
  int* V_3) //p3
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  ldflda     ""S2 S1.s""
  IL_000b:  conv.u
  IL_000c:  stloc.2
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldflda     ""S2 S1.s""
  IL_0014:  ldflda     ""int S2.x""
  IL_0019:  conv.u
  IL_001a:  stloc.3
  IL_001b:  ldarg.0
  IL_001c:  ldloc.0
  IL_001d:  ldloc.1
  IL_001e:  ldloc.2
  IL_001f:  ldloc.3
  IL_0020:  call       ""void C.Goo(S1, S1*, S2*, int*)""
  IL_0025:  ret
}
");
        }

        [Fact]
        public void AddressOfSuppressOptimization()
        {
            var text = @"
unsafe class C
{
    static void M()
    {
        int x = 123;
        Goo(&x); // should not optimize into 'Goo(&123)'
    }

    static void Goo(int* p) { }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);
            compVerifier.VerifyIL("C.M", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  call       ""void C.Goo(int*)""
  IL_000b:  ret
}
");
        }

        #endregion AddressOf tests

        #region Dereference tests

        [Fact]
        public void DereferenceLocal()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        int x = 123;
        int* p = &x;
        System.Console.WriteLine(*p);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "123", verify: Verification.Fails);

            // NOTE: p is optimized away, but & and * aren't.
            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  ldind.i4
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void DereferenceParameter()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        long x = 456;
        System.Console.WriteLine(Dereference(&x));
    }

    static long Dereference(long* p)
    {
        return *p;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "456", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Dereference", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldind.i8
  IL_0002:  ret
}
");
        }

        [Fact]
        public void DereferenceWrite()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        int x = 1;
        int* p = &x;
        *p = 2;
        System.Console.WriteLine(x);
    }
}
";
            var compVerifierOptimized = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "2", verify: Verification.Fails);

            // NOTE: p is optimized away, but & and * aren't.
            compVerifierOptimized.VerifyIL("C.Main", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  conv.u
  IL_0005:  ldc.i4.2
  IL_0006:  stind.i4
  IL_0007:  ldloc.0
  IL_0008:  call       ""void System.Console.WriteLine(int)""
  IL_000d:  ret
}
");
            var compVerifierUnoptimized = CompileAndVerify(text, options: TestOptions.UnsafeDebugExe, expectedOutput: "2", verify: Verification.Fails);

            compVerifierUnoptimized.VerifyIL("C.Main", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (int V_0, //x
  int* V_1) //p
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.2
  IL_0009:  stind.i4
  IL_000a:  ldloc.0
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  nop
  IL_0011:  ret
}
");
        }

        [Fact]
        public void DereferenceStruct()
        {
            var text = @"
unsafe struct S
{
    S* p;
    byte x;

    static void Main()
    {
        S s;
        S* sp = &s;
        (*sp).p = sp;
        (*sp).x = 1;
        System.Console.WriteLine((*(s.p)).x);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "1", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (S V_0, //s
  S* V_1) //sp
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  ldloc.1
  IL_0006:  stfld      ""S* S.p""
  IL_000b:  ldloc.1
  IL_000c:  ldc.i4.1
  IL_000d:  stfld      ""byte S.x""
  IL_0012:  ldloc.0
  IL_0013:  ldfld      ""S* S.p""
  IL_0018:  ldfld      ""byte S.x""
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  ret
}
");
        }

        [Fact]
        public void DereferenceSwap()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        byte b1 = 2;
        byte b2 = 7;

        Console.WriteLine(""Before: {0} {1}"", b1, b2);
        Swap(&b1, &b2);
        Console.WriteLine(""After: {0} {1}"", b1, b2);
    }

    static void Swap(byte* p1, byte* p2)
    {
        byte tmp = *p1;
        *p1 = *p2;
        *p2 = tmp;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"Before: 2 7
After: 7 2", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Swap", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  .locals init (byte V_0) //tmp
  IL_0000:  ldarg.0
  IL_0001:  ldind.u1
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  ldarg.1
  IL_0005:  ldind.u1
  IL_0006:  stind.i1
  IL_0007:  ldarg.1
  IL_0008:  ldloc.0
  IL_0009:  stind.i1
  IL_000a:  ret
}
");
        }

        [Fact]
        public void DereferenceIsLValue1()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        char c = 'a';
        char* p = &c;

        Console.Write(c);
        Incr(ref *p);
        Console.Write(c);
    }

    static void Incr(ref char c)
    {
        c++;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"ab", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (char V_0) //c
  IL_0000:  ldc.i4.s   97
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  ldloc.0
  IL_0007:  call       ""void System.Console.Write(char)""
  IL_000c:  call       ""void C.Incr(ref char)""
  IL_0011:  ldloc.0
  IL_0012:  call       ""void System.Console.Write(char)""
  IL_0017:  ret
}
");
        }

        [Fact]
        public void DereferenceIsLValue2()
        {
            var text = @"
using System;

unsafe struct S
{

    int x;

    static void Main()
    {
        S s;
        s.x = 1;
        S* p = &s;
        Console.Write(s.x);
        (*p).Mutate();
        Console.Write(s.x);
    }

    void Mutate()
    {
        x++;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"12", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  stfld      ""int S.x""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldloc.0
  IL_000c:  ldfld      ""int S.x""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  call       ""void S.Mutate()""
  IL_001b:  ldloc.0
  IL_001c:  ldfld      ""int S.x""
  IL_0021:  call       ""void System.Console.Write(int)""
  IL_0026:  ret
}
");
        }

        #endregion Dereference tests

        #region Pointer member access tests

        [Fact]
        public void PointerMemberAccessRead()
        {
            var text = @"
using System;

unsafe struct S
{
    int x;
    
    static void Main()
    {
        S s;
        s.x = 3;
        S* p = &s;
        Console.Write(p->x);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"3", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  stfld      ""int S.x""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldfld      ""int S.x""
  IL_0010:  call       ""void System.Console.Write(int)""
  IL_0015:  ret
}
");

            compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"3", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  stfld      ""int S.x""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldfld      ""int S.x""
  IL_0010:  call       ""void System.Console.Write(int)""
  IL_0015:  ret
}
");
        }

        [Fact]
        public void PointerMemberAccessWrite()
        {
            var text = @"
using System;

unsafe struct S
{
    int x;
    
    static void Main()
    {
        S s;
        s.x = 3;
        S* p = &s;
        Console.Write(s.x);
        p->x = 4;
        Console.Write(s.x);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"34", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  stfld      ""int S.x""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldloc.0
  IL_000c:  ldfld      ""int S.x""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  ldc.i4.4
  IL_0017:  stfld      ""int S.x""
  IL_001c:  ldloc.0
  IL_001d:  ldfld      ""int S.x""
  IL_0022:  call       ""void System.Console.Write(int)""
  IL_0027:  ret
}
");

            compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"34", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  stfld      ""int S.x""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldloc.0
  IL_000c:  ldfld      ""int S.x""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  ldc.i4.4
  IL_0017:  stfld      ""int S.x""
  IL_001c:  ldloc.0
  IL_001d:  ldfld      ""int S.x""
  IL_0022:  call       ""void System.Console.Write(int)""
  IL_0027:  ret
}
");
        }

        [Fact]
        public void PointerMemberAccessInvoke()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S s;
        S* p = &s;
        p->M();
        p->M(1);
        p->M(1, 2);
    }

    void M() { Console.Write(1); }
    void M(int x) { Console.Write(2); }
}

static class Extensions
{
    public static void M(this S s, int x, int y) { Console.Write(3); }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"123", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  dup
  IL_0004:  call       ""void S.M()""
  IL_0009:  dup
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void S.M(int)""
  IL_0010:  ldobj      ""S""
  IL_0015:  ldc.i4.1
  IL_0016:  ldc.i4.2
  IL_0017:  call       ""void Extensions.M(S, int, int)""
  IL_001c:  ret
}
");

            compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"123", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  dup
  IL_0004:  call       ""void S.M()""
  IL_0009:  dup
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""void S.M(int)""
  IL_0010:  ldobj      ""S""
  IL_0015:  ldc.i4.1
  IL_0016:  ldc.i4.2
  IL_0017:  call       ""void Extensions.M(S, int, int)""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void PointerMemberAccessInvoke001()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S s;
        S* p = &s;
        Test(ref p);
    }

    static void Test(ref S* p)
    {
        p->M();
        p->M(1);
        p->M(1, 2);
    }

    void M() { Console.Write(1); }
    void M(int x) { Console.Write(2); }
}

static class Extensions
{
    public static void M(this S s, int x, int y) { Console.Write(3); }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"123", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Test(ref S*)", @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldind.i
  IL_0002:  call       ""void S.M()""
  IL_0007:  ldarg.0
  IL_0008:  ldind.i
  IL_0009:  ldc.i4.1
  IL_000a:  call       ""void S.M(int)""
  IL_000f:  ldarg.0
  IL_0010:  ldind.i
  IL_0011:  ldobj      ""S""
  IL_0016:  ldc.i4.1
  IL_0017:  ldc.i4.2
  IL_0018:  call       ""void Extensions.M(S, int, int)""
  IL_001d:  ret
}
");

            compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"123", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Test(ref S*)", @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldind.i
  IL_0002:  call       ""void S.M()""
  IL_0007:  ldarg.0
  IL_0008:  ldind.i
  IL_0009:  ldc.i4.1
  IL_000a:  call       ""void S.M(int)""
  IL_000f:  ldarg.0
  IL_0010:  ldind.i
  IL_0011:  ldobj      ""S""
  IL_0016:  ldc.i4.1
  IL_0017:  ldc.i4.2
  IL_0018:  call       ""void Extensions.M(S, int, int)""
  IL_001d:  ret
}
");
        }

        [Fact]
        public void PointerMemberAccessMutate()
        {
            var text = @"
using System;

unsafe struct S
{
    int x;
    
    static void Main()
    {
        S s;
        s.x = 3;
        S* p = &s;
        Console.Write((p->x)++);
        Console.Write((p->x)++);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"34", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (S V_0, //s
                int V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  stfld      ""int S.x""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  dup
  IL_000c:  ldflda     ""int S.x""
  IL_0011:  dup
  IL_0012:  ldind.i4
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stind.i4
  IL_0018:  ldloc.1
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ldflda     ""int S.x""
  IL_0023:  dup
  IL_0024:  ldind.i4
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.1
  IL_0028:  add
  IL_0029:  stind.i4
  IL_002a:  ldloc.1
  IL_002b:  call       ""void System.Console.Write(int)""
  IL_0030:  ret
}
");

            compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"34", verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (S V_0, //s
  int V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  stfld      ""int S.x""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  dup
  IL_000c:  ldflda     ""int S.x""
  IL_0011:  dup
  IL_0012:  ldind.i4
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stind.i4
  IL_0018:  ldloc.1
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ldflda     ""int S.x""
  IL_0023:  dup
  IL_0024:  ldind.i4
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.1
  IL_0028:  add
  IL_0029:  stind.i4
  IL_002a:  ldloc.1
  IL_002b:  call       ""void System.Console.Write(int)""
  IL_0030:  ret
}
");
        }

        #endregion Pointer member access tests

        #region Pointer element access tests

        [Fact]
        public void PointerElementAccessCheckedAndUnchecked()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S s = new S();
        S* p = &s;
        int i = (int)p;
        uint ui = (uint)p;
        long l = (long)p;
        ulong ul = (ulong)p;
        checked
        {
            s = p[i];
            s = p[ui];
            s = p[l];
            s = p[ul];
        }
        unchecked
        {
            s = p[i];
            s = p[ui];
            s = p[l];
            s = p[ul];
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);

            // The conversions differ from dev10 in the same way as for numeric addition.
            // Note that, unlike for numeric addition, the add operation is never checked.
            compVerifier.VerifyIL("S.Main", @"
{
  // Code size      170 (0xaa)
  .maxstack  4
  .locals init (S V_0, //s
                int V_1, //i
                uint V_2, //ui
                long V_3, //l
                ulong V_4) //ul
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  dup
  IL_000c:  conv.i4
  IL_000d:  stloc.1
  IL_000e:  dup
  IL_000f:  conv.u4
  IL_0010:  stloc.2
  IL_0011:  dup
  IL_0012:  conv.u8
  IL_0013:  stloc.3
  IL_0014:  dup
  IL_0015:  conv.u8
  IL_0016:  stloc.s    V_4
  IL_0018:  dup
  IL_0019:  ldloc.1
  IL_001a:  conv.i
  IL_001b:  sizeof     ""S""
  IL_0021:  mul.ovf
  IL_0022:  add
  IL_0023:  ldobj      ""S""
  IL_0028:  stloc.0
  IL_0029:  dup
  IL_002a:  ldloc.2
  IL_002b:  conv.u8
  IL_002c:  sizeof     ""S""
  IL_0032:  conv.i8
  IL_0033:  mul.ovf
  IL_0034:  conv.i
  IL_0035:  add
  IL_0036:  ldobj      ""S""
  IL_003b:  stloc.0
  IL_003c:  dup
  IL_003d:  ldloc.3
  IL_003e:  sizeof     ""S""
  IL_0044:  conv.i8
  IL_0045:  mul.ovf
  IL_0046:  conv.i
  IL_0047:  add
  IL_0048:  ldobj      ""S""
  IL_004d:  stloc.0
  IL_004e:  dup
  IL_004f:  ldloc.s    V_4
  IL_0051:  sizeof     ""S""
  IL_0057:  conv.ovf.u8
  IL_0058:  mul.ovf.un
  IL_0059:  conv.u
  IL_005a:  add
  IL_005b:  ldobj      ""S""
  IL_0060:  stloc.0
  IL_0061:  dup
  IL_0062:  ldloc.1
  IL_0063:  conv.i
  IL_0064:  sizeof     ""S""
  IL_006a:  mul
  IL_006b:  add
  IL_006c:  ldobj      ""S""
  IL_0071:  stloc.0
  IL_0072:  dup
  IL_0073:  ldloc.2
  IL_0074:  conv.u8
  IL_0075:  sizeof     ""S""
  IL_007b:  conv.i8
  IL_007c:  mul
  IL_007d:  conv.i
  IL_007e:  add
  IL_007f:  ldobj      ""S""
  IL_0084:  stloc.0
  IL_0085:  dup
  IL_0086:  ldloc.3
  IL_0087:  sizeof     ""S""
  IL_008d:  conv.i8
  IL_008e:  mul
  IL_008f:  conv.i
  IL_0090:  add
  IL_0091:  ldobj      ""S""
  IL_0096:  stloc.0
  IL_0097:  ldloc.s    V_4
  IL_0099:  sizeof     ""S""
  IL_009f:  conv.i8
  IL_00a0:  mul
  IL_00a1:  conv.u
  IL_00a2:  add
  IL_00a3:  ldobj      ""S""
  IL_00a8:  stloc.0
  IL_00a9:  ret
}
");
        }

        [Fact]
        public void PointerElementAccessWrite()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        int* p = null;
        p[1] = 2;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);

            compVerifier.VerifyIL("S.Main", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  .locals init (int* V_0) //p
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.4
  IL_0005:  add
  IL_0006:  ldc.i4.2
  IL_0007:  stind.i4
  IL_0008:  ret
}
");
        }

        [Fact]
        public void PointerElementAccessMutate()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        int[] array = new int[3];
        fixed (int* p = array)
        {
            p[1] += ++p[0];
            p[2] -= p[1]--;
        }

        foreach (int element in array)
        {
            Console.WriteLine(element);
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"
1
0
-1", verify: Verification.Fails);
        }

        [Fact]
        public void PointerElementAccessNested()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        fixed (int* q = new int[3])
        {
            q[0] = 2;
            q[1] = 0;
            q[2] = 1;

            Console.Write(q[q[q[q[q[q[*q]]]]]]);
            Console.Write(q[q[q[q[q[q[q[*q]]]]]]]);
            Console.Write(q[q[q[q[q[q[q[q[*q]]]]]]]]);
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "210", verify: Verification.Fails);
        }

        [Fact]
        public void PointerElementAccessZero()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        int x = 1;
        int* p = &x;
        Console.WriteLine(p[0]);
    }
}
";
            // NOTE: no pointer arithmetic - just dereference p.
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "1", verify: Verification.Fails).VerifyIL("C.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  conv.u
  IL_0005:  ldind.i4
  IL_0006:  call       ""void System.Console.WriteLine(int)""
  IL_000b:  ret
}
");
        }

        #endregion Pointer element access tests

        #region Fixed statement tests

        [Fact]
        public void FixedStatementField()
        {
            var text = @"
using System;

unsafe class C
{
    int x;
    
    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.x)
        {
            *p = 1;
        }
        Console.WriteLine(c.x);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"1", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  .locals init (pinned int& V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""int C.x""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  conv.u
  IL_000e:  ldc.i4.1
  IL_000f:  stind.i4
  IL_0010:  ldc.i4.0
  IL_0011:  conv.u
  IL_0012:  stloc.0
  IL_0013:  ldfld      ""int C.x""
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ret
}
");
        }

        [Fact]
        public void FixedStatementThis()
        {
            var text = @"
public class Program
{
    public static void Main()
    {
        S1 s = default;
        s.Test();
    }

    unsafe readonly struct S1
    {
        readonly int x;

        public void Test()
        {
            fixed(void* p = &this)
            {
                *(int*)p = 123;
            }

            ref readonly S1 r = ref this;

            fixed (S1* p = &r)
            {
                System.Console.WriteLine(p->x);
            }
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"123", verify: Verification.Fails);

            compVerifier.VerifyIL("Program.S1.Test()", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (void* V_0, //p
                pinned Program.S1& V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloc.1
  IL_0003:  conv.u
  IL_0004:  stloc.0
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.s   123
  IL_0008:  stind.i4
  IL_0009:  ldc.i4.0
  IL_000a:  conv.u
  IL_000b:  stloc.1
  IL_000c:  ldarg.0
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  conv.u
  IL_0010:  ldfld      ""int Program.S1.x""
  IL_0015:  call       ""void System.Console.WriteLine(int)""
  IL_001a:  ldc.i4.0
  IL_001b:  conv.u
  IL_001c:  stloc.1
  IL_001d:  ret
}
");
        }

        [WorkItem(22306, "https://github.com/dotnet/roslyn/issues/22306")]
        [Fact]
        public void FixedStatementMultipleFields()
        {
            var text = @"
using System;

unsafe class C
{
    int x;
    readonly int y;
    
    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.x, q = &c.y)
        {
            *p = 1;
            *q = 2;
        }
        Console.Write(c.x);
        Console.Write(c.y);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"12", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (int* V_0, //p
                pinned int& V_1,
                pinned int& V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""int C.x""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  conv.u
  IL_000e:  stloc.0
  IL_000f:  dup
  IL_0010:  ldflda     ""int C.y""
  IL_0015:  stloc.2
  IL_0016:  ldloc.2
  IL_0017:  conv.u
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.1
  IL_001a:  stind.i4
  IL_001b:  ldc.i4.2
  IL_001c:  stind.i4
  IL_001d:  ldc.i4.0
  IL_001e:  conv.u
  IL_001f:  stloc.1
  IL_0020:  ldc.i4.0
  IL_0021:  conv.u
  IL_0022:  stloc.2
  IL_0023:  dup
  IL_0024:  ldfld      ""int C.x""
  IL_0029:  call       ""void System.Console.Write(int)""
  IL_002e:  ldfld      ""int C.y""
  IL_0033:  call       ""void System.Console.Write(int)""
  IL_0038:  ret
}
");
        }

        [WorkItem(22306, "https://github.com/dotnet/roslyn/issues/22306")]
        [Fact]
        public void FixedStatementMultipleMethods()
        {
            var text = @"
using System;

unsafe class C
{
    int x;
    readonly int y;
    
    ref int X()=>ref x;
    ref readonly int this[int i]=>ref y;

    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.X(), q = &c[3])
        {
            *p = 1;
            *q = 2;
        }
        Console.Write(c.x);
        Console.Write(c.y);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"12", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       58 (0x3a)
  .maxstack  4
  .locals init (int* V_0, //p
                pinned int& V_1,
                pinned int& V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""ref int C.X()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  conv.u
  IL_000e:  stloc.0
  IL_000f:  dup
  IL_0010:  ldc.i4.3
  IL_0011:  callvirt   ""ref readonly int C.this[int].get""
  IL_0016:  stloc.2
  IL_0017:  ldloc.2
  IL_0018:  conv.u
  IL_0019:  ldloc.0
  IL_001a:  ldc.i4.1
  IL_001b:  stind.i4
  IL_001c:  ldc.i4.2
  IL_001d:  stind.i4
  IL_001e:  ldc.i4.0
  IL_001f:  conv.u
  IL_0020:  stloc.1
  IL_0021:  ldc.i4.0
  IL_0022:  conv.u
  IL_0023:  stloc.2
  IL_0024:  dup
  IL_0025:  ldfld      ""int C.x""
  IL_002a:  call       ""void System.Console.Write(int)""
  IL_002f:  ldfld      ""int C.y""
  IL_0034:  call       ""void System.Console.Write(int)""
  IL_0039:  ret
}
");
        }

        [WorkItem(546866, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546866")]
        [Fact]
        public void FixedStatementProperty()
        {
            var text =
@"class C
{
    string P { get { return null; } }
    char[] Q { get { return null; } }
    unsafe static void M(C c)
    {
        fixed (char* o = c.P)
        {
        }
        fixed (char* o = c.Q)
        {
        }
    }
}";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);
            compVerifier.VerifyIL("C.M(C)",
@"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (char* V_0, //o
                pinned string V_1,
                char* V_2, //o
                pinned char[] V_3)
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""string C.P.get""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  conv.u
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  brfalse.s  IL_0015
  IL_000d:  ldloc.0
  IL_000e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0013:  add
  IL_0014:  stloc.0
  IL_0015:  ldnull
  IL_0016:  stloc.1
  IL_0017:  ldarg.0
  IL_0018:  callvirt   ""char[] C.Q.get""
  IL_001d:  dup
  IL_001e:  stloc.3
  IL_001f:  brfalse.s  IL_0026
  IL_0021:  ldloc.3
  IL_0022:  ldlen
  IL_0023:  conv.i4
  IL_0024:  brtrue.s   IL_002b
  IL_0026:  ldc.i4.0
  IL_0027:  conv.u
  IL_0028:  stloc.2
  IL_0029:  br.s       IL_0034
  IL_002b:  ldloc.3
  IL_002c:  ldc.i4.0
  IL_002d:  ldelema    ""char""
  IL_0032:  conv.u
  IL_0033:  stloc.2
  IL_0034:  ldnull
  IL_0035:  stloc.3
  IL_0036:  ret
}
");
        }

        [Fact]
        public void FixedStatementMultipleOptimized()
        {
            var text = @"
using System;

unsafe class C
{
    int x;
    int y;
    
    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.x, q = &c.y)
        {
            *p = 1;
            *q = 2;
        }
        Console.Write(c.x);
        Console.Write(c.y);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"12", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (int* V_0, //p
                pinned int& V_1,
                pinned int& V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""int C.x""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  conv.u
  IL_000e:  stloc.0
  IL_000f:  dup
  IL_0010:  ldflda     ""int C.y""
  IL_0015:  stloc.2
  IL_0016:  ldloc.2
  IL_0017:  conv.u
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.1
  IL_001a:  stind.i4
  IL_001b:  ldc.i4.2
  IL_001c:  stind.i4
  IL_001d:  ldc.i4.0
  IL_001e:  conv.u
  IL_001f:  stloc.1
  IL_0020:  ldc.i4.0
  IL_0021:  conv.u
  IL_0022:  stloc.2
  IL_0023:  dup
  IL_0024:  ldfld      ""int C.x""
  IL_0029:  call       ""void System.Console.Write(int)""
  IL_002e:  ldfld      ""int C.y""
  IL_0033:  call       ""void System.Console.Write(int)""
  IL_0038:  ret
}
");
        }

        [Fact]
        public void FixedStatementReferenceParameter()
        {
            var text = @"
using System;

class C
{
    static void Main()
    {
        char ch;
        M(out ch);
        Console.WriteLine(ch);
    }

    unsafe static void M(out char ch)
    {
        fixed (char* p = &ch)
        {
            *p = 'a';
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"a", verify: Verification.Fails);

            compVerifier.VerifyIL("C.M", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  .locals init (pinned char& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  conv.u
  IL_0004:  ldc.i4.s   97
  IL_0006:  stind.i2
  IL_0007:  ldc.i4.0
  IL_0008:  conv.u
  IL_0009:  stloc.0
  IL_000a:  ret
}
");
        }

        [Fact]
        public void FixedStatementReferenceParameterDebug()
        {
            var text = @"
using System;

class C
{
    static void Main()
    {
        char ch;
        M(out ch);
        Console.WriteLine(ch);
    }

    unsafe static void M(out char ch)
    {
        fixed (char* p = &ch)
        {
            *p = 'a';
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeDebugExe, expectedOutput: @"a", verify: Verification.Fails);

            compVerifier.VerifyIL("C.M", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned char& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloc.1
  IL_0004:  conv.u
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   97
  IL_000a:  stind.i2
  IL_000b:  nop
  IL_000c:  ldc.i4.0
  IL_000d:  conv.u
  IL_000e:  stloc.1
  IL_000f:  ret
}
");
        }

        [Fact]
        public void FixedStatementStringLiteral()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        fixed (char* p = ""hello"")
        {
            Console.WriteLine(*p);
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeDebugExe, expectedOutput: @"h", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
 -IL_0000:  nop
  IL_0001:  ldstr      ""hello""
  IL_0006:  stloc.1
 -IL_0007:  ldloc.1
  IL_0008:  conv.u
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  brfalse.s  IL_0015
  IL_000d:  ldloc.0
  IL_000e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0013:  add
  IL_0014:  stloc.0
 -IL_0015:  nop
 -IL_0016:  ldloc.0
  IL_0017:  ldind.u2
  IL_0018:  call       ""void System.Console.WriteLine(char)""
  IL_001d:  nop
 -IL_001e:  nop
 ~IL_001f:  ldnull
  IL_0020:  stloc.1
 -IL_0021:  ret
}
", sequencePoints: "C.Main");
        }

        [Fact]
        public void FixedStatementStringVariable()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        string s = ""hello"";
        fixed (char* p = s)
        {
            Console.Write(*p);
        }

        s = null;
        fixed (char* p = s)
        {
            Console.Write(p == null);
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeDebugExe, expectedOutput: @"hTrue", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (string V_0, //s
                char* V_1, //p
                pinned string V_2,
                char* V_3, //p
                pinned string V_4)
 -IL_0000:  nop
 -IL_0001:  ldstr      ""hello""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.2
 -IL_0009:  ldloc.2
  IL_000a:  conv.u
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  brfalse.s  IL_0017
  IL_000f:  ldloc.1
  IL_0010:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0015:  add
  IL_0016:  stloc.1
 -IL_0017:  nop
 -IL_0018:  ldloc.1
  IL_0019:  ldind.u2
  IL_001a:  call       ""void System.Console.Write(char)""
  IL_001f:  nop
 -IL_0020:  nop
 ~IL_0021:  ldnull
  IL_0022:  stloc.2
 -IL_0023:  ldnull
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  stloc.s    V_4
 -IL_0028:  ldloc.s    V_4
  IL_002a:  conv.u
  IL_002b:  stloc.3
  IL_002c:  ldloc.3
  IL_002d:  brfalse.s  IL_0037
  IL_002f:  ldloc.3
  IL_0030:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0035:  add
  IL_0036:  stloc.3
 -IL_0037:  nop
 -IL_0038:  ldloc.3
  IL_0039:  ldc.i4.0
  IL_003a:  conv.u
  IL_003b:  ceq
  IL_003d:  call       ""void System.Console.Write(bool)""
  IL_0042:  nop
 -IL_0043:  nop
 ~IL_0044:  ldnull
  IL_0045:  stloc.s    V_4
 -IL_0047:  ret
}
", sequencePoints: "C.Main");
        }

        [Fact]
        public void FixedStatementStringVariableOptimized()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        string s = ""hello"";
        fixed (char* p = s)
        {
            Console.Write(*p);
        }

        s = null;
        fixed (char* p = s)
        {
            Console.Write(p == null);
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"hTrue", verify: Verification.Fails);

            // Null checks and branches are much simpler, but string temps are NOT optimized away.
            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1,
                char* V_2) //p
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ldind.u2
  IL_0016:  call       ""void System.Console.Write(char)""
  IL_001b:  ldnull
  IL_001c:  stloc.1
  IL_001d:  ldnull
  IL_001e:  stloc.1
  IL_001f:  ldloc.1
  IL_0020:  conv.u
  IL_0021:  stloc.2
  IL_0022:  ldloc.2
  IL_0023:  brfalse.s  IL_002d
  IL_0025:  ldloc.2
  IL_0026:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_002b:  add
  IL_002c:  stloc.2
  IL_002d:  ldloc.2
  IL_002e:  ldc.i4.0
  IL_002f:  conv.u
  IL_0030:  ceq
  IL_0032:  call       ""void System.Console.Write(bool)""
  IL_0037:  ldnull
  IL_0038:  stloc.1
  IL_0039:  ret
}
");
        }

        [Fact]
        public void FixedStatementOneDimensionalArray()
        {
            var text = @"
using System;

unsafe class C
{
    int[] a = new int[1];

    static void Main()
    {
        C c = new C();
        Console.Write(c.a[0]);
        fixed (int* p = c.a)
        {
            *p = 1;
        }
        Console.Write(c.a[0]);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"01", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  .locals init (int* V_0, //p
                pinned int[] V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""int[] C.a""
  IL_000b:  ldc.i4.0
  IL_000c:  ldelem.i4
  IL_000d:  call       ""void System.Console.Write(int)""
  IL_0012:  dup
  IL_0013:  ldfld      ""int[] C.a""
  IL_0018:  dup
  IL_0019:  stloc.1
  IL_001a:  brfalse.s  IL_0021
  IL_001c:  ldloc.1
  IL_001d:  ldlen
  IL_001e:  conv.i4
  IL_001f:  brtrue.s   IL_0026
  IL_0021:  ldc.i4.0
  IL_0022:  conv.u
  IL_0023:  stloc.0
  IL_0024:  br.s       IL_002f
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.0
  IL_0028:  ldelema    ""int""
  IL_002d:  conv.u
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.1
  IL_0031:  stind.i4
  IL_0032:  ldnull
  IL_0033:  stloc.1
  IL_0034:  ldfld      ""int[] C.a""
  IL_0039:  ldc.i4.0
  IL_003a:  ldelem.i4
  IL_003b:  call       ""void System.Console.Write(int)""
  IL_0040:  ret
}
");
        }

        [Fact]
        public void FixedStatementOneDimensionalArrayOptimized()
        {
            var text = @"
using System;

unsafe class C
{
    int[] a = new int[1];

    static void Main()
    {
        C c = new C();
        Console.Write(c.a[0]);
        fixed (int* p = c.a)
        {
            *p = 1;
        }
        Console.Write(c.a[0]);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"01", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  .locals init (int* V_0, //p
                pinned int[] V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""int[] C.a""
  IL_000b:  ldc.i4.0
  IL_000c:  ldelem.i4
  IL_000d:  call       ""void System.Console.Write(int)""
  IL_0012:  dup
  IL_0013:  ldfld      ""int[] C.a""
  IL_0018:  dup
  IL_0019:  stloc.1
  IL_001a:  brfalse.s  IL_0021
  IL_001c:  ldloc.1
  IL_001d:  ldlen
  IL_001e:  conv.i4
  IL_001f:  brtrue.s   IL_0026
  IL_0021:  ldc.i4.0
  IL_0022:  conv.u
  IL_0023:  stloc.0
  IL_0024:  br.s       IL_002f
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.0
  IL_0028:  ldelema    ""int""
  IL_002d:  conv.u
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.1
  IL_0031:  stind.i4
  IL_0032:  ldnull
  IL_0033:  stloc.1
  IL_0034:  ldfld      ""int[] C.a""
  IL_0039:  ldc.i4.0
  IL_003a:  ldelem.i4
  IL_003b:  call       ""void System.Console.Write(int)""
  IL_0040:  ret
}
");
        }

        [Fact]
        public void FixedStatementMultiDimensionalArrayOptimized()
        {
            var text = @"
using System;

unsafe class C
{
    int[,] a = new int[1,1];

    static void Main()
    {
        C c = new C();
        Console.Write(c.a[0, 0]);
        fixed (int* p = c.a)
        {
            *p = 1;
        }
        Console.Write(c.a[0, 0]);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"01", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (int* V_0, //p
                pinned int[,] V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""int[,] C.a""
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.0
  IL_000d:  call       ""int[*,*].Get""
  IL_0012:  call       ""void System.Console.Write(int)""
  IL_0017:  dup
  IL_0018:  ldfld      ""int[,] C.a""
  IL_001d:  dup
  IL_001e:  stloc.1
  IL_001f:  brfalse.s  IL_0029
  IL_0021:  ldloc.1
  IL_0022:  callvirt   ""int System.Array.Length.get""
  IL_0027:  brtrue.s   IL_002e
  IL_0029:  ldc.i4.0
  IL_002a:  conv.u
  IL_002b:  stloc.0
  IL_002c:  br.s       IL_0038
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldc.i4.0
  IL_0031:  call       ""int[*,*].Address""
  IL_0036:  conv.u
  IL_0037:  stloc.0
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.1
  IL_003a:  stind.i4
  IL_003b:  ldnull
  IL_003c:  stloc.1
  IL_003d:  ldfld      ""int[,] C.a""
  IL_0042:  ldc.i4.0
  IL_0043:  ldc.i4.0
  IL_0044:  call       ""int[*,*].Get""
  IL_0049:  call       ""void System.Console.Write(int)""
  IL_004e:  ret
}
");
        }

        [Fact]
        public void FixedStatementMixed()
        {
            var text = @"
using System;

unsafe class C
{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {
        C c = new C();
        fixed (char* p = &c.c, q = c.a, r = ""hello"")
        {
            Console.Write((int)*p);
            Console.Write((int)*q);
            Console.Write((int)*r);
        }
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"970104", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       99 (0x63)
  .maxstack  2
  .locals init (char* V_0, //p
                char* V_1, //q
                char* V_2, //r
                pinned char& V_3,
                pinned char[] V_4,
                pinned string V_5)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""char C.c""
  IL_000b:  stloc.3
  IL_000c:  ldloc.3
  IL_000d:  conv.u
  IL_000e:  stloc.0
  IL_000f:  ldfld      ""char[] C.a""
  IL_0014:  dup
  IL_0015:  stloc.s    V_4
  IL_0017:  brfalse.s  IL_001f
  IL_0019:  ldloc.s    V_4
  IL_001b:  ldlen
  IL_001c:  conv.i4
  IL_001d:  brtrue.s   IL_0024
  IL_001f:  ldc.i4.0
  IL_0020:  conv.u
  IL_0021:  stloc.1
  IL_0022:  br.s       IL_002e
  IL_0024:  ldloc.s    V_4
  IL_0026:  ldc.i4.0
  IL_0027:  ldelema    ""char""
  IL_002c:  conv.u
  IL_002d:  stloc.1
  IL_002e:  ldstr      ""hello""
  IL_0033:  stloc.s    V_5
  IL_0035:  ldloc.s    V_5
  IL_0037:  conv.u
  IL_0038:  stloc.2
  IL_0039:  ldloc.2
  IL_003a:  brfalse.s  IL_0044
  IL_003c:  ldloc.2
  IL_003d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0042:  add
  IL_0043:  stloc.2
  IL_0044:  ldloc.0
  IL_0045:  ldind.u2
  IL_0046:  call       ""void System.Console.Write(int)""
  IL_004b:  ldloc.1
  IL_004c:  ldind.u2
  IL_004d:  call       ""void System.Console.Write(int)""
  IL_0052:  ldloc.2
  IL_0053:  ldind.u2
  IL_0054:  call       ""void System.Console.Write(int)""
  IL_0059:  ldc.i4.0
  IL_005a:  conv.u
  IL_005b:  stloc.3
  IL_005c:  ldnull
  IL_005d:  stloc.s    V_4
  IL_005f:  ldnull
  IL_0060:  stloc.s    V_5
  IL_0062:  ret
}
");
        }

        [Fact]
        public void FixedStatementInTryOfTryFinally()
        {
            var text = @"
unsafe class C
{
    static void nop() { }
    void Test()
    {
        try
        {
            fixed (char* p = ""hello"")
            {
            }
        }
        finally
        {
            nop();
        }
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    .try
    {
      IL_0000:  ldstr      ""hello""
      IL_0005:  stloc.1
      IL_0006:  ldloc.1
      IL_0007:  conv.u
      IL_0008:  stloc.0
      IL_0009:  ldloc.0
      IL_000a:  brfalse.s  IL_0014
      IL_000c:  ldloc.0
      IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
      IL_0012:  add
      IL_0013:  stloc.0
      IL_0014:  leave.s    IL_001f
    }
    finally
    {
      IL_0016:  ldnull
      IL_0017:  stloc.1
      IL_0018:  endfinally
    }
  }
  finally
  {
    IL_0019:  call       ""void C.nop()""
    IL_001e:  endfinally
  }
  IL_001f:  ret
}
");
        }

        [Fact]
        public void FixedStatementInTryOfTryCatch()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        try
        {
            fixed (char* p = ""hello"")
            {
            }
        }
        catch
        {
        }
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    .try
    {
      IL_0000:  ldstr      ""hello""
      IL_0005:  stloc.1
      IL_0006:  ldloc.1
      IL_0007:  conv.u
      IL_0008:  stloc.0
      IL_0009:  ldloc.0
      IL_000a:  brfalse.s  IL_0014
      IL_000c:  ldloc.0
      IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
      IL_0012:  add
      IL_0013:  stloc.0
      IL_0014:  leave.s    IL_0019
    }
    finally
    {
      IL_0016:  ldnull
      IL_0017:  stloc.1
      IL_0018:  endfinally
    }
    IL_0019:  leave.s    IL_001e
  }
  catch object
  {
    IL_001b:  pop
    IL_001c:  leave.s    IL_001e
  }
  IL_001e:  ret
}
");
        }

        [Fact]
        public void FixedStatementInFinally()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        try
        {
        }
        finally
        {
            fixed (char* p = ""hello"")
            {
            }
        }
    }
}
";
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  leave.s    IL_0019
  }
  finally
  {
    IL_0002:  ldstr      ""hello""
    IL_0007:  stloc.1
    IL_0008:  ldloc.1
    IL_0009:  conv.u
    IL_000a:  stloc.0
    IL_000b:  ldloc.0
    IL_000c:  brfalse.s  IL_0016
    IL_000e:  ldloc.0
    IL_000f:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0014:  add
    IL_0015:  stloc.0
    IL_0016:  ldnull
    IL_0017:  stloc.1
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
        }

        [Fact]
        public void FixedStatementInCatchOfTryCatch()
        {
            var text = @"
unsafe class C
{
    void nop() { }
    void Test()
    {
        try
        {
            nop();
        }
        catch
        {
            fixed (char* p = ""hello"")
            {
            }
        }
    }
}
";
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test",
@"{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  call       ""void C.nop()""
    IL_0006:  leave.s    IL_0021
  }
  catch object
  {
    IL_0008:  pop
    IL_0009:  ldstr      ""hello""
    IL_000e:  stloc.1
    IL_000f:  ldloc.1
    IL_0010:  conv.u
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  brfalse.s  IL_001d
    IL_0015:  ldloc.0
    IL_0016:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_001b:  add
    IL_001c:  stloc.0
    IL_001d:  ldnull
    IL_001e:  stloc.1
    IL_001f:  leave.s    IL_0021
  }
  IL_0021:  ret
}");
        }

        [Fact]
        public void FixedStatementInCatchOfTryCatchFinally()
        {
            var text = @"
unsafe class C
{
    static void nop() { }
    void Test()
    {
        try
        {
            nop();
        }
        catch
        {
            fixed (char* p = ""hello"")
            {
            }
        }
        finally
        {
        }
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  call       ""void C.nop()""
    IL_0005:  leave.s    IL_0023
  }
  catch object
  {
    IL_0007:  pop
    .try
    {
      IL_0008:  ldstr      ""hello""
      IL_000d:  stloc.1
      IL_000e:  ldloc.1
      IL_000f:  conv.u
      IL_0010:  stloc.0
      IL_0011:  ldloc.0
      IL_0012:  brfalse.s  IL_001c
      IL_0014:  ldloc.0
      IL_0015:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
      IL_001a:  add
      IL_001b:  stloc.0
      IL_001c:  leave.s    IL_0021
    }
    finally
    {
      IL_001e:  ldnull
      IL_001f:  stloc.1
      IL_0020:  endfinally
    }
    IL_0021:  leave.s    IL_0023
  }
  IL_0023:  ret
}
");
        }

        [Fact]
        public void FixedStatementInFixed_NoBranch()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* q = ""goodbye"")
        {
            fixed (char* p = ""hello"")
            {
            }
        }
    }
}
";
            // Neither inner nor outer has finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (char* V_0, //q
                pinned string V_1,
                char* V_2, //p
                pinned string V_3)
  IL_0000:  ldstr      ""goodbye""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldstr      ""hello""
  IL_0019:  stloc.3
  IL_001a:  ldloc.3
  IL_001b:  conv.u
  IL_001c:  stloc.2
  IL_001d:  ldloc.2
  IL_001e:  brfalse.s  IL_0028
  IL_0020:  ldloc.2
  IL_0021:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0026:  add
  IL_0027:  stloc.2
  IL_0028:  ldnull
  IL_0029:  stloc.3
  IL_002a:  ldnull
  IL_002b:  stloc.1
  IL_002c:  ret
}
");
        }

        [Fact]
        public void FixedStatementInFixed_InnerBranch()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* q = ""goodbye"")
        {
            fixed (char* p = ""hello"")
            {
                goto label;
            }
        }
      label: ;
    }
}
";
            // Inner and outer both have finally blocks.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (char* V_0, //q
                pinned string V_1,
                char* V_2, //p
                pinned string V_3)
  .try
  {
    IL_0000:  ldstr      ""goodbye""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  nop
    .try
    {
      IL_0015:  ldstr      ""hello""
      IL_001a:  stloc.3
      IL_001b:  ldloc.3
      IL_001c:  conv.u
      IL_001d:  stloc.2
      IL_001e:  ldloc.2
      IL_001f:  brfalse.s  IL_0029
      IL_0021:  ldloc.2
      IL_0022:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
      IL_0027:  add
      IL_0028:  stloc.2
      IL_0029:  leave.s    IL_0031
    }
    finally
    {
      IL_002b:  ldnull
      IL_002c:  stloc.3
      IL_002d:  endfinally
    }
  }
  finally
  {
    IL_002e:  ldnull
    IL_002f:  stloc.1
    IL_0030:  endfinally
  }
  IL_0031:  ret
}
");
        }

        [Fact]
        public void FixedStatementInFixed_OuterBranch()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* q = ""goodbye"")
        {
            fixed (char* p = ""hello"")
            {
            }
            goto label;
        }
      label: ;
    }
}
";
            // Outer has finally, inner does not.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (char* V_0, //q
                pinned string V_1,
                char* V_2, //p
                pinned string V_3)
  .try
  {
    IL_0000:  ldstr      ""goodbye""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  ldstr      ""hello""
    IL_0019:  stloc.3
    IL_001a:  ldloc.3
    IL_001b:  conv.u
    IL_001c:  stloc.2
    IL_001d:  ldloc.2
    IL_001e:  brfalse.s  IL_0028
    IL_0020:  ldloc.2
    IL_0021:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0026:  add
    IL_0027:  stloc.2
    IL_0028:  ldnull
    IL_0029:  stloc.3
    IL_002a:  leave.s    IL_002f
  }
  finally
  {
    IL_002c:  ldnull
    IL_002d:  stloc.1
    IL_002e:  endfinally
  }
  IL_002f:  ret
}
");
        }

        [Fact]
        public void FixedStatementInFixed_Nesting()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p1 = ""A"")
        {
            fixed (char* p2 = ""B"")
            {
                fixed (char* p3 = ""C"")
                {
                }
                fixed (char* p4 = ""D"")
                {
                }
            }
            fixed (char* p5 = ""E"")
            {
                fixed (char* p6 = ""F"")
                {
                }
                fixed (char* p7 = ""G"")
                {
                }
            }
        }
    }
}
";
            // This test checks two things:
            //   1) nothing blows up with triple-nesting, and
            //   2) none of the fixed statements has a try-finally.
            // CONSIDER: Shorter test that performs the same checks.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size      187 (0xbb)
  .maxstack  2
  .locals init (char* V_0, //p1
                pinned string V_1,
                char* V_2, //p2
                pinned string V_3,
                char* V_4, //p3
                pinned string V_5,
                char* V_6, //p4
                char* V_7, //p5
                char* V_8, //p6
                char* V_9) //p7
  IL_0000:  ldstr      ""A""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldstr      ""B""
  IL_0019:  stloc.3
  IL_001a:  ldloc.3
  IL_001b:  conv.u
  IL_001c:  stloc.2
  IL_001d:  ldloc.2
  IL_001e:  brfalse.s  IL_0028
  IL_0020:  ldloc.2
  IL_0021:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0026:  add
  IL_0027:  stloc.2
  IL_0028:  ldstr      ""C""
  IL_002d:  stloc.s    V_5
  IL_002f:  ldloc.s    V_5
  IL_0031:  conv.u
  IL_0032:  stloc.s    V_4
  IL_0034:  ldloc.s    V_4
  IL_0036:  brfalse.s  IL_0042
  IL_0038:  ldloc.s    V_4
  IL_003a:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_003f:  add
  IL_0040:  stloc.s    V_4
  IL_0042:  ldnull
  IL_0043:  stloc.s    V_5
  IL_0045:  ldstr      ""D""
  IL_004a:  stloc.s    V_5
  IL_004c:  ldloc.s    V_5
  IL_004e:  conv.u
  IL_004f:  stloc.s    V_6
  IL_0051:  ldloc.s    V_6
  IL_0053:  brfalse.s  IL_005f
  IL_0055:  ldloc.s    V_6
  IL_0057:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_005c:  add
  IL_005d:  stloc.s    V_6
  IL_005f:  ldnull
  IL_0060:  stloc.s    V_5
  IL_0062:  ldnull
  IL_0063:  stloc.3
  IL_0064:  ldstr      ""E""
  IL_0069:  stloc.3
  IL_006a:  ldloc.3
  IL_006b:  conv.u
  IL_006c:  stloc.s    V_7
  IL_006e:  ldloc.s    V_7
  IL_0070:  brfalse.s  IL_007c
  IL_0072:  ldloc.s    V_7
  IL_0074:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0079:  add
  IL_007a:  stloc.s    V_7
  IL_007c:  ldstr      ""F""
  IL_0081:  stloc.s    V_5
  IL_0083:  ldloc.s    V_5
  IL_0085:  conv.u
  IL_0086:  stloc.s    V_8
  IL_0088:  ldloc.s    V_8
  IL_008a:  brfalse.s  IL_0096
  IL_008c:  ldloc.s    V_8
  IL_008e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0093:  add
  IL_0094:  stloc.s    V_8
  IL_0096:  ldnull
  IL_0097:  stloc.s    V_5
  IL_0099:  ldstr      ""G""
  IL_009e:  stloc.s    V_5
  IL_00a0:  ldloc.s    V_5
  IL_00a2:  conv.u
  IL_00a3:  stloc.s    V_9
  IL_00a5:  ldloc.s    V_9
  IL_00a7:  brfalse.s  IL_00b3
  IL_00a9:  ldloc.s    V_9
  IL_00ab:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_00b0:  add
  IL_00b1:  stloc.s    V_9
  IL_00b3:  ldnull
  IL_00b4:  stloc.s    V_5
  IL_00b6:  ldnull
  IL_00b7:  stloc.3
  IL_00b8:  ldnull
  IL_00b9:  stloc.1
  IL_00ba:  ret
}
");
        }

        [Fact]
        public void FixedStatementInUsing()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        using (System.IDisposable d = null)
        {
            fixed (char* p = ""hello"")
            {
            }
        }
    }
}
";
            // CONSIDER: This is sort of silly since the using is optimized away.
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  ldstr      ""hello""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  leave.s    IL_0019
  }
  finally
  {
    IL_0016:  ldnull
    IL_0017:  stloc.1
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
        }

        [Fact]
        public void FixedStatementInLock()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        lock (this)
        {
            fixed (char* p = ""hello"")
            {
            }
        }
    }
}
";
            // Cleanup not in finally (matches dev11, but not clear why).
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (C V_0,
                bool V_1,
                char* V_2, //p
                pinned string V_3)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  .try
  {
    IL_0004:  ldloc.0
    IL_0005:  ldloca.s   V_1
    IL_0007:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_000c:  ldstr      ""hello""
    IL_0011:  stloc.3
    IL_0012:  ldloc.3
    IL_0013:  conv.u
    IL_0014:  stloc.2
    IL_0015:  ldloc.2
    IL_0016:  brfalse.s  IL_0020
    IL_0018:  ldloc.2
    IL_0019:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_001e:  add
    IL_001f:  stloc.2
    IL_0020:  ldnull
    IL_0021:  stloc.3
    IL_0022:  leave.s    IL_002e
  }
  finally
  {
    IL_0024:  ldloc.1
    IL_0025:  brfalse.s  IL_002d
    IL_0027:  ldloc.0
    IL_0028:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_002d:  endfinally
  }
  IL_002e:  ret
}
");
        }

        [Fact]
        public void FixedStatementInForEach_NoDispose()
        {
            var text = @"
unsafe class C
{
    void Test(int[] array)
    {
        foreach (int i in array)
        {
            fixed (char* p = ""hello"")
            {
            }
        }
    }
}
";
            // Cleanup in finally.
            // CONSIDER: dev11 is smarter and skips the try-finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (int[] V_0,
                int V_1,
                char* V_2, //p
                pinned string V_3)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  br.s       IL_0027
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  ldelem.i4
  IL_0009:  pop
  .try
  {
    IL_000a:  ldstr      ""hello""
    IL_000f:  stloc.3
    IL_0010:  ldloc.3
    IL_0011:  conv.u
    IL_0012:  stloc.2
    IL_0013:  ldloc.2
    IL_0014:  brfalse.s  IL_001e
    IL_0016:  ldloc.2
    IL_0017:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_001c:  add
    IL_001d:  stloc.2
    IL_001e:  leave.s    IL_0023
  }
  finally
  {
    IL_0020:  ldnull
    IL_0021:  stloc.3
    IL_0022:  endfinally
  }
  IL_0023:  ldloc.1
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.1
  IL_0027:  ldloc.1
  IL_0028:  ldloc.0
  IL_0029:  ldlen
  IL_002a:  conv.i4
  IL_002b:  blt.s      IL_0006
  IL_002d:  ret
}
");
        }

        [Fact]
        public void FixedStatementInForEach_Dispose()
        {
            var text = @"
unsafe class C
{
    void Test(Enumerable e)
    {
        foreach (var x in e)
        {
            fixed (char* p = ""hello"")
            {
            }
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator : System.IDisposable
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    void System.IDisposable.Dispose() { }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (Enumerator V_0,
                char* V_1, //p
                pinned string V_2)
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""Enumerator Enumerable.GetEnumerator()""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  br.s       IL_0029
    IL_0009:  ldloc.0
    IL_000a:  callvirt   ""int Enumerator.Current.get""
    IL_000f:  pop
    .try
    {
      IL_0010:  ldstr      ""hello""
      IL_0015:  stloc.2
      IL_0016:  ldloc.2
      IL_0017:  conv.u
      IL_0018:  stloc.1
      IL_0019:  ldloc.1
      IL_001a:  brfalse.s  IL_0024
      IL_001c:  ldloc.1
      IL_001d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
      IL_0022:  add
      IL_0023:  stloc.1
      IL_0024:  leave.s    IL_0029
    }
    finally
    {
      IL_0026:  ldnull
      IL_0027:  stloc.2
      IL_0028:  endfinally
    }
    IL_0029:  ldloc.0
    IL_002a:  callvirt   ""bool Enumerator.MoveNext()""
    IL_002f:  brtrue.s   IL_0009
    IL_0031:  leave.s    IL_003d
  }
  finally
  {
    IL_0033:  ldloc.0
    IL_0034:  brfalse.s  IL_003c
    IL_0036:  ldloc.0
    IL_0037:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003c:  endfinally
  }
  IL_003d:  ret
}
");
        }

        [Fact]
        public void FixedStatementInLambda1()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        System.Action a = () =>
        {
            try
            {
                fixed (char* p = ""hello"")
                {
                }
            }
            finally
            {
            }
        };
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.<>c.<Test>b__0_0()", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  ldstr      ""hello""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  leave.s    IL_0019
  }
  finally
  {
    IL_0016:  ldnull
    IL_0017:  stloc.1
    IL_0018:  endfinally
  }
  IL_0019:  ret
}");
        }

        [Fact]
        public void FixedStatementInLambda2()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        try
        {
            System.Action a = () =>
            {
                    fixed (char* p = ""hello"")
                    {
                    }
            };
        }
        finally
        {
        }
    }
}
";
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.<>c.<Test>b__0_0()", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldnull
  IL_0015:  stloc.1
  IL_0016:  ret
}
");
        }

        [Fact]
        public void FixedStatementInLambda3()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        System.Action a = () =>
        {
            fixed (char* p = ""hello"")
            {
                goto label;
            }
          label: ;
        };
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.<>c.<Test>b__0_0()", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  ldstr      ""hello""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  leave.s    IL_0019
  }
  finally
  {
    IL_0016:  ldnull
    IL_0017:  stloc.1
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
        }

        [Fact]
        public void FixedStatementInFieldInitializer1()
        {
            var text = @"
unsafe class C
{
    System.Action a = () =>
        {
            try
            {
                fixed (char* p = ""hello"")
                {
                }
            }
            finally
            {
            }
        };
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.<>c.<.ctor>b__1_0()", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  ldstr      ""hello""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  leave.s    IL_0019
  }
  finally
  {
    IL_0016:  ldnull
    IL_0017:  stloc.1
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
        }

        [Fact]
        public void FixedStatementInFieldInitializer2()
        {
            var text = @"
unsafe class C
{
    System.Action a = () =>
        {
            fixed (char* p = ""hello"")
            {
                goto label;
            }
            label: ;
        };
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.<>c.<.ctor>b__1_0()", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  ldstr      ""hello""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  leave.s    IL_0019
  }
  finally
  {
    IL_0016:  ldnull
    IL_0017:  stloc.1
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_LoopBreak()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        while(true)
        {
            fixed (char* p = ""hello"")
            {
                break;
            }
        }
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldstr      ""hello""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  conv.u
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  brfalse.s  IL_0015
    IL_000d:  ldloc.0
    IL_000e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0013:  add
    IL_0014:  stloc.0
    IL_0015:  leave.s    IL_001a
  }
  finally
  {
    IL_0017:  ldnull
    IL_0018:  stloc.1
    IL_0019:  endfinally
  }
  IL_001a:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_LoopContinue()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        while(true)
        {
            fixed (char* p = ""hello"")
            {
                continue;
            }
        }
    }
}
";
            // Cleanup in finally.
            // CONSIDER: dev11 doesn't have a finally here, but that seems incorrect.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldstr      ""hello""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  conv.u
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  brfalse.s  IL_0015
    IL_000d:  ldloc.0
    IL_000e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0013:  add
    IL_0014:  stloc.0
    IL_0015:  leave.s    IL_0000
  }
  finally
  {
    IL_0017:  ldnull
    IL_0018:  stloc.1
    IL_0019:  endfinally
  }
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_SwitchBreak()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        switch (1)
        {
            case 1:
                fixed (char* p = ""hello"")
                {
                    break;
                }
        }
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldstr      ""hello""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  conv.u
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  brfalse.s  IL_0015
    IL_000d:  ldloc.0
    IL_000e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0013:  add
    IL_0014:  stloc.0
    IL_0015:  leave.s    IL_001a
  }
  finally
  {
    IL_0017:  ldnull
    IL_0018:  stloc.1
    IL_0019:  endfinally
  }
  IL_001a:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_SwitchGoto()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        switch (1)
        {
            case 1:
                fixed (char* p = ""hello"")
                {
                    goto case 1;
                }
        }
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldstr      ""hello""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  conv.u
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  brfalse.s  IL_0015
    IL_000d:  ldloc.0
    IL_000e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0013:  add
    IL_0014:  stloc.0
    IL_0015:  leave.s    IL_0000
  }
  finally
  {
    IL_0017:  ldnull
    IL_0018:  stloc.1
    IL_0019:  endfinally
  }
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_BackwardGoto()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
      label:
        fixed (char* p = ""hello"")
        {
            goto label;
        }
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldstr      ""hello""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  conv.u
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  brfalse.s  IL_0015
    IL_000d:  ldloc.0
    IL_000e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0013:  add
    IL_0014:  stloc.0
    IL_0015:  leave.s    IL_0000
  }
  finally
  {
    IL_0017:  ldnull
    IL_0018:  stloc.1
    IL_0019:  endfinally
  }
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_ForwardGoto()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p = ""hello"")
        {
            goto label;
        }
      label: ;
    }
}
";
            // Cleanup in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  .try
  {
    IL_0000:  ldstr      ""hello""
    IL_0005:  stloc.1
    IL_0006:  ldloc.1
    IL_0007:  conv.u
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_0014
    IL_000c:  ldloc.0
    IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
    IL_0012:  add
    IL_0013:  stloc.0
    IL_0014:  leave.s    IL_0019
  }
  finally
  {
    IL_0016:  ldnull
    IL_0017:  stloc.1
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_Throw()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p = ""hello"")
        {
            throw null;
        }
    }
}
";
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldnull
  IL_0015:  throw
}
");
        }

        [Fact]
        public void FixedStatementWithBranchOut_Return()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p = ""hello"")
        {
            return;
        }
    }
}
";
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithNoBranchOut_Loop()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p = ""hello"")
        {
            for (int i = 0; i < 10; i++)
            {
            }
        }
    }
}
";
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1,
                int V_2) //i
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  stloc.2
  IL_0016:  br.s       IL_001c
  IL_0018:  ldloc.2
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  stloc.2
  IL_001c:  ldloc.2
  IL_001d:  ldc.i4.s   10
  IL_001f:  blt.s      IL_0018
  IL_0021:  ldnull
  IL_0022:  stloc.1
  IL_0023:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithNoBranchOut_InternalGoto()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p = ""hello"")
        {
            goto label;
          label: ;
        }
    }
}
";
            // NOTE: Dev11 uses a finally here, but it's unnecessary.
            // From GotoChecker::VisitGOTO:
            //      We have an unrealized goto, so we do not know whether it
            //      branches out or not.  We should be conservative and assume that
            //      it does.
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldnull
  IL_0015:  stloc.1
  IL_0016:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithNoBranchOut_Switch()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p = ""hello"")
        {
            switch(*p)
            {
                case 'a':
                    Test();
                    goto case 'b';
                case 'b':
                    Test();
                    goto case 'c';
                case 'c':
                    Test();
                    goto case 'd';
                case 'd':
                    Test();
                    goto case 'e';
                case 'e':
                    Test();
                    goto case 'f';
                case 'f':
                    Test();
                    goto default;
                default:
                    Test();
                    break;
            }
        }
    }
}
";
            // Cleanup not in finally.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size      103 (0x67)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1,
                char V_2)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ldind.u2
  IL_0016:  stloc.2
  IL_0017:  ldloc.2
  IL_0018:  ldc.i4.s   97
  IL_001a:  sub
  IL_001b:  switch    (
        IL_003a,
        IL_0040,
        IL_0046,
        IL_004c,
        IL_0052,
        IL_0058)
  IL_0038:  br.s       IL_005e
  IL_003a:  ldarg.0
  IL_003b:  call       ""void C.Test()""
  IL_0040:  ldarg.0
  IL_0041:  call       ""void C.Test()""
  IL_0046:  ldarg.0
  IL_0047:  call       ""void C.Test()""
  IL_004c:  ldarg.0
  IL_004d:  call       ""void C.Test()""
  IL_0052:  ldarg.0
  IL_0053:  call       ""void C.Test()""
  IL_0058:  ldarg.0
  IL_0059:  call       ""void C.Test()""
  IL_005e:  ldarg.0
  IL_005f:  call       ""void C.Test()""
  IL_0064:  ldnull
  IL_0065:  stloc.1
  IL_0066:  ret
}
");
        }

        [Fact]
        public void FixedStatementWithParenthesizedStringExpression()
        {
            var text = @"
unsafe class C
{
    void Test()
    {
        fixed (char* p = ((""hello"")))
        {
        }
    }
}";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).
                VerifyIL("C.Test", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldnull
  IL_0015:  stloc.1
  IL_0016:  ret
}
");
        }

        #endregion Fixed statement tests

        #region Custom fixed statement tests

        [Fact]
        public void SimpleCaseOfCustomFixed()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

class Fixable
{
    public ref int GetPinnableReference()
    {
        return ref (new int[]{1,2,3})[0];
    }
}

static class FixableExt
{
    public static ref int GetPinnableReference(this Fixable self)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"2", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (pinned int& V_0)
  IL_0000:  newobj     ""Fixable..ctor()""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000d
  IL_0008:  pop
  IL_0009:  ldc.i4.0
  IL_000a:  conv.u
  IL_000b:  br.s       IL_0015
  IL_000d:  call       ""ref int Fixable.GetPinnableReference()""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  conv.u
  IL_0015:  ldc.i4.4
  IL_0016:  add
  IL_0017:  ldind.i4
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ldc.i4.0
  IL_001e:  conv.u
  IL_001f:  stloc.0
  IL_0020:  ret
}
");
        }

        [Fact]
        public void SimpleCaseOfCustomFixedExt()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

class Fixable
{
    public ref int GetPinnableReference<T>() => throw null;
}

static class FixableExt
{
    public static ref int GetPinnableReference<T>(this T self)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"2", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (pinned int& V_0)
  IL_0000:  newobj     ""Fixable..ctor()""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000d
  IL_0008:  pop
  IL_0009:  ldc.i4.0
  IL_000a:  conv.u
  IL_000b:  br.s       IL_0015
  IL_000d:  call       ""ref int FixableExt.GetPinnableReference<Fixable>(Fixable)""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  conv.u
  IL_0015:  ldc.i4.4
  IL_0016:  add
  IL_0017:  ldind.i4
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ldc.i4.0
  IL_001e:  conv.u
  IL_001f:  stloc.0
  IL_0020:  ret
}
");
        }

        [Fact]
        public void SimpleCaseOfCustomFixed_oldVersion()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }

    class Fixable
    {
        public ref int GetPinnableReference()
        {
            return ref (new int[]{1,2,3})[0];
        }
    }

}
";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular7_2);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS8320: Feature 'extensible fixed statement' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         fixed (int* p = new Fixable())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "new Fixable()").WithArguments("extensible fixed statement", "7.3").WithLocation(6, 25)
                );
        }

        [Fact]
        public void SimpleCaseOfCustomFixedNull()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = (Fixable)null)
        {
            System.Console.WriteLine((int)p);
        }
    }

    class Fixable
    {
        public ref int GetPinnableReference()
        {
            return ref (new int[]{1,2,3})[0];
        }
    }

}";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"0", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (pinned int& V_0)
  IL_0000:  ldnull
  IL_0001:  brtrue.s   IL_0007
  IL_0003:  ldc.i4.0
  IL_0004:  conv.u
  IL_0005:  br.s       IL_0010
  IL_0007:  ldnull
  IL_0008:  call       ""ref int C.Fixable.GetPinnableReference()""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  conv.u
  IL_0010:  conv.i4
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  ldc.i4.0
  IL_0017:  conv.u
  IL_0018:  stloc.0
  IL_0019:  ret
}
");
        }

        [Fact]
        public void SimpleCaseOfCustomFixedStruct()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }

    struct Fixable
    {
        public ref int GetPinnableReference()
        {
            return ref (new int[]{1,2,3})[0];
        }
    }

}";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"2", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (pinned int& V_0,
                C.Fixable V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""C.Fixable""
  IL_0009:  call       ""ref int C.Fixable.GetPinnableReference()""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  conv.u
  IL_0011:  ldc.i4.4
  IL_0012:  add
  IL_0013:  ldind.i4
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldc.i4.0
  IL_001a:  conv.u
  IL_001b:  stloc.0
  IL_001c:  ret
}
");
        }

        [Fact]
        public void CustomFixedStructNullable()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        Fixable? f = new Fixable();

        fixed (int* p = f)
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

public struct Fixable
{
    public ref int GetPinnableReference()
    {
        return ref (new int[]{1,2,3})[0];
    }
}

public static class FixableExt
{
    public static ref int GetPinnableReference(this Fixable? f)
    {
        return ref f.Value.GetPinnableReference();
    }
}

";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"2", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Fixable V_0,
                pinned int& V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Fixable""
  IL_0008:  ldloc.0
  IL_0009:  newobj     ""Fixable?..ctor(Fixable)""
  IL_000e:  call       ""ref int FixableExt.GetPinnableReference(Fixable?)""
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  conv.u
  IL_0016:  ldc.i4.4
  IL_0017:  add
  IL_0018:  ldind.i4
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldc.i4.0
  IL_001f:  conv.u
  IL_0020:  stloc.1
  IL_0021:  ret
}
");
        }

        [Fact]
        public void CustomFixedStructNullableErr()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        Fixable? f = new Fixable();

        fixed (int* p = f)
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

public struct Fixable
{
    public ref int GetPinnableReference()
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (8,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = f)
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "f").WithLocation(8, 25)
                );
        }

        [Fact]
        public void CustomFixedErrAmbiguous()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }

        var f = new Fixable(1);
        fixed (int* p = f)
        {
            System.Console.Write(p[2]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    public static ref readonly int GetPinnableReference(in this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

public static class FixableExt1
{
    public static ref readonly int GetPinnableReference(in this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}
";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS0121: The call is ambiguous between the following methods or properties: 'FixableExt.GetPinnableReference(in Fixable)' and 'FixableExt1.GetPinnableReference(in Fixable)'
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_AmbigCall, "new Fixable(1)").WithArguments("FixableExt.GetPinnableReference(in Fixable)", "FixableExt1.GetPinnableReference(in Fixable)").WithLocation(6, 25),
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25),
                // (12,25): error CS0121: The call is ambiguous between the following methods or properties: 'FixableExt.GetPinnableReference(in Fixable)' and 'FixableExt1.GetPinnableReference(in Fixable)'
                //         fixed (int* p = f)
                Diagnostic(ErrorCode.ERR_AmbigCall, "f").WithArguments("FixableExt.GetPinnableReference(in Fixable)", "FixableExt1.GetPinnableReference(in Fixable)").WithLocation(12, 25),
                // (12,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = f)
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "f").WithLocation(12, 25)
                );
        }

        [Fact]
        public void CustomFixedErrDynamic()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = (dynamic)(new Fixable(1)))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    public static ref readonly int GetPinnableReference(in this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}
";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = (dynamic)(new Fixable(1)))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "(dynamic)(new Fixable(1))").WithLocation(6, 25)
                );
        }

        [Fact]
        public void CustomFixedErrBad()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = (HocusPocus)(new Fixable(1)))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    public static ref readonly int GetPinnableReference(in this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}
";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,26): error CS0246: The type or namespace name 'HocusPocus' could not be found (are you missing a using directive or an assembly reference?)
                //         fixed (int* p = (HocusPocus)(new Fixable(1)))
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "HocusPocus").WithArguments("HocusPocus").WithLocation(6, 26)
                );
        }

        [Fact]
        public void SimpleCaseOfCustomFixedGeneric()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        Test(42);
        Test((object)null);
    }

    public static void Test<T>(T arg)
    {
        fixed (int* p = arg)
        {
            System.Console.Write(p == null? 0: p[1]);
        }
    }
}

static class FixAllExt
{
    public static ref int GetPinnableReference<T>(this T dummy)
    {
        return ref (new int[]{1,2,3})[0];
    }
}
";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"20", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Test<T>(T)", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (int* V_0, //p
                pinned int& V_1)
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  brtrue.s   IL_000c
  IL_0008:  ldc.i4.0
  IL_0009:  conv.u
  IL_000a:  br.s       IL_001b
  IL_000c:  ldarga.s   V_0
  IL_000e:  ldobj      ""T""
  IL_0013:  call       ""ref int FixAllExt.GetPinnableReference<T>(T)""
  IL_0018:  stloc.1
  IL_0019:  ldloc.1
  IL_001a:  conv.u
  IL_001b:  stloc.0
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.0
  IL_001e:  conv.u
  IL_001f:  beq.s      IL_0027
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.4
  IL_0023:  add
  IL_0024:  ldind.i4
  IL_0025:  br.s       IL_0028
  IL_0027:  ldc.i4.0
  IL_0028:  call       ""void System.Console.Write(int)""
  IL_002d:  ldc.i4.0
  IL_002e:  conv.u
  IL_002f:  stloc.1
  IL_0030:  ret
}
");
        }

        [Fact]
        public void CustomFixedStructSideeffects()
        {
            var text = @"
    unsafe class C
    {
        public static void Main()
        {
            var b = new FixableStruct();
            Test(ref b);
            System.Console.WriteLine(b.x);
        }

        public static void Test(ref FixableStruct arg)
        {
            fixed (int* p = arg)
            {
                System.Console.Write(p[1]);
            }
        }
    }

    struct FixableStruct
    {
        public int x;

        public ref int GetPinnableReference()
        {
            x = 456;
            return ref (new int[] { 4, 5, 6 })[0];
        }
    }
";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"5456");

            compVerifier.VerifyIL("C.Test(ref FixableStruct)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (pinned int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int FixableStruct.GetPinnableReference()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  conv.u
  IL_0009:  ldc.i4.4
  IL_000a:  add
  IL_000b:  ldind.i4
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  ldc.i4.0
  IL_0012:  conv.u
  IL_0013:  stloc.0
  IL_0014:  ret
}
");
        }

        [Fact]
        public void CustomFixedClassSideeffects()
        {
            var text = @"
    using System;
    unsafe class C
    {
        public static void Main()
        {
            var b = new FixableClass();
            Test(ref b);
            System.Console.WriteLine(b.x);
        }

        public static void Test(ref FixableClass arg)
        {
            fixed (int* p = arg)
            {
                System.Console.Write(p[1]);
            }
        }
    }

    class FixableClass
    {
        public int x;

        [Obsolete]
        public ref int GetPinnableReference()
        {
            x = 456;
            return ref (new int[] { 4, 5, 6 })[0];
        }
    }
";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"5456");

            compVerifier.VerifyDiagnostics(
                // (14,29): warning CS0612: 'FixableClass.GetPinnableReference()' is obsolete
                //             fixed (int* p = arg)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "arg").WithArguments("FixableClass.GetPinnableReference()").WithLocation(14, 29)
                );

            // note that defensive copy is created
            compVerifier.VerifyIL("C.Test(ref FixableClass)", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (pinned int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_000a
  IL_0005:  pop
  IL_0006:  ldc.i4.0
  IL_0007:  conv.u
  IL_0008:  br.s       IL_0012
  IL_000a:  call       ""ref int FixableClass.GetPinnableReference()""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  conv.u
  IL_0012:  ldc.i4.4
  IL_0013:  add
  IL_0014:  ldind.i4
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ldc.i4.0
  IL_001b:  conv.u
  IL_001c:  stloc.0
  IL_001d:  ret
}
");
        }

        [Fact]
        public void CustomFixedGenericSideeffects()
        {
            var text = @"
    unsafe class C
    {
        public static void Main()
        {
            var a = new FixableClass();
            Test(ref a);
            System.Console.WriteLine(a.x);
            var b = new FixableStruct();
            Test(ref b);
            System.Console.WriteLine(b.x);
        }

        public static void Test<T>(ref T arg) where T: IFixable
        {
            fixed (int* p = arg)
            {
                System.Console.Write(p[1]);
            }
        }
    }

    interface IFixable
    {
        ref int GetPinnableReference();
    }

    class FixableClass : IFixable
    {

        public int x;

        public ref int GetPinnableReference()
        {
            x = 123;
            return ref (new int[] { 1, 2, 3 })[0];
        }
    }

    struct FixableStruct : IFixable
    {
        public int x;

        public ref int GetPinnableReference()
        {
            x = 456;
            return ref (new int[] { 4, 5, 6 })[0];
        }
    }
";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"2123
5456");

            compVerifier.VerifyIL("C.Test<T>(ref T)", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (pinned int& V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_1
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.1
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0026
  IL_0011:  ldobj      ""T""
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldloc.1
  IL_001a:  box        ""T""
  IL_001f:  brtrue.s   IL_0026
  IL_0021:  pop
  IL_0022:  ldc.i4.0
  IL_0023:  conv.u
  IL_0024:  br.s       IL_0034
  IL_0026:  constrained. ""T""
  IL_002c:  callvirt   ""ref int IFixable.GetPinnableReference()""
  IL_0031:  stloc.0
  IL_0032:  ldloc.0
  IL_0033:  conv.u
  IL_0034:  ldc.i4.4
  IL_0035:  add
  IL_0036:  ldind.i4
  IL_0037:  call       ""void System.Console.Write(int)""
  IL_003c:  ldc.i4.0
  IL_003d:  conv.u
  IL_003e:  stloc.0
  IL_003f:  ret
}
");
        }

        [Fact]
        public void CustomFixedGenericRefExtension()
        {
            var text = @"
    unsafe class C
    {
        public static void Main()
        {
            var b = new FixableStruct();
            Test(ref b);
            System.Console.WriteLine(b.x);
        }

        public static void Test<T>(ref T arg) where T: struct, IFixable
        {
            fixed (int* p = arg)
            {
                System.Console.Write(p[1]);
            }
        }
    }

    public interface IFixable
    {
        ref int GetPinnableReferenceImpl();
    }

    public struct FixableStruct : IFixable
    {
        public int x;

        public ref int GetPinnableReferenceImpl()
        {
            x = 456;
            return ref (new int[] { 4, 5, 6 })[0];
        }
    }

    public static class FixableExt
    {
        public static ref int GetPinnableReference<T>(ref this T f) where T: struct, IFixable
        {
            return ref f.GetPinnableReferenceImpl();
        }
    }
";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"5456");

            compVerifier.VerifyIL("C.Test<T>(ref T)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (pinned int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int FixableExt.GetPinnableReference<T>(ref T)""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  conv.u
  IL_0009:  ldc.i4.4
  IL_000a:  add
  IL_000b:  ldind.i4
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  ldc.i4.0
  IL_0012:  conv.u
  IL_0013:  stloc.0
  IL_0014:  ret
}
");
        }

        [Fact]
        public void CustomFixedStructInExtension()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }

        var f = new Fixable(1);
        fixed (int* p = f)
        {
            System.Console.Write(p[2]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    public static ref readonly int GetPinnableReference(in this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"23", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       61 (0x3d)
  .maxstack  3
  .locals init (Fixable V_0, //f
                pinned int& V_1,
                Fixable V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""Fixable..ctor(int)""
  IL_0006:  stloc.2
  IL_0007:  ldloca.s   V_2
  IL_0009:  call       ""ref readonly int FixableExt.GetPinnableReference(in Fixable)""
  IL_000e:  stloc.1
  IL_000f:  ldloc.1
  IL_0010:  conv.u
  IL_0011:  ldc.i4.4
  IL_0012:  add
  IL_0013:  ldind.i4
  IL_0014:  call       ""void System.Console.Write(int)""
  IL_0019:  ldc.i4.0
  IL_001a:  conv.u
  IL_001b:  stloc.1
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldc.i4.1
  IL_001f:  call       ""Fixable..ctor(int)""
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""ref readonly int FixableExt.GetPinnableReference(in Fixable)""
  IL_002b:  stloc.1
  IL_002c:  ldloc.1
  IL_002d:  conv.u
  IL_002e:  ldc.i4.2
  IL_002f:  conv.i
  IL_0030:  ldc.i4.4
  IL_0031:  mul
  IL_0032:  add
  IL_0033:  ldind.i4
  IL_0034:  call       ""void System.Console.Write(int)""
  IL_0039:  ldc.i4.0
  IL_003a:  conv.u
  IL_003b:  stloc.1
  IL_003c:  ret
}
");
        }

        [Fact]
        public void CustomFixedStructRefExtension()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        var f = new Fixable(1);
        fixed (int* p = f)
        {
            System.Console.Write(p[2]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    public static ref int GetPinnableReference(ref this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"3", verify: Verification.Fails);

            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (Fixable V_0, //f
                pinned int& V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""Fixable..ctor(int)""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""ref int FixableExt.GetPinnableReference(ref Fixable)""
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  conv.u
  IL_0012:  ldc.i4.2
  IL_0013:  conv.i
  IL_0014:  ldc.i4.4
  IL_0015:  mul
  IL_0016:  add
  IL_0017:  ldind.i4
  IL_0018:  call       ""void System.Console.Write(int)""
  IL_001d:  ldc.i4.0
  IL_001e:  conv.u
  IL_001f:  stloc.1
  IL_0020:  ret
}
");
        }

        [Fact]
        public void CustomFixedStructRefExtensionErr()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    public static ref int GetPinnableReference(ref this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS1510: A ref or out value must be an assignable variable
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new Fixable(1)").WithLocation(6, 25),
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr01()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    private static ref int GetPinnableReference(this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS8385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr01_oldVersion()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}
}

public static class FixableExt
{
    private static ref int GetPinnableReference(this Fixable f)
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular7_2);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr02()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}

    public static ref int GetPinnableReference()
    {
        return ref (new int[]{1,2,3})[0];
    }
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25),
                // (6,25): error CS0176: Member 'Fixable.GetPinnableReference()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "new Fixable(1)").WithArguments("Fixable.GetPinnableReference()").WithLocation(6, 25)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr03()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}

    public ref int GetPinnableReference => ref (new int[]{1,2,3})[0];
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS1955: Non-invocable member 'Fixable.GetPinnableReference' cannot be used like a method.
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "new Fixable(1)").WithArguments("Fixable.GetPinnableReference").WithLocation(6, 25),
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr04()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}

    public ref int GetPinnableReference<T>() => ref (new int[]{1,2,3})[0];
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS0411: The type arguments for method 'Fixable.GetPinnableReference<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new Fixable(1)").WithArguments("Fixable.GetPinnableReference<T>()").WithLocation(6, 25),
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr05_Obsolete()
        {
            var text = @"
using System;

unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}

    [Obsolete(""hi"", true)]
    public ref int GetPinnableReference() => ref (new int[]{1,2,3})[0];
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (8,25): error CS0619: 'Fixable.GetPinnableReference()' is obsolete: 'hi'
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Fixable(1)").WithArguments("Fixable.GetPinnableReference()", "hi").WithLocation(8, 25)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr06_UseSite()
        {
            var missing_cs = "public struct Missing { }";
            var missing = CreateCompilationWithMscorlib45(missing_cs, options: TestOptions.DebugDll, assemblyName: "missing");

            var lib_cs = @"
public struct Fixable
{
    public Fixable(int arg){}

    public ref Missing GetPinnableReference() => throw null;
}
";

            var lib = CreateCompilationWithMscorlib45(lib_cs, references: new[] { missing.EmitToImageReference() }, options: TestOptions.DebugDll);

            var source =
@"
unsafe class C
{
    public static void Main()
    {
        fixed (void* p = new Fixable(1))
        {
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: new[] { lib.EmitToImageReference() }, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         fixed (void* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new Fixable(1)").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 26),
                // (6,26): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (void* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 26)
                );
        }

        [Fact]
        public void CustomFixedStructVariousErr07_Optional()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable(1))
        {
            System.Console.Write(p[1]);
        }
    }
}

public struct Fixable
{
    public Fixable(int arg){}

    public ref int GetPinnableReference(int x = 0) => ref (new int[]{1,2,3})[0];
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): warning CS0280: 'Fixable' does not implement the 'fixed' pattern. 'Fixable.GetPinnableReference(int)' has the wrong signature.
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "new Fixable(1)").WithArguments("Fixable", "fixed", "Fixable.GetPinnableReference(int)").WithLocation(6, 25),
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable(1))
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable(1)").WithLocation(6, 25)
                );
        }

        [Fact]
        public void FixStringMissingAllHelpers()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (char* p = string.Empty)
        {
        }
    }
}

";

            var comp = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__get_OffsetToStringData);

            comp.VerifyEmitDiagnostics(
                // (6,26): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.get_OffsetToStringData'
                //         fixed (char* p = string.Empty)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "string.Empty").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "get_OffsetToStringData").WithLocation(6, 26)
                );
        }

        [Fact]
        public void FixStringArrayExtensionHelpersIgnored()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (char* p = ""A"")
        {
            *p = default;
        }

        fixed (char* p = new char[1])
        {
            *p = default;
        }
    }
}

public static class FixableExt
{
    public static ref char GetPinnableReference(this string self) => throw null;
    public static ref char GetPinnableReference(this char[] self) => throw null;
}
";

            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"");

            compVerifier.VerifyIL("C.Main()", @"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1,
                char* V_2, //p
                pinned char[] V_3)
  IL_0000:  ldstr      ""A""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  conv.u
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldloc.0
  IL_000d:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.0
  IL_0016:  stind.i2
  IL_0017:  ldnull
  IL_0018:  stloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  newarr     ""char""
  IL_001f:  dup
  IL_0020:  stloc.3
  IL_0021:  brfalse.s  IL_0028
  IL_0023:  ldloc.3
  IL_0024:  ldlen
  IL_0025:  conv.i4
  IL_0026:  brtrue.s   IL_002d
  IL_0028:  ldc.i4.0
  IL_0029:  conv.u
  IL_002a:  stloc.2
  IL_002b:  br.s       IL_0036
  IL_002d:  ldloc.3
  IL_002e:  ldc.i4.0
  IL_002f:  ldelema    ""char""
  IL_0034:  conv.u
  IL_0035:  stloc.2
  IL_0036:  ldloc.2
  IL_0037:  ldc.i4.0
  IL_0038:  stind.i2
  IL_0039:  ldnull
  IL_003a:  stloc.3
  IL_003b:  ret
}
");
        }

        [Fact]
        public void CustomFixedDelegateErr()
        {
            var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.Write(p[1]);
        }
    }
}

public delegate ref int ReturnsRef();

public struct Fixable
{
    public Fixable(int arg){}

    public ReturnsRef GetPinnableReference => null;
}

";

            var compVerifier = CreateCompilationWithMscorlib46(text, options: TestOptions.UnsafeReleaseExe);

            compVerifier.VerifyDiagnostics(
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = new Fixable())
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable()").WithLocation(6, 25)
                );
        }

        #endregion Custom fixed statement tests

        #region Pointer conversion tests

        [Fact]
        public void ConvertNullToPointer()
        {
            var template = @"
using System;

unsafe class C
{{
    static void Main()
    {{
        {0}
        {{
            char ch = 'a';
            char* p = &ch;
            Console.WriteLine(p == null);

            p = null;
            Console.WriteLine(p == null);
        }}
    }}
}}
";

            var expectedIL = @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (char V_0, //ch
      char* V_1) //p
  IL_0000:  ldc.i4.s   97
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.0
  IL_0009:  conv.u
  IL_000a:  ceq
  IL_000c:  call       ""void System.Console.WriteLine(bool)""
  IL_0011:  ldc.i4.0
  IL_0012:  conv.u
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.0
  IL_0016:  conv.u
  IL_0017:  ceq
  IL_0019:  call       ""void System.Console.WriteLine(bool)""
  IL_001e:  ret
}
";
            var expectedOutput = @"False
True";

            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL);
            CompileAndVerify(string.Format(template, "checked"), options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL);
        }

        [Fact]
        public void ConvertPointerToPointerOrVoid()
        {
            var template = @"
using System;

unsafe class C
{{
    static void Main()
    {{
        {0}
        {{
            char ch = 'a';
            char* c1 = &ch;
            void* v1 = c1;
            void* v2 = (void**)v1;
            char* c2 = (char*)v2;
            Console.WriteLine(*c2);
        }}
    }}
}}
";
            var expectedIL = @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (char V_0, //ch
                void* V_1, //v1
                void* V_2, //v2
                char* V_3) //c2
  IL_0000:  ldc.i4.s   97
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  stloc.2
  IL_0009:  ldloc.2
  IL_000a:  stloc.3
  IL_000b:  ldloc.3
  IL_000c:  ldind.u2
  IL_000d:  call       ""void System.Console.WriteLine(char)""
  IL_0012:  ret
}
";
            var expectedOutput = @"a";

            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL);
            CompileAndVerify(string.Format(template, "checked"), options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL);
        }

        [Fact]
        public void ConvertPointerToNumericUnchecked()
        {
            var text = @"
using System;

unsafe class C
{
    void M(int* pi, void* pv, sbyte sb, byte b, short s, ushort us, int i, uint ui, long l, ulong ul)
    {
        unchecked
        {
            sb = (sbyte)pi;
            b = (byte)pi;
            s = (short)pi;
            us = (ushort)pi;
            i = (int)pi;
            ui = (uint)pi;
            l = (long)pi;
            ul = (ulong)pi;

            sb = (sbyte)pv;
            b = (byte)pv;
            s = (short)pv;
            us = (ushort)pv;
            i = (int)pv;
            ui = (uint)pv;
            l = (long)pv;
            ul = (ulong)pv;
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  conv.i1
  IL_0002:  starg.s    V_3
  IL_0004:  ldarg.1
  IL_0005:  conv.u1
  IL_0006:  starg.s    V_4
  IL_0008:  ldarg.1
  IL_0009:  conv.i2
  IL_000a:  starg.s    V_5
  IL_000c:  ldarg.1
  IL_000d:  conv.u2
  IL_000e:  starg.s    V_6
  IL_0010:  ldarg.1
  IL_0011:  conv.i4
  IL_0012:  starg.s    V_7
  IL_0014:  ldarg.1
  IL_0015:  conv.u4
  IL_0016:  starg.s    V_8
  IL_0018:  ldarg.1
  IL_0019:  conv.u8
  IL_001a:  starg.s    V_9
  IL_001c:  ldarg.1
  IL_001d:  conv.u8
  IL_001e:  starg.s    V_10
  IL_0020:  ldarg.2
  IL_0021:  conv.i1
  IL_0022:  starg.s    V_3
  IL_0024:  ldarg.2
  IL_0025:  conv.u1
  IL_0026:  starg.s    V_4
  IL_0028:  ldarg.2
  IL_0029:  conv.i2
  IL_002a:  starg.s    V_5
  IL_002c:  ldarg.2
  IL_002d:  conv.u2
  IL_002e:  starg.s    V_6
  IL_0030:  ldarg.2
  IL_0031:  conv.i4
  IL_0032:  starg.s    V_7
  IL_0034:  ldarg.2
  IL_0035:  conv.u4
  IL_0036:  starg.s    V_8
  IL_0038:  ldarg.2
  IL_0039:  conv.u8
  IL_003a:  starg.s    V_9
  IL_003c:  ldarg.2
  IL_003d:  conv.u8
  IL_003e:  starg.s    V_10
  IL_0040:  ret
}
");
        }

        [Fact]
        public void ConvertPointerToNumericChecked()
        {
            var text = @"
using System;

unsafe class C
{
    void M(int* pi, void* pv, sbyte sb, byte b, short s, ushort us, int i, uint ui, long l, ulong ul)
    {
        checked
        {
            sb = (sbyte)pi;
            b = (byte)pi;
            s = (short)pi;
            us = (ushort)pi;
            i = (int)pi;
            ui = (uint)pi;
            l = (long)pi;
            ul = (ulong)pi;

            sb = (sbyte)pv;
            b = (byte)pv;
            s = (short)pv;
            us = (ushort)pv;
            i = (int)pv;
            ui = (uint)pv;
            l = (long)pv;
            ul = (ulong)pv;
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  conv.ovf.i1.un
  IL_0002:  starg.s    V_3
  IL_0004:  ldarg.1
  IL_0005:  conv.ovf.u1.un
  IL_0006:  starg.s    V_4
  IL_0008:  ldarg.1
  IL_0009:  conv.ovf.i2.un
  IL_000a:  starg.s    V_5
  IL_000c:  ldarg.1
  IL_000d:  conv.ovf.u2.un
  IL_000e:  starg.s    V_6
  IL_0010:  ldarg.1
  IL_0011:  conv.ovf.i4.un
  IL_0012:  starg.s    V_7
  IL_0014:  ldarg.1
  IL_0015:  conv.ovf.u4.un
  IL_0016:  starg.s    V_8
  IL_0018:  ldarg.1
  IL_0019:  conv.ovf.i8.un
  IL_001a:  starg.s    V_9
  IL_001c:  ldarg.1
  IL_001d:  conv.u8
  IL_001e:  starg.s    V_10
  IL_0020:  ldarg.2
  IL_0021:  conv.ovf.i1.un
  IL_0022:  starg.s    V_3
  IL_0024:  ldarg.2
  IL_0025:  conv.ovf.u1.un
  IL_0026:  starg.s    V_4
  IL_0028:  ldarg.2
  IL_0029:  conv.ovf.i2.un
  IL_002a:  starg.s    V_5
  IL_002c:  ldarg.2
  IL_002d:  conv.ovf.u2.un
  IL_002e:  starg.s    V_6
  IL_0030:  ldarg.2
  IL_0031:  conv.ovf.i4.un
  IL_0032:  starg.s    V_7
  IL_0034:  ldarg.2
  IL_0035:  conv.ovf.u4.un
  IL_0036:  starg.s    V_8
  IL_0038:  ldarg.2
  IL_0039:  conv.ovf.i8.un
  IL_003a:  starg.s    V_9
  IL_003c:  ldarg.2
  IL_003d:  conv.u8
  IL_003e:  starg.s    V_10
  IL_0040:  ret
}
");
        }

        [Fact]
        public void ConvertNumericToPointerUnchecked()
        {
            var text = @"
using System;

unsafe class C
{
    void M(int* pi, void* pv, sbyte sb, byte b, short s, ushort us, int i, uint ui, long l, ulong ul)
    {
        unchecked
        {
            pi = (int*)sb;
            pi = (int*)b;
            pi = (int*)s;
            pi = (int*)us;
            pi = (int*)i;
            pi = (int*)ui;
            pi = (int*)l;
            pi = (int*)ul;

            pv = (void*)sb;
            pv = (void*)b;
            pv = (void*)s;
            pv = (void*)us;
            pv = (void*)i;
            pv = (void*)ui;
            pv = (void*)l;
            pv = (void*)ul;
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.FailsPEVerify).VerifyIL("C.M", @"
{
  // Code size       79 (0x4f)
  .maxstack  1
  IL_0000:  ldarg.3
  IL_0001:  conv.i
  IL_0002:  starg.s    V_1
  IL_0004:  ldarg.s    V_4
  IL_0006:  conv.u
  IL_0007:  starg.s    V_1
  IL_0009:  ldarg.s    V_5
  IL_000b:  conv.i
  IL_000c:  starg.s    V_1
  IL_000e:  ldarg.s    V_6
  IL_0010:  conv.u
  IL_0011:  starg.s    V_1
  IL_0013:  ldarg.s    V_7
  IL_0015:  conv.i
  IL_0016:  starg.s    V_1
  IL_0018:  ldarg.s    V_8
  IL_001a:  conv.u
  IL_001b:  starg.s    V_1
  IL_001d:  ldarg.s    V_9
  IL_001f:  conv.u
  IL_0020:  starg.s    V_1
  IL_0022:  ldarg.s    V_10
  IL_0024:  conv.u
  IL_0025:  starg.s    V_1
  IL_0027:  ldarg.3
  IL_0028:  conv.i
  IL_0029:  starg.s    V_2
  IL_002b:  ldarg.s    V_4
  IL_002d:  conv.u
  IL_002e:  starg.s    V_2
  IL_0030:  ldarg.s    V_5
  IL_0032:  conv.i
  IL_0033:  starg.s    V_2
  IL_0035:  ldarg.s    V_6
  IL_0037:  conv.u
  IL_0038:  starg.s    V_2
  IL_003a:  ldarg.s    V_7
  IL_003c:  conv.i
  IL_003d:  starg.s    V_2
  IL_003f:  ldarg.s    V_8
  IL_0041:  conv.u
  IL_0042:  starg.s    V_2
  IL_0044:  ldarg.s    V_9
  IL_0046:  conv.u
  IL_0047:  starg.s    V_2
  IL_0049:  ldarg.s    V_10
  IL_004b:  conv.u
  IL_004c:  starg.s    V_2
  IL_004e:  ret
}
");
        }

        [Fact]
        public void ConvertNumericToPointerChecked()
        {
            var text = @"
using System;

unsafe class C
{
    void M(int* pi, void* pv, sbyte sb, byte b, short s, ushort us, int i, uint ui, long l, ulong ul)
    {
        checked
        {
            pi = (int*)sb;
            pi = (int*)b;
            pi = (int*)s;
            pi = (int*)us;
            pi = (int*)i;
            pi = (int*)ui;
            pi = (int*)l;
            pi = (int*)ul;

            pv = (void*)sb;
            pv = (void*)b;
            pv = (void*)s;
            pv = (void*)us;
            pv = (void*)i;
            pv = (void*)ui;
            pv = (void*)l;
            pv = (void*)ul;
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.FailsPEVerify).VerifyIL("C.M", @"
{
  // Code size       79 (0x4f)
  .maxstack  1
  IL_0000:  ldarg.3
  IL_0001:  conv.ovf.u
  IL_0002:  starg.s    V_1
  IL_0004:  ldarg.s    V_4
  IL_0006:  conv.u
  IL_0007:  starg.s    V_1
  IL_0009:  ldarg.s    V_5
  IL_000b:  conv.ovf.u
  IL_000c:  starg.s    V_1
  IL_000e:  ldarg.s    V_6
  IL_0010:  conv.u
  IL_0011:  starg.s    V_1
  IL_0013:  ldarg.s    V_7
  IL_0015:  conv.ovf.u
  IL_0016:  starg.s    V_1
  IL_0018:  ldarg.s    V_8
  IL_001a:  conv.u
  IL_001b:  starg.s    V_1
  IL_001d:  ldarg.s    V_9
  IL_001f:  conv.ovf.u
  IL_0020:  starg.s    V_1
  IL_0022:  ldarg.s    V_10
  IL_0024:  conv.ovf.u.un
  IL_0025:  starg.s    V_1
  IL_0027:  ldarg.3
  IL_0028:  conv.ovf.u
  IL_0029:  starg.s    V_2
  IL_002b:  ldarg.s    V_4
  IL_002d:  conv.u
  IL_002e:  starg.s    V_2
  IL_0030:  ldarg.s    V_5
  IL_0032:  conv.ovf.u
  IL_0033:  starg.s    V_2
  IL_0035:  ldarg.s    V_6
  IL_0037:  conv.u
  IL_0038:  starg.s    V_2
  IL_003a:  ldarg.s    V_7
  IL_003c:  conv.ovf.u
  IL_003d:  starg.s    V_2
  IL_003f:  ldarg.s    V_8
  IL_0041:  conv.u
  IL_0042:  starg.s    V_2
  IL_0044:  ldarg.s    V_9
  IL_0046:  conv.ovf.u
  IL_0047:  starg.s    V_2
  IL_0049:  ldarg.s    V_10
  IL_004b:  conv.ovf.u.un
  IL_004c:  starg.s    V_2
  IL_004e:  ret
}
");
        }

        [Fact]
        public void ConvertClassToPointerUDC()
        {
            var template = @"
using System;

unsafe class C
{{
    void M(int* pi, void* pv, Explicit e, Implicit i)
    {{
        {0}
        {{
            e = (Explicit)pi;
            e = (Explicit)pv;

            i = pi;
            i = pv;

            pi = (int*)e;
            pv = (int*)e;

            pi = i;
            pv = i;
        }}
    }}
}}

unsafe class Explicit
{{
    public static explicit operator Explicit(void* p)
    {{
        return null;
    }}

    public static explicit operator int*(Explicit e)
    {{
        return null;
    }}
}}

unsafe class Implicit
{{
    public static implicit operator Implicit(void* p)
    {{
        return null;
    }}

    public static implicit operator int*(Implicit e)
    {{
        return null;
    }}
}}
";
            var expectedIL = @"
{
  // Code size       67 (0x43)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       ""Explicit Explicit.op_Explicit(void*)""
  IL_0006:  starg.s    V_3
  IL_0008:  ldarg.2
  IL_0009:  call       ""Explicit Explicit.op_Explicit(void*)""
  IL_000e:  starg.s    V_3
  IL_0010:  ldarg.1
  IL_0011:  call       ""Implicit Implicit.op_Implicit(void*)""
  IL_0016:  starg.s    V_4
  IL_0018:  ldarg.2
  IL_0019:  call       ""Implicit Implicit.op_Implicit(void*)""
  IL_001e:  starg.s    V_4
  IL_0020:  ldarg.3
  IL_0021:  call       ""int* Explicit.op_Explicit(Explicit)""
  IL_0026:  starg.s    V_1
  IL_0028:  ldarg.3
  IL_0029:  call       ""int* Explicit.op_Explicit(Explicit)""
  IL_002e:  starg.s    V_2
  IL_0030:  ldarg.s    V_4
  IL_0032:  call       ""int* Implicit.op_Implicit(Implicit)""
  IL_0037:  starg.s    V_1
  IL_0039:  ldarg.s    V_4
  IL_003b:  call       ""int* Implicit.op_Implicit(Implicit)""
  IL_0040:  starg.s    V_2
  IL_0042:  ret
}
";
            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", expectedIL);
            CompileAndVerify(string.Format(template, "checked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", expectedIL);
        }

        [Fact]
        public void ConvertIntPtrToPointer()
        {
            var template = @"
using System;

unsafe class C
{{
    void M(int* pi, void* pv, IntPtr i, UIntPtr u)
    {{
        {0}
        {{
            i = (IntPtr)pi;
            i = (IntPtr)pv;

            u = (UIntPtr)pi;
            u = (UIntPtr)pv;

            pi = (int*)i;
            pv = (int*)i;

            pi = (int*)u;
            pv = (int*)u;
        }}
    }}
}}
";
            // Nothing special here - just more UDCs.
            var expectedIL = @"
{
  // Code size       67 (0x43)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  starg.s    V_3
  IL_0008:  ldarg.2
  IL_0009:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_000e:  starg.s    V_3
  IL_0010:  ldarg.1
  IL_0011:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0016:  starg.s    V_4
  IL_0018:  ldarg.2
  IL_0019:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_001e:  starg.s    V_4
  IL_0020:  ldarg.3
  IL_0021:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0026:  starg.s    V_1
  IL_0028:  ldarg.3
  IL_0029:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_002e:  starg.s    V_2
  IL_0030:  ldarg.s    V_4
  IL_0032:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0037:  starg.s    V_1
  IL_0039:  ldarg.s    V_4
  IL_003b:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0040:  starg.s    V_2
  IL_0042:  ret
}
";
            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", expectedIL);
            CompileAndVerify(string.Format(template, "checked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", expectedIL);
        }

        [Fact]
        public void FixedStatementConversion()
        {
            var template = @"
using System;

unsafe class C
{{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {{
        {0}
        {{
            C c = new C();
            fixed (void* p = &c.c, q = c.a, r = ""hello"")
            {{
                Console.Write((int)*(char*)p);
                Console.Write((int)*(char*)q);
                Console.Write((int)*(char*)r);
            }}
        }}
    }}
}}
";
            // NB: "pinned System.IntPtr&" (which ildasm displays as "pinned native int&"), not void.
            var expectedIL = @"
{
  // Code size      112 (0x70)
  .maxstack  2
  .locals init (C V_0, //c
                void* V_1, //p
                void* V_2, //q
                void* V_3, //r
                pinned char& V_4,
                pinned char[] V_5,
                pinned string V_6)
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  newobj     ""C..ctor()""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldflda     ""char C.c""
  IL_000e:  stloc.s    V_4
 -IL_0010:  ldloc.s    V_4
  IL_0012:  conv.u
  IL_0013:  stloc.1
 -IL_0014:  ldloc.0
  IL_0015:  ldfld      ""char[] C.a""
  IL_001a:  dup
  IL_001b:  stloc.s    V_5
  IL_001d:  brfalse.s  IL_0025
  IL_001f:  ldloc.s    V_5
  IL_0021:  ldlen
  IL_0022:  conv.i4
  IL_0023:  brtrue.s   IL_002a
  IL_0025:  ldc.i4.0
  IL_0026:  conv.u
  IL_0027:  stloc.2
  IL_0028:  br.s       IL_0034
  IL_002a:  ldloc.s    V_5
  IL_002c:  ldc.i4.0
  IL_002d:  ldelema    ""char""
  IL_0032:  conv.u
  IL_0033:  stloc.2
  IL_0034:  ldstr      ""hello""
  IL_0039:  stloc.s    V_6
 -IL_003b:  ldloc.s    V_6
  IL_003d:  conv.u
  IL_003e:  stloc.3
  IL_003f:  ldloc.3
  IL_0040:  brfalse.s  IL_004a
  IL_0042:  ldloc.3
  IL_0043:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0048:  add
  IL_0049:  stloc.3
 -IL_004a:  nop
 -IL_004b:  ldloc.1
  IL_004c:  ldind.u2
  IL_004d:  call       ""void System.Console.Write(int)""
  IL_0052:  nop
 -IL_0053:  ldloc.2
  IL_0054:  ldind.u2
  IL_0055:  call       ""void System.Console.Write(int)""
  IL_005a:  nop
 -IL_005b:  ldloc.3
  IL_005c:  ldind.u2
  IL_005d:  call       ""void System.Console.Write(int)""
  IL_0062:  nop
 -IL_0063:  nop
 ~IL_0064:  ldc.i4.0
  IL_0065:  conv.u
  IL_0066:  stloc.s    V_4
  IL_0068:  ldnull
  IL_0069:  stloc.s    V_5
  IL_006b:  ldnull
  IL_006c:  stloc.s    V_6
 -IL_006e:  nop
 -IL_006f:  ret
}
";
            var expectedOutput = @"970104";
            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeDebugExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL, sequencePoints: "C.Main");
            CompileAndVerify(string.Format(template, "checked  "), options: TestOptions.UnsafeDebugExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL, sequencePoints: "C.Main");
        }

        [Fact]
        public void FixedStatementVoidPointerPointer()
        {
            var template = @"
using System;

unsafe class C
{{
    void* v;

    static void Main()
    {{
        {0}
        {{
            char ch = 'a';
            C c = new C();
            c.v = &ch;
            fixed (void** p = &c.v)
            {{
                Console.Write(*(char*)*p);
            }}
        }}
    }}
}}
";
            // NB: "pinned void*&", as in Dev10.
            var expectedIL = @"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (char V_0, //ch
                pinned void*& V_1)
  IL_0000:  ldc.i4.s   97
  IL_0002:  stloc.0
  IL_0003:  newobj     ""C..ctor()""
  IL_0008:  dup
  IL_0009:  ldloca.s   V_0
  IL_000b:  conv.u
  IL_000c:  stfld      ""void* C.v""
  IL_0011:  ldflda     ""void* C.v""
  IL_0016:  stloc.1
  IL_0017:  ldloc.1
  IL_0018:  conv.u
  IL_0019:  ldind.i
  IL_001a:  ldind.u2
  IL_001b:  call       ""void System.Console.Write(char)""
  IL_0020:  ldc.i4.0
  IL_0021:  conv.u
  IL_0022:  stloc.1
  IL_0023:  ret
}
";
            var expectedOutput = @"a";
            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL);
            CompileAndVerify(string.Format(template, "checked"), options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("C.Main", expectedIL);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void PointerArrayConversion()
        {
            var template = @"
using System;

unsafe class C
{{
    void M(int*[] api, void*[] apv, Array a)
    {{
        {0}
        {{
            a = api;
            a = apv;

            api = (int*[])a;
            apv = (void*[])a;
        }}
    }}
}}
";
            var expectedIL = @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  starg.s    V_3
  IL_0003:  ldarg.2
  IL_0004:  starg.s    V_3
  IL_0006:  ldarg.3
  IL_0007:  castclass  ""int*[]""
  IL_000c:  starg.s    V_1
  IL_000e:  ldarg.3
  IL_000f:  castclass  ""void*[]""
  IL_0014:  starg.s    V_2
  IL_0016:  ret
}
";
            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes).VerifyIL("C.M", expectedIL);
            CompileAndVerify(string.Format(template, "checked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes).VerifyIL("C.M", expectedIL);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void PointerArrayConversionRuntimeError()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            int*[] api = new int*[1];
            System.Array a = api;
            a.GetValue(0);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";
            CompileAndVerifyException<NotSupportedException>(text, "Type is not supported.", allowUnsafe: true, verify: Verification.Fails);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void PointerArrayEnumerableConversion()
        {
            var template = @"
using System.Collections;

unsafe class C
{{
    void M(int*[] api, void*[] apv, IEnumerable e)
    {{
        {0}
        {{
            e = api;
            e = apv;

            api = (int*[])e;
            apv = (void*[])e;
        }}
    }}
}}
";
            var expectedIL = @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  starg.s    V_3
  IL_0003:  ldarg.2
  IL_0004:  starg.s    V_3
  IL_0006:  ldarg.3
  IL_0007:  castclass  ""int*[]""
  IL_000c:  starg.s    V_1
  IL_000e:  ldarg.3
  IL_000f:  castclass  ""void*[]""
  IL_0014:  starg.s    V_2
  IL_0016:  ret
}
";
            CompileAndVerify(string.Format(template, "unchecked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes).VerifyIL("C.M", expectedIL);
            CompileAndVerify(string.Format(template, "checked"), options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes).VerifyIL("C.M", expectedIL);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void PointerArrayEnumerableConversionRuntimeError()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            int*[] api = new int*[1];
            System.Collections.IEnumerable e = api;
            var enumerator = e.GetEnumerator();
            enumerator.MoveNext();
            var current = enumerator.Current;
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";
            CompileAndVerifyException<NotSupportedException>(text, "Type is not supported.", allowUnsafe: true, verify: Verification.Fails);
        }

        [Fact]
        public void PointerArrayForeachSingle()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        int*[] array = new []
        {
            (int*)1,
            (int*)2,
        };
        foreach (var element in array)
        {
            Console.Write((int)element);
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Fails).VerifyIL("C.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (int*[] V_0,
                int V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""int*""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  conv.i
  IL_000a:  stelem.i
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  conv.i
  IL_000f:  stelem.i
  IL_0010:  stloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  stloc.1
  IL_0013:  br.s       IL_0022
  IL_0015:  ldloc.0
  IL_0016:  ldloc.1
  IL_0017:  ldelem.i
  IL_0018:  conv.i4
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ldloc.1
  IL_001f:  ldc.i4.1
  IL_0020:  add
  IL_0021:  stloc.1
  IL_0022:  ldloc.1
  IL_0023:  ldloc.0
  IL_0024:  ldlen
  IL_0025:  conv.i4
  IL_0026:  blt.s      IL_0015
  IL_0028:  ret
}
");
        }

        [Fact]
        public void PointerArrayForeachMultiple()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        int*[,] array = new [,]
        {
            { (int*)1, (int*)2, },
            { (int*)3, (int*)4, },
        };
        foreach (var element in array)
        {
            Console.Write((int)element);
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "1234", verify: Verification.FailsPEVerify).VerifyIL("C.Main", @"
{
  // Code size      120 (0x78)
  .maxstack  5
  .locals init (int*[,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4)
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""int*[*,*]..ctor""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.1
  IL_000b:  conv.i
  IL_000c:  call       ""int*[*,*].Set""
  IL_0011:  dup
  IL_0012:  ldc.i4.0
  IL_0013:  ldc.i4.1
  IL_0014:  ldc.i4.2
  IL_0015:  conv.i
  IL_0016:  call       ""int*[*,*].Set""
  IL_001b:  dup
  IL_001c:  ldc.i4.1
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.3
  IL_001f:  conv.i
  IL_0020:  call       ""int*[*,*].Set""
  IL_0025:  dup
  IL_0026:  ldc.i4.1
  IL_0027:  ldc.i4.1
  IL_0028:  ldc.i4.4
  IL_0029:  conv.i
  IL_002a:  call       ""int*[*,*].Set""
  IL_002f:  stloc.0
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.0
  IL_0032:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0037:  stloc.1
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.1
  IL_003a:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_003f:  stloc.2
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.0
  IL_0042:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0047:  stloc.3
  IL_0048:  br.s       IL_0073
  IL_004a:  ldloc.0
  IL_004b:  ldc.i4.1
  IL_004c:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0051:  stloc.s    V_4
  IL_0053:  br.s       IL_006a
  IL_0055:  ldloc.0
  IL_0056:  ldloc.3
  IL_0057:  ldloc.s    V_4
  IL_0059:  call       ""int*[*,*].Get""
  IL_005e:  conv.i4
  IL_005f:  call       ""void System.Console.Write(int)""
  IL_0064:  ldloc.s    V_4
  IL_0066:  ldc.i4.1
  IL_0067:  add
  IL_0068:  stloc.s    V_4
  IL_006a:  ldloc.s    V_4
  IL_006c:  ldloc.2
  IL_006d:  ble.s      IL_0055
  IL_006f:  ldloc.3
  IL_0070:  ldc.i4.1
  IL_0071:  add
  IL_0072:  stloc.3
  IL_0073:  ldloc.3
  IL_0074:  ldloc.1
  IL_0075:  ble.s      IL_004a
  IL_0077:  ret
}
");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void PointerArrayForeachEnumerable()
        {
            var text = @"
using System;
using System.Collections;

unsafe class C
{
    static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            int*[] array = new []
            {
                (int*)1,
                (int*)2,
            };
            foreach (var element in (IEnumerable)array)
            {
                Console.Write((int)element);
            }
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";
            CompileAndVerifyException<NotSupportedException>(text, "Type is not supported.", allowUnsafe: true, verify: Verification.Fails);
        }

        #endregion Pointer conversion tests

        #region sizeof tests

        [Fact]
        public void SizeOfConstant()
        {
            var text = @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(sizeof(sbyte));
        Console.WriteLine(sizeof(byte));
        Console.WriteLine(sizeof(short));
        Console.WriteLine(sizeof(ushort));
        Console.WriteLine(sizeof(int));
        Console.WriteLine(sizeof(uint));
        Console.WriteLine(sizeof(long));
        Console.WriteLine(sizeof(ulong));
        Console.WriteLine(sizeof(char));
        Console.WriteLine(sizeof(float));
        Console.WriteLine(sizeof(double));
        Console.WriteLine(sizeof(bool));
        Console.WriteLine(sizeof(decimal)); //Supported by dev10, but not spec.
    }
}
";
            var expectedOutput = @"
1
1
2
2
4
4
8
8
2
4
8
1
16
".Trim();
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Passes).VerifyIL("C.Main", @"
{
  // Code size       80 (0x50)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.WriteLine(int)""
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ldc.i4.2
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""void System.Console.WriteLine(int)""
  IL_0018:  ldc.i4.4
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldc.i4.4
  IL_001f:  call       ""void System.Console.WriteLine(int)""
  IL_0024:  ldc.i4.8
  IL_0025:  call       ""void System.Console.WriteLine(int)""
  IL_002a:  ldc.i4.8
  IL_002b:  call       ""void System.Console.WriteLine(int)""
  IL_0030:  ldc.i4.2
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ldc.i4.4
  IL_0037:  call       ""void System.Console.WriteLine(int)""
  IL_003c:  ldc.i4.8
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ldc.i4.1
  IL_0043:  call       ""void System.Console.WriteLine(int)""
  IL_0048:  ldc.i4.s   16
  IL_004a:  call       ""void System.Console.WriteLine(int)""
  IL_004f:  ret
}
");
        }

        [Fact]
        public void SizeOfNonConstant()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        Console.WriteLine(sizeof(S));
        Console.WriteLine(sizeof(Outer.Inner));
        Console.WriteLine(sizeof(int*));
        Console.WriteLine(sizeof(void*));
    }
}

struct S
{
    public byte b;
}

class Outer
{
    public struct Inner
    {
        public char c;
    }
}
";
            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
1
2
4
4
".Trim();
            }
            else
            {
                expectedOutput = @"
1
2
8
8
".Trim();
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Passes).VerifyIL("C.Main", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  IL_0000:  sizeof     ""S""
  IL_0006:  call       ""void System.Console.WriteLine(int)""
  IL_000b:  sizeof     ""Outer.Inner""
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  sizeof     ""int*""
  IL_001c:  call       ""void System.Console.WriteLine(int)""
  IL_0021:  sizeof     ""void*""
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  ret
}
");
        }

        [Fact]
        public void SizeOfEnum()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        Console.WriteLine(sizeof(E1));
        Console.WriteLine(sizeof(E2));
        Console.WriteLine(sizeof(E3));
    }
}

enum E1 { A }
enum E2 : byte { A }
enum E3 : long { A }
";
            var expectedOutput = @"
4
1
8
".Trim();
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Passes).VerifyIL("C.Main", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  ldc.i4.4
  IL_0001:  call       ""void System.Console.WriteLine(int)""
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ldc.i4.8
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  ret
}
");
        }

        #endregion sizeof tests

        #region Pointer arithmetic tests

        [Fact]
        public void NumericAdditionChecked()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        checked
        {
            S s = new S();
            S* p = &s;
            p = p + 2;
            p = p + 3u;
            p = p + 4l;
            p = p + 5ul;
        }
    }
}
";

            // Dev10 has conv.u after IL_000d and conv.i8 in place of conv.u8 at IL_0017.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldc.i4.2
  IL_000c:  conv.i
  IL_000d:  sizeof     ""S""
  IL_0013:  mul.ovf
  IL_0014:  add.ovf.un
  IL_0015:  ldc.i4.3
  IL_0016:  conv.u8
  IL_0017:  sizeof     ""S""
  IL_001d:  conv.i8
  IL_001e:  mul.ovf
  IL_001f:  conv.i
  IL_0020:  add.ovf.un
  IL_0021:  ldc.i4.4
  IL_0022:  conv.i8
  IL_0023:  sizeof     ""S""
  IL_0029:  conv.i8
  IL_002a:  mul.ovf
  IL_002b:  conv.i
  IL_002c:  add.ovf.un
  IL_002d:  ldc.i4.5
  IL_002e:  conv.i8
  IL_002f:  sizeof     ""S""
  IL_0035:  conv.ovf.u8
  IL_0036:  mul.ovf.un
  IL_0037:  conv.u
  IL_0038:  add.ovf.un
  IL_0039:  pop
  IL_003a:  ret
}
");
        }

        [Fact]
        public void NumericAdditionUnchecked()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        unchecked
        {
            S s = new S();
            S* p = &s;
            p = p + 2;
            p = p + 3u;
            p = p + 4l;
            p = p + 5ul;
        }
    }
}
";

            // Dev10 has conv.u after IL_000d and conv.i8 in place of conv.u8 at IL_0017.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldc.i4.2
  IL_000c:  conv.i
  IL_000d:  sizeof     ""S""
  IL_0013:  mul
  IL_0014:  add
  IL_0015:  ldc.i4.3
  IL_0016:  conv.u8
  IL_0017:  sizeof     ""S""
  IL_001d:  conv.i8
  IL_001e:  mul
  IL_001f:  conv.i
  IL_0020:  add
  IL_0021:  ldc.i4.4
  IL_0022:  conv.i8
  IL_0023:  sizeof     ""S""
  IL_0029:  conv.i8
  IL_002a:  mul
  IL_002b:  conv.i
  IL_002c:  add
  IL_002d:  pop
  IL_002e:  ldc.i4.5
  IL_002f:  conv.i8
  IL_0030:  sizeof     ""S""
  IL_0036:  conv.i8
  IL_0037:  mul
  IL_0038:  conv.u
  IL_0039:  pop
  IL_003a:  ret
}
");
        }

        [Fact]
        public void NumericSubtractionChecked()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        checked
        {
            S s = new S();
            S* p = &s;
            p = p - 2;
            p = p - 3u;
            p = p - 4l;
            p = p - 5ul;
        }
    }
}
";

            // Dev10 has conv.u after IL_000d and conv.i8 in place of conv.u8 at IL_0017.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldc.i4.2
  IL_000c:  conv.i
  IL_000d:  sizeof     ""S""
  IL_0013:  mul.ovf
  IL_0014:  sub.ovf.un
  IL_0015:  ldc.i4.3
  IL_0016:  conv.u8
  IL_0017:  sizeof     ""S""
  IL_001d:  conv.i8
  IL_001e:  mul.ovf
  IL_001f:  conv.i
  IL_0020:  sub.ovf.un
  IL_0021:  ldc.i4.4
  IL_0022:  conv.i8
  IL_0023:  sizeof     ""S""
  IL_0029:  conv.i8
  IL_002a:  mul.ovf
  IL_002b:  conv.i
  IL_002c:  sub.ovf.un
  IL_002d:  ldc.i4.5
  IL_002e:  conv.i8
  IL_002f:  sizeof     ""S""
  IL_0035:  conv.ovf.u8
  IL_0036:  mul.ovf.un
  IL_0037:  conv.u
  IL_0038:  sub.ovf.un
  IL_0039:  pop
  IL_003a:  ret
}
");
        }

        [Fact]
        public void NumericSubtractionUnchecked()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        unchecked
        {
            S s = new S();
            S* p = &s;
            p = p - 2;
            p = p - 3u;
            p = p - 4l;
            p = p - 5ul;
        }
    }
}
";

            // Dev10 has conv.u after IL_000d and conv.i8 in place of conv.u8 at IL_0017.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  ldc.i4.2
  IL_000c:  conv.i
  IL_000d:  sizeof     ""S""
  IL_0013:  mul
  IL_0014:  sub
  IL_0015:  ldc.i4.3
  IL_0016:  conv.u8
  IL_0017:  sizeof     ""S""
  IL_001d:  conv.i8
  IL_001e:  mul
  IL_001f:  conv.i
  IL_0020:  sub
  IL_0021:  ldc.i4.4
  IL_0022:  conv.i8
  IL_0023:  sizeof     ""S""
  IL_0029:  conv.i8
  IL_002a:  mul
  IL_002b:  conv.i
  IL_002c:  sub
  IL_002d:  pop
  IL_002e:  ldc.i4.5
  IL_002f:  conv.i8
  IL_0030:  sizeof     ""S""
  IL_0036:  conv.i8
  IL_0037:  mul
  IL_0038:  conv.u
  IL_0039:  pop
  IL_003a:  ret
}
");
        }

        [WorkItem(546750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546750")]
        [Fact]
        public void NumericAdditionUnchecked_SizeOne()
        {
            var text = @"
using System;

unsafe class C
{
    void Test(int i, uint u, long l, ulong ul)
    {
        unchecked
        {
            byte b = 3;
            byte* p = &b;
            p = p + 2;
            p = p + 3u;
            p = p + 4l;
            p = p + 5ul;
            p = p + i;
            p = p + u;
            p = p + l;
            p = p + ul;
        }
    }
}
";
            // NOTE: even when not optimized.
            // NOTE: additional conversions applied to constants of type int and uint.
            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll, verify: Verification.Fails).VerifyIL("C.Test", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (byte V_0, //b
                byte* V_1) //p
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldc.i4.3
  IL_0003:  stloc.0
  IL_0004:  ldloca.s   V_0
  IL_0006:  conv.u
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.2
  IL_000a:  add
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.3
  IL_000e:  add
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.4
  IL_0012:  conv.i8
  IL_0013:  conv.i
  IL_0014:  add
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.5
  IL_0018:  conv.i8
  IL_0019:  conv.u
  IL_001a:  add
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  ldarg.1
  IL_001e:  add
  IL_001f:  stloc.1
  IL_0020:  ldloc.1
  IL_0021:  ldarg.2
  IL_0022:  conv.u
  IL_0023:  add
  IL_0024:  stloc.1
  IL_0025:  ldloc.1
  IL_0026:  ldarg.3
  IL_0027:  conv.i
  IL_0028:  add
  IL_0029:  stloc.1
  IL_002a:  ldloc.1
  IL_002b:  ldarg.s    V_4
  IL_002d:  conv.u
  IL_002e:  add
  IL_002f:  stloc.1
  IL_0030:  nop
  IL_0031:  ret
}
");
        }

        [WorkItem(18871, "https://github.com/dotnet/roslyn/issues/18871")]
        [WorkItem(546750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546750")]
        [Fact]
        public void NumericAdditionChecked_SizeOne()
        {
            var text = @"
using System;

unsafe class C
{
    void Test(int i, uint u, long l, ulong ul)
    {
        checked
        {
            byte b = 3;
            byte* p = &b;
            p = p + 2;
            p = p + 3u;
            p = p + 4l;
            p = p + 5ul;
            p = p + i;
            p = p + u;
            p = p + l;
            p = p + ul;
            p = p + (-2);
        }
    }

    void Test1(int i, uint u, long l, ulong ul)
    {
        checked
        {
            byte b = 3;
            byte* p = &b;
            p = p - 2;
            p = p - 3u;
            p = p - 4l;
            p = p - 5ul;
            p = p - i;
            p = p - u;
            p = p - l;
            p = p - ul;
            p = p - (-1);
        }
    }
}
";
            // NOTE: even when not optimized.
            // NOTE: additional conversions applied to constants of type int and uint.
            // NOTE: identical to unchecked except "add" becomes "add.ovf.un".
            var comp = CompileAndVerify(text, options: TestOptions.UnsafeDebugDll, verify: Verification.Fails);

            comp.VerifyIL("C.Test", @"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (byte V_0, //b
                byte* V_1) //p
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldc.i4.3
  IL_0003:  stloc.0
  IL_0004:  ldloca.s   V_0
  IL_0006:  conv.u
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.2
  IL_000a:  add.ovf.un
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.3
  IL_000e:  add.ovf.un
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.4
  IL_0012:  conv.i8
  IL_0013:  conv.i
  IL_0014:  add.ovf.un
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.5
  IL_0018:  conv.i8
  IL_0019:  conv.u
  IL_001a:  add.ovf.un
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  ldarg.1
  IL_001e:  conv.i
  IL_001f:  add.ovf.un
  IL_0020:  stloc.1
  IL_0021:  ldloc.1
  IL_0022:  ldarg.2
  IL_0023:  conv.u
  IL_0024:  add.ovf.un
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldarg.3
  IL_0028:  conv.i
  IL_0029:  add.ovf.un
  IL_002a:  stloc.1
  IL_002b:  ldloc.1
  IL_002c:  ldarg.s    V_4
  IL_002e:  conv.u
  IL_002f:  add.ovf.un
  IL_0030:  stloc.1
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.s   -2
  IL_0034:  conv.i
  IL_0035:  add.ovf.un
  IL_0036:  stloc.1
  IL_0037:  nop
  IL_0038:  ret
}");

            comp.VerifyIL("C.Test1", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (byte V_0, //b
                byte* V_1) //p
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldc.i4.3
  IL_0003:  stloc.0
  IL_0004:  ldloca.s   V_0
  IL_0006:  conv.u
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.2
  IL_000a:  sub.ovf.un
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.3
  IL_000e:  sub.ovf.un
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.4
  IL_0012:  conv.i8
  IL_0013:  conv.i
  IL_0014:  sub.ovf.un
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.5
  IL_0018:  conv.i8
  IL_0019:  conv.u
  IL_001a:  sub.ovf.un
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  ldarg.1
  IL_001e:  conv.i
  IL_001f:  sub.ovf.un
  IL_0020:  stloc.1
  IL_0021:  ldloc.1
  IL_0022:  ldarg.2
  IL_0023:  conv.u
  IL_0024:  sub.ovf.un
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldarg.3
  IL_0028:  conv.i
  IL_0029:  sub.ovf.un
  IL_002a:  stloc.1
  IL_002b:  ldloc.1
  IL_002c:  ldarg.s    V_4
  IL_002e:  conv.u
  IL_002f:  sub.ovf.un
  IL_0030:  stloc.1
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.m1
  IL_0033:  conv.i
  IL_0034:  sub.ovf.un
  IL_0035:  stloc.1
  IL_0036:  nop
  IL_0037:  ret
}");
        }

        [Fact]
        public void CheckedSignExtend()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        byte* ptr1 = default(byte*);

        ptr1 = (byte*)2;
        checked
        {
            // should not overflow regardless of 32/64 bit
            ptr1 = ptr1 + 2147483649;
        }

        Console.WriteLine((long)ptr1);

        byte* ptr = (byte*)2;
        try
        { 
            checked
            {
                int i = -1;
                // should overflow regardless of 32/64 bit
                ptr = ptr + i;
            }
            Console.WriteLine((long)ptr);
        }
        catch (OverflowException)
        {
            Console.WriteLine(""overflow"");
            Console.WriteLine((long)ptr);
        }
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"2147483651
overflow
2", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       67 (0x43)
  .maxstack  2
  .locals init (byte* V_0, //ptr1
                byte* V_1, //ptr
                int V_2) //i
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""byte*""
  IL_0008:  ldc.i4.2
  IL_0009:  conv.i
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4     0x80000001
  IL_0011:  conv.u
  IL_0012:  add.ovf.un
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  conv.u8
  IL_0016:  call       ""void System.Console.WriteLine(long)""
  IL_001b:  ldc.i4.2
  IL_001c:  conv.i
  IL_001d:  stloc.1
  .try
  {
    IL_001e:  ldc.i4.m1
    IL_001f:  stloc.2
    IL_0020:  ldloc.1
    IL_0021:  ldloc.2
    IL_0022:  conv.i
    IL_0023:  add.ovf.un
    IL_0024:  stloc.1
    IL_0025:  ldloc.1
    IL_0026:  conv.u8
    IL_0027:  call       ""void System.Console.WriteLine(long)""
    IL_002c:  leave.s    IL_0042
  }
  catch System.OverflowException
  {
    IL_002e:  pop
    IL_002f:  ldstr      ""overflow""
    IL_0034:  call       ""void System.Console.WriteLine(string)""
    IL_0039:  ldloc.1
    IL_003a:  conv.u8
    IL_003b:  call       ""void System.Console.WriteLine(long)""
    IL_0040:  leave.s    IL_0042
  }
  IL_0042:  ret
}
");
        }

        [Fact]
        public void Increment()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S* p = (S*)0;
        checked
        {
            p++;
        }
        checked
        {
            ++p;
        }
        unchecked
        {
            p++;
        }
        unchecked
        {
            ++p;
        }
        Console.WriteLine((int)p);
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "4", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (S* V_0) //p
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  sizeof     ""S""
  IL_000a:  add.ovf.un
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  sizeof     ""S""
  IL_0013:  add.ovf.un
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  sizeof     ""S""
  IL_001c:  add
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  sizeof     ""S""
  IL_0025:  add
  IL_0026:  stloc.0
  IL_0027:  ldloc.0
  IL_0028:  conv.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ret
}
");
        }

        [Fact]
        public void Decrement()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S* p = (S*)8;
        checked
        {
            p--;
        }
        checked
        {
            --p;
        }
        unchecked
        {
            p--;
        }
        unchecked
        {
            --p;
        }
        Console.WriteLine((int)p);
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "4", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (S* V_0) //p
  IL_0000:  ldc.i4.8
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  sizeof     ""S""
  IL_000a:  sub.ovf.un
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  sizeof     ""S""
  IL_0013:  sub.ovf.un
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  sizeof     ""S""
  IL_001c:  sub
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  sizeof     ""S""
  IL_0025:  sub
  IL_0026:  stloc.0
  IL_0027:  ldloc.0
  IL_0028:  conv.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ret
}
");
        }

        [Fact]
        public void IncrementProperty()
        {
            var text = @"
using System;

unsafe struct S
{
    S* P { get; set; }
    S* this[int x] { get { return P; } set { P = value; } }

    static void Main()
    {
        S s = new S();
        s.P++;
        --s[GetIndex()];
        Console.Write((int)s.P);
    }

    static int GetIndex()
    {
        Console.Write(""I"");
        return 1;
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "I0", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       74 (0x4a)
  .maxstack  3
  .locals init (S V_0, //s
                S* V_1,
                int V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  dup
  IL_000b:  call       ""readonly S* S.P.get""
  IL_0010:  stloc.1
  IL_0011:  ldloc.1
  IL_0012:  sizeof     ""S""
  IL_0018:  add
  IL_0019:  call       ""void S.P.set""
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""int S.GetIndex()""
  IL_0025:  stloc.2
  IL_0026:  dup
  IL_0027:  ldloc.2
  IL_0028:  call       ""S* S.this[int].get""
  IL_002d:  sizeof     ""S""
  IL_0033:  sub
  IL_0034:  stloc.1
  IL_0035:  ldloc.2
  IL_0036:  ldloc.1
  IL_0037:  call       ""void S.this[int].set""
  IL_003c:  ldloca.s   V_0
  IL_003e:  call       ""readonly S* S.P.get""
  IL_0043:  conv.i4
  IL_0044:  call       ""void System.Console.Write(int)""
  IL_0049:  ret
}
");
        }

        [Fact]
        public void CompoundAssignment()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S* p = (S*)8;
        checked
        {
            p += 1;
            p += 2U;
            p -= 1L;
            p -= 2UL;
        }
        unchecked
        {
            p += 1;
            p += 2U;
            p -= 1L;
            p -= 2UL;
        }
        Console.WriteLine((int)p);
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "8", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size      103 (0x67)
  .maxstack  3
  .locals init (S* V_0) //p
  IL_0000:  ldc.i4.8
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  sizeof     ""S""
  IL_000a:  add.ovf.un
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.2
  IL_000e:  conv.u8
  IL_000f:  sizeof     ""S""
  IL_0015:  conv.i8
  IL_0016:  mul.ovf
  IL_0017:  conv.i
  IL_0018:  add.ovf.un
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  sizeof     ""S""
  IL_0021:  sub.ovf.un
  IL_0022:  stloc.0
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.2
  IL_0025:  conv.i8
  IL_0026:  sizeof     ""S""
  IL_002c:  conv.ovf.u8
  IL_002d:  mul.ovf.un
  IL_002e:  conv.u
  IL_002f:  sub.ovf.un
  IL_0030:  stloc.0
  IL_0031:  ldloc.0
  IL_0032:  sizeof     ""S""
  IL_0038:  add
  IL_0039:  stloc.0
  IL_003a:  ldloc.0
  IL_003b:  ldc.i4.2
  IL_003c:  conv.u8
  IL_003d:  sizeof     ""S""
  IL_0043:  conv.i8
  IL_0044:  mul
  IL_0045:  conv.i
  IL_0046:  add
  IL_0047:  stloc.0
  IL_0048:  ldloc.0
  IL_0049:  sizeof     ""S""
  IL_004f:  sub
  IL_0050:  stloc.0
  IL_0051:  ldloc.0
  IL_0052:  ldc.i4.2
  IL_0053:  conv.i8
  IL_0054:  sizeof     ""S""
  IL_005a:  conv.i8
  IL_005b:  mul
  IL_005c:  conv.u
  IL_005d:  sub
  IL_005e:  stloc.0
  IL_005f:  ldloc.0
  IL_0060:  conv.i4
  IL_0061:  call       ""void System.Console.WriteLine(int)""
  IL_0066:  ret
}
");
        }

        [Fact]
        public void CompoundAssignProperty()
        {
            var text = @"
using System;

unsafe struct S
{
    S* P { get; set; }
    S* this[int x] { get { return P; } set { P = value; } }

    static void Main()
    {
        S s = new S();
        s.P += 3;
        s[GetIndex()] -= 2;
        Console.Write((int)s.P);
    }

    static int GetIndex()
    {
        Console.Write(""I"");
        return 1;
    }
}
";

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"I4";
            }
            else
            {
                expectedOutput = @"I8";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       78 (0x4e)
  .maxstack  5
  .locals init (S V_0, //s
                S& V_1,
                int V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  dup
  IL_000b:  call       ""readonly S* S.P.get""
  IL_0010:  ldc.i4.3
  IL_0011:  conv.i
  IL_0012:  sizeof     ""S""
  IL_0018:  mul
  IL_0019:  add
  IL_001a:  call       ""void S.P.set""
  IL_001f:  ldloca.s   V_0
  IL_0021:  stloc.1
  IL_0022:  call       ""int S.GetIndex()""
  IL_0027:  stloc.2
  IL_0028:  ldloc.1
  IL_0029:  ldloc.2
  IL_002a:  ldloc.1
  IL_002b:  ldloc.2
  IL_002c:  call       ""S* S.this[int].get""
  IL_0031:  ldc.i4.2
  IL_0032:  conv.i
  IL_0033:  sizeof     ""S""
  IL_0039:  mul
  IL_003a:  sub
  IL_003b:  call       ""void S.this[int].set""
  IL_0040:  ldloca.s   V_0
  IL_0042:  call       ""readonly S* S.P.get""
  IL_0047:  conv.i4
  IL_0048:  call       ""void System.Console.Write(int)""
  IL_004d:  ret
}
");
        }

        [Fact]
        public void PointerSubtraction_EmptyStruct()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S* p = (S*)8;
        S* q = (S*)4;
        checked
        {
            Console.Write(p - q);
        }
        unchecked
        {
            Console.Write(p - q);
        }
    }
}
";

            // NOTE: don't use checked subtraction or division in either case (matches dev10).
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "44", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (S* V_0, //p
                S* V_1) //q
  IL_0000:  ldc.i4.8
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.4
  IL_0004:  conv.i
  IL_0005:  stloc.1
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  sub
  IL_0009:  sizeof     ""S""
  IL_000f:  div
  IL_0010:  conv.i8
  IL_0011:  call       ""void System.Console.Write(long)""
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  sub
  IL_0019:  sizeof     ""S""
  IL_001f:  div
  IL_0020:  conv.i8
  IL_0021:  call       ""void System.Console.Write(long)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void PointerSubtraction_NonEmptyStruct()
        {
            var text = @"
using System;

unsafe struct S
{
    int x; //non-empty struct

    static void Main()
    {
        S* p = (S*)8;
        S* q = (S*)4;
        checked
        {
            Console.Write(p - q);
        }
        unchecked
        {
            Console.Write(p - q);
        }
    }
}
";

            // NOTE: don't use checked subtraction or division in either case (matches dev10).
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "11", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (S* V_0, //p
                S* V_1) //q
  IL_0000:  ldc.i4.8
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.4
  IL_0004:  conv.i
  IL_0005:  stloc.1
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  sub
  IL_0009:  sizeof     ""S""
  IL_000f:  div
  IL_0010:  conv.i8
  IL_0011:  call       ""void System.Console.Write(long)""
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  sub
  IL_0019:  sizeof     ""S""
  IL_001f:  div
  IL_0020:  conv.i8
  IL_0021:  call       ""void System.Console.Write(long)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void PointerSubtraction_ConstantSize()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        int* p = (int*)8; //size is known at compile-time
        int* q = (int*)4;
        checked
        {
            Console.Write(p - q);
        }
        unchecked
        {
            Console.Write(p - q);
        }
    }
}
";

            // NOTE: don't use checked subtraction or division in either case (matches dev10).
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "11", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int* V_0, //p
                int* V_1) //q
  IL_0000:  ldc.i4.8
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.4
  IL_0004:  conv.i
  IL_0005:  stloc.1
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  sub
  IL_0009:  ldc.i4.4
  IL_000a:  div
  IL_000b:  conv.i8
  IL_000c:  call       ""void System.Console.Write(long)""
  IL_0011:  ldloc.0
  IL_0012:  ldloc.1
  IL_0013:  sub
  IL_0014:  ldc.i4.4
  IL_0015:  div
  IL_0016:  conv.i8
  IL_0017:  call       ""void System.Console.Write(long)""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void PointerSubtraction_IntegerDivision()
        {
            var text = @"
using System;

unsafe struct S
{
    int x; //size = 4

    static void Main()
    {
        S* p1 = (S*)7; //size is known at compile-time
        S* p2 = (S*)9; //size is known at compile-time
        S* q = (S*)4;
        checked
        {
            Console.Write(p1 - q);
        }
        unchecked
        {
            Console.Write(p2 - q);
        }
    }
}
";

            // NOTE: don't use checked subtraction or division in either case (matches dev10).
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "01", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (S* V_0, //p1
                S* V_1, //p2
                S* V_2) //q
  IL_0000:  ldc.i4.7
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.s   9
  IL_0005:  conv.i
  IL_0006:  stloc.1
  IL_0007:  ldc.i4.4
  IL_0008:  conv.i
  IL_0009:  stloc.2
  IL_000a:  ldloc.0
  IL_000b:  ldloc.2
  IL_000c:  sub
  IL_000d:  sizeof     ""S""
  IL_0013:  div
  IL_0014:  conv.i8
  IL_0015:  call       ""void System.Console.Write(long)""
  IL_001a:  ldloc.1
  IL_001b:  ldloc.2
  IL_001c:  sub
  IL_001d:  sizeof     ""S""
  IL_0023:  div
  IL_0024:  conv.i8
  IL_0025:  call       ""void System.Console.Write(long)""
  IL_002a:  ret
}
");
        }

        [WorkItem(544155, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544155")]
        [Fact]
        public void SubtractPointerTypes()
        {
            var text = @"
using System;

class PointerArithmetic
{
    static unsafe void Main()
    {
        short ia1 = 10;
        short* ptr = &ia1;
        short* newPtr;
        newPtr = ptr - 2;        

        Console.WriteLine((int)(ptr - newPtr));
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "2", verify: Verification.Fails);
        }

        #endregion Pointer arithmetic tests

        #region Checked pointer arithmetic overflow tests

        // 0 - operation name (e.g. "Add")
        // 1 - pointed at type name (e.g. "S")
        // 2 - operator (e.g. "+")
        // 3 - checked/unchecked
        private const string CheckedNumericHelperTemplate = @"
unsafe static class Helper
{{
    public static void {0}Int({1}* p, int num, string description)
    {{
        {3}
        {{
            try
            {{
                p = p {2} num;
                Console.WriteLine(""{0}Int: No exception at {{0}} (value = {{1}})"",
                    description, (ulong)p);
            }}
            catch (OverflowException)
            {{
                Console.WriteLine(""{0}Int: Exception at {{0}}"", description);
            }}
        }}
    }}

    public static void {0}UInt({1}* p, uint num, string description)
    {{
        {3}
        {{
            try
            {{
                p = p {2} num;
                Console.WriteLine(""{0}UInt: No exception at {{0}} (value = {{1}})"",
                    description, (ulong)p);
            }}
            catch (OverflowException)
            {{
                Console.WriteLine(""{0}UInt: Exception at {{0}}"", description);
            }}
        }}
    }}

    public static void {0}Long({1}* p, long num, string description)
    {{
        {3}
        {{
            try
            {{
                p = p {2} num;
                Console.WriteLine(""{0}Long: No exception at {{0}} (value = {{1}})"",
                    description, (ulong)p);
            }}
            catch (OverflowException)
            {{
                Console.WriteLine(""{0}Long: Exception at {{0}}"", description);
            }}
        }}
    }}

    public static void {0}ULong({1}* p, ulong num, string description)
    {{
        {3}
        {{
            try
            {{
                p = p {2} num;
                Console.WriteLine(""{0}ULong: No exception at {{0}} (value = {{1}})"",
                    description, (ulong)p);
            }}
            catch (OverflowException)
            {{
                Console.WriteLine(""{0}ULong: Exception at {{0}}"", description);
            }}
        }}
    }}
}}
";

        private const string SizedStructs = @"
//sizeof SXX is 2 ^ XX
struct S00 { }
struct S01 { S00 a, b; }
struct S02 { S01 a, b; }
struct S03 { S02 a, b; }
struct S04 { S03 a, b; }
struct S05 { S04 a, b; }
struct S06 { S05 a, b; }
struct S07 { S06 a, b; }
struct S08 { S07 a, b; }
struct S09 { S08 a, b; }
struct S10 { S09 a, b; }
struct S11 { S10 a, b; }
struct S12 { S11 a, b; }
struct S13 { S12 a, b; }
struct S14 { S13 a, b; }
struct S15 { S14 a, b; }
struct S16 { S15 a, b; }
struct S17 { S16 a, b; }
struct S18 { S17 a, b; }
struct S19 { S18 a, b; }
struct S20 { S19 a, b; }
struct S21 { S20 a, b; }
struct S22 { S21 a, b; }
struct S23 { S22 a, b; }
struct S24 { S23 a, b; }
struct S25 { S24 a, b; }
struct S26 { S25 a, b; }
struct S27 { S26 a, b; }
//struct S28 { S27 a, b; } //Can't load type
//struct S29 { S28 a, b; } //Can't load type
//struct S30 { S29 a, b; } //Can't load type
//struct S31 { S30 a, b; } //Can't load type
";

        // 0 - pointed-at type
        private const string PositiveNumericAdditionCasesTemplate = @"
            Helper.AddInt(({0}*)0, int.MaxValue, ""0 + int.MaxValue"");
            Helper.AddInt(({0}*)1, int.MaxValue, ""1 + int.MaxValue"");
            Helper.AddInt(({0}*)int.MaxValue, 0, ""int.MaxValue + 0"");
            Helper.AddInt(({0}*)int.MaxValue, 1, ""int.MaxValue + 1"");

            //Helper.AddInt(({0}*)0, uint.MaxValue, ""0 + uint.MaxValue"");
            //Helper.AddInt(({0}*)1, uint.MaxValue, ""1 + uint.MaxValue"");
            Helper.AddInt(({0}*)uint.MaxValue, 0, ""uint.MaxValue + 0"");
            Helper.AddInt(({0}*)uint.MaxValue, 1, ""uint.MaxValue + 1"");

            //Helper.AddInt(({0}*)0, long.MaxValue, ""0 + long.MaxValue"");
            //Helper.AddInt(({0}*)1, long.MaxValue, ""1 + long.MaxValue"");
            //Helper.AddInt(({0}*)long.MaxValue, 0, ""long.MaxValue + 0"");
            //Helper.AddInt(({0}*)long.MaxValue, 1, ""long.MaxValue + 1"");

            //Helper.AddInt(({0}*)0, ulong.MaxValue, ""0 + ulong.MaxValue"");
            //Helper.AddInt(({0}*)1, ulong.MaxValue, ""1 + ulong.MaxValue"");
            //Helper.AddInt(({0}*)ulong.MaxValue, 0, ""ulong.MaxValue + 0"");
            //Helper.AddInt(({0}*)ulong.MaxValue, 1, ""ulong.MaxValue + 1"");

            Console.WriteLine();

            Helper.AddUInt(({0}*)0, int.MaxValue, ""0 + int.MaxValue"");
            Helper.AddUInt(({0}*)1, int.MaxValue, ""1 + int.MaxValue"");
            Helper.AddUInt(({0}*)int.MaxValue, 0, ""int.MaxValue + 0"");
            Helper.AddUInt(({0}*)int.MaxValue, 1, ""int.MaxValue + 1"");

            Helper.AddUInt(({0}*)0, uint.MaxValue, ""0 + uint.MaxValue"");
            Helper.AddUInt(({0}*)1, uint.MaxValue, ""1 + uint.MaxValue"");
            Helper.AddUInt(({0}*)uint.MaxValue, 0, ""uint.MaxValue + 0"");
            Helper.AddUInt(({0}*)uint.MaxValue, 1, ""uint.MaxValue + 1"");

            //Helper.AddUInt(({0}*)0, long.MaxValue, ""0 + long.MaxValue"");
            //Helper.AddUInt(({0}*)1, long.MaxValue, ""1 + long.MaxValue"");
            //Helper.AddUInt(({0}*)long.MaxValue, 0, ""long.MaxValue + 0"");
            //Helper.AddUInt(({0}*)long.MaxValue, 1, ""long.MaxValue + 1"");

            //Helper.AddUInt(({0}*)0, ulong.MaxValue, ""0 + ulong.MaxValue"");
            //Helper.AddUInt(({0}*)1, ulong.MaxValue, ""1 + ulong.MaxValue"");
            //Helper.AddUInt(({0}*)ulong.MaxValue, 0, ""ulong.MaxValue + 0"");
            //Helper.AddUInt(({0}*)ulong.MaxValue, 1, ""ulong.MaxValue + 1"");

            Console.WriteLine();

            Helper.AddLong(({0}*)0, int.MaxValue, ""0 + int.MaxValue"");
            Helper.AddLong(({0}*)1, int.MaxValue, ""1 + int.MaxValue"");
            Helper.AddLong(({0}*)int.MaxValue, 0, ""int.MaxValue + 0"");
            Helper.AddLong(({0}*)int.MaxValue, 1, ""int.MaxValue + 1"");

            Helper.AddLong(({0}*)0, uint.MaxValue, ""0 + uint.MaxValue"");
            Helper.AddLong(({0}*)1, uint.MaxValue, ""1 + uint.MaxValue"");
            Helper.AddLong(({0}*)uint.MaxValue, 0, ""uint.MaxValue + 0"");
            Helper.AddLong(({0}*)uint.MaxValue, 1, ""uint.MaxValue + 1"");

            Helper.AddLong(({0}*)0, long.MaxValue, ""0 + long.MaxValue"");
            Helper.AddLong(({0}*)1, long.MaxValue, ""1 + long.MaxValue"");
            //Helper.AddLong(({0}*)long.MaxValue, 0, ""long.MaxValue + 0"");
            //Helper.AddLong(({0}*)long.MaxValue, 1, ""long.MaxValue + 1"");

            //Helper.AddLong(({0}*)0, ulong.MaxValue, ""0 + ulong.MaxValue"");
            //Helper.AddLong(({0}*)1, ulong.MaxValue, ""1 + ulong.MaxValue"");
            //Helper.AddLong(({0}*)ulong.MaxValue, 0, ""ulong.MaxValue + 0"");
            //Helper.AddLong(({0}*)ulong.MaxValue, 1, ""ulong.MaxValue + 1"");

            Console.WriteLine();

            Helper.AddULong(({0}*)0, int.MaxValue, ""0 + int.MaxValue"");
            Helper.AddULong(({0}*)1, int.MaxValue, ""1 + int.MaxValue"");
            Helper.AddULong(({0}*)int.MaxValue, 0, ""int.MaxValue + 0"");
            Helper.AddULong(({0}*)int.MaxValue, 1, ""int.MaxValue + 1"");

            Helper.AddULong(({0}*)0, uint.MaxValue, ""0 + uint.MaxValue"");
            Helper.AddULong(({0}*)1, uint.MaxValue, ""1 + uint.MaxValue"");
            Helper.AddULong(({0}*)uint.MaxValue, 0, ""uint.MaxValue + 0"");
            Helper.AddULong(({0}*)uint.MaxValue, 1, ""uint.MaxValue + 1"");

            Helper.AddULong(({0}*)0, long.MaxValue, ""0 + long.MaxValue"");
            Helper.AddULong(({0}*)1, long.MaxValue, ""1 + long.MaxValue"");
            //Helper.AddULong(({0}*)long.MaxValue, 0, ""long.MaxValue + 0"");
            //Helper.AddULong(({0}*)long.MaxValue, 1, ""long.MaxValue + 1"");

            Helper.AddULong(({0}*)0, ulong.MaxValue, ""0 + ulong.MaxValue"");
            Helper.AddULong(({0}*)1, ulong.MaxValue, ""1 + ulong.MaxValue"");
            //Helper.AddULong(({0}*)ulong.MaxValue, 0, ""ulong.MaxValue + 0"");
            //Helper.AddULong(({0}*)ulong.MaxValue, 1, ""ulong.MaxValue + 1"");
";

        // 0 - pointed-at type
        private const string NegativeNumericAdditionCasesTemplate = @"
            Helper.AddInt(({0}*)0, -1, ""0 + (-1)"");
            Helper.AddInt(({0}*)0, int.MinValue, ""0 + int.MinValue"");
            //Helper.AddInt(({0}*)0, long.MinValue, ""0 + long.MinValue"");

            Console.WriteLine();

            Helper.AddLong(({0}*)0, -1, ""0 + (-1)"");
            Helper.AddLong(({0}*)0, int.MinValue, ""0 + int.MinValue"");
            Helper.AddLong(({0}*)0, long.MinValue, ""0 + long.MinValue"");
";

        // 0 - pointed-at type
        private const string PositiveNumericSubtractionCasesTemplate = @"
            Helper.SubInt(({0}*)0, 1, ""0 - 1"");
            Helper.SubInt(({0}*)0, int.MaxValue, ""0 - int.MaxValue"");
            //Helper.SubInt(({0}*)0, uint.MaxValue, ""0 - uint.MaxValue"");
            //Helper.SubInt(({0}*)0, long.MaxValue, ""0 - long.MaxValue"");
            //Helper.SubInt(({0}*)0, ulong.MaxValue, ""0 - ulong.MaxValue"");

            Console.WriteLine();

            Helper.SubUInt(({0}*)0, 1, ""0 - 1"");
            Helper.SubUInt(({0}*)0, int.MaxValue, ""0 - int.MaxValue"");
            Helper.SubUInt(({0}*)0, uint.MaxValue, ""0 - uint.MaxValue"");
            //Helper.SubUInt(({0}*)0, long.MaxValue, ""0 - long.MaxValue"");
            //Helper.SubUInt(({0}*)0, ulong.MaxValue, ""0 - ulong.MaxValue"");

            Console.WriteLine();

            Helper.SubLong(({0}*)0, 1, ""0 - 1"");
            Helper.SubLong(({0}*)0, int.MaxValue, ""0 - int.MaxValue"");
            Helper.SubLong(({0}*)0, uint.MaxValue, ""0 - uint.MaxValue"");
            Helper.SubLong(({0}*)0, long.MaxValue, ""0 - long.MaxValue"");
            //Helper.SubLong(({0}*)0, ulong.MaxValue, ""0 - ulong.MaxValue"");

            Console.WriteLine();

            Helper.SubULong(({0}*)0, 1, ""0 - 1"");
            Helper.SubULong(({0}*)0, int.MaxValue, ""0 - int.MaxValue"");
            Helper.SubULong(({0}*)0, uint.MaxValue, ""0 - uint.MaxValue"");
            Helper.SubULong(({0}*)0, long.MaxValue, ""0 - long.MaxValue"");
            Helper.SubULong(({0}*)0, ulong.MaxValue, ""0 - ulong.MaxValue"");
";

        // 0 - pointed-at type
        private const string NegativeNumericSubtractionCasesTemplate = @"
            Helper.SubInt(({0}*)0, -1, ""0 - -1"");
            Helper.SubInt(({0}*)0, int.MinValue, ""0 - int.MinValue"");
            Helper.SubInt(({0}*)0, -1 * int.MaxValue, ""0 - -int.MaxValue"");

            Console.WriteLine();

            Helper.SubLong(({0}*)0, -1L, ""0 - -1"");
            Helper.SubLong(({0}*)0, int.MinValue, ""0 - int.MinValue"");
            Helper.SubLong(({0}*)0, long.MinValue, ""0 - long.MinValue"");
            Helper.SubLong(({0}*)0, -1L * int.MaxValue, ""0 - -int.MaxValue"");
            Helper.SubLong(({0}*)0, -1L * uint.MaxValue, ""0 - -uint.MaxValue"");
            Helper.SubLong(({0}*)0, -1L * long.MaxValue, ""0 - -long.MaxValue"");
            Helper.SubLong(({0}*)0, -1L * long.MaxValue, ""0 - -ulong.MaxValue"");
            Helper.SubLong(({0}*)0, -1L * int.MinValue, ""0 - -int.MinValue"");
            //Helper.SubLong(({0}*)0, -1L * long.MinValue, ""0 - -long.MinValue"");
";

        private static string MakeNumericOverflowTest(string casesTemplate, string pointedAtType, string operationName, string @operator, string checkedness)
        {
            const string mainClassTemplate = @"
using System;

unsafe class C
{{
    static void Main()
    {{
        {0}
        {{
{1}
        }}
    }}
}}

{2}

{3}
";
            return string.Format(mainClassTemplate,
                checkedness,
                string.Format(casesTemplate, pointedAtType),
                string.Format(CheckedNumericHelperTemplate, operationName, pointedAtType, @operator, checkedness),
                SizedStructs);
        }

        // Positive numbers, size = 1
        [Fact]
        public void CheckedNumericAdditionOverflow1()
        {
            var text = MakeNumericOverflowTest(PositiveNumericAdditionCasesTemplate, "S00", "Add", "+", "checked");
            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: Exception at uint.MaxValue + 1

AddUInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddUInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddUInt: No exception at 0 + uint.MaxValue (value = 4294967295)
AddUInt: Exception at 1 + uint.MaxValue
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: Exception at uint.MaxValue + 1

AddLong: No exception at 0 + int.MaxValue (value = 2147483647)
AddLong: No exception at 1 + int.MaxValue (value = 2147483648)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483648)
AddLong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddLong: Exception at 1 + uint.MaxValue
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: Exception at uint.MaxValue + 1
AddLong: No exception at 0 + long.MaxValue (value = 4294967295)
AddLong: Exception at 1 + long.MaxValue

AddULong: No exception at 0 + int.MaxValue (value = 2147483647)
AddULong: No exception at 1 + int.MaxValue (value = 2147483648)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483648)
AddULong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddULong: Exception at 1 + uint.MaxValue
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: Exception at uint.MaxValue + 1
AddULong: No exception at 0 + long.MaxValue (value = 4294967295)
AddULong: Exception at 1 + long.MaxValue
AddULong: No exception at 0 + ulong.MaxValue (value = 4294967295)
AddULong: Exception at 1 + ulong.MaxValue
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: No exception at uint.MaxValue + 1 (value = 4294967296)

AddUInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddUInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddUInt: No exception at 0 + uint.MaxValue (value = 4294967295)
AddUInt: No exception at 1 + uint.MaxValue (value = 4294967296)
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: No exception at uint.MaxValue + 1 (value = 4294967296)

AddLong: No exception at 0 + int.MaxValue (value = 2147483647)
AddLong: No exception at 1 + int.MaxValue (value = 2147483648)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483648)
AddLong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddLong: No exception at 1 + uint.MaxValue (value = 4294967296)
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: No exception at uint.MaxValue + 1 (value = 4294967296)
AddLong: No exception at 0 + long.MaxValue (value = 9223372036854775807)
AddLong: No exception at 1 + long.MaxValue (value = 9223372036854775808)

AddULong: No exception at 0 + int.MaxValue (value = 2147483647)
AddULong: No exception at 1 + int.MaxValue (value = 2147483648)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483648)
AddULong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddULong: No exception at 1 + uint.MaxValue (value = 4294967296)
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: No exception at uint.MaxValue + 1 (value = 4294967296)
AddULong: No exception at 0 + long.MaxValue (value = 9223372036854775807)
AddULong: No exception at 1 + long.MaxValue (value = 9223372036854775808)
AddULong: No exception at 0 + ulong.MaxValue (value = 18446744073709551615)
AddULong: Exception at 1 + ulong.MaxValue
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Positive numbers, size = 4
        [Fact]
        public void CheckedNumericAdditionOverflow2()
        {
            var text = MakeNumericOverflowTest(PositiveNumericAdditionCasesTemplate, "S02", "Add", "+", "checked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: Exception at 0 + int.MaxValue
AddInt: Exception at 1 + int.MaxValue
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: Exception at uint.MaxValue + 1

AddUInt: No exception at 0 + int.MaxValue (value = 4294967292)
AddUInt: No exception at 1 + int.MaxValue (value = 4294967293)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddUInt: No exception at 0 + uint.MaxValue (value = 4294967292)
AddUInt: No exception at 1 + uint.MaxValue (value = 4294967293)
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: Exception at uint.MaxValue + 1

AddLong: No exception at 0 + int.MaxValue (value = 4294967292)
AddLong: No exception at 1 + int.MaxValue (value = 4294967293)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483651)
AddLong: No exception at 0 + uint.MaxValue (value = 4294967292)
AddLong: No exception at 1 + uint.MaxValue (value = 4294967293)
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: Exception at uint.MaxValue + 1
AddLong: Exception at 0 + long.MaxValue
AddLong: Exception at 1 + long.MaxValue

AddULong: No exception at 0 + int.MaxValue (value = 4294967292)
AddULong: No exception at 1 + int.MaxValue (value = 4294967293)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483651)
AddULong: No exception at 0 + uint.MaxValue (value = 4294967292)
AddULong: No exception at 1 + uint.MaxValue (value = 4294967293)
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: Exception at uint.MaxValue + 1
AddULong: Exception at 0 + long.MaxValue
AddULong: Exception at 1 + long.MaxValue
AddULong: Exception at 0 + ulong.MaxValue
AddULong: Exception at 1 + ulong.MaxValue
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + int.MaxValue (value = 8589934588)
AddInt: No exception at 1 + int.MaxValue (value = 8589934589)
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: No exception at uint.MaxValue + 1 (value = 4294967299)

AddUInt: No exception at 0 + int.MaxValue (value = 8589934588)
AddUInt: No exception at 1 + int.MaxValue (value = 8589934589)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddUInt: No exception at 0 + uint.MaxValue (value = 17179869180)
AddUInt: No exception at 1 + uint.MaxValue (value = 17179869181)
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: No exception at uint.MaxValue + 1 (value = 4294967299)

AddLong: No exception at 0 + int.MaxValue (value = 8589934588)
AddLong: No exception at 1 + int.MaxValue (value = 8589934589)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483651)
AddLong: No exception at 0 + uint.MaxValue (value = 17179869180)
AddLong: No exception at 1 + uint.MaxValue (value = 17179869181)
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: No exception at uint.MaxValue + 1 (value = 4294967299)
AddLong: Exception at 0 + long.MaxValue
AddLong: Exception at 1 + long.MaxValue

AddULong: No exception at 0 + int.MaxValue (value = 8589934588)
AddULong: No exception at 1 + int.MaxValue (value = 8589934589)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483651)
AddULong: No exception at 0 + uint.MaxValue (value = 17179869180)
AddULong: No exception at 1 + uint.MaxValue (value = 17179869181)
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: No exception at uint.MaxValue + 1 (value = 4294967299)
AddULong: Exception at 0 + long.MaxValue
AddULong: Exception at 1 + long.MaxValue
AddULong: Exception at 0 + ulong.MaxValue
AddULong: Exception at 1 + ulong.MaxValue
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Negative numbers, size = 1
        [Fact]
        public void CheckedNumericAdditionOverflow3()
        {
            var text = MakeNumericOverflowTest(NegativeNumericAdditionCasesTemplate, "S00", "Add", "+", "checked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 4294967295)
AddInt: No exception at 0 + int.MinValue (value = 2147483648)

AddLong: No exception at 0 + (-1) (value = 4294967295)
AddLong: No exception at 0 + int.MinValue (value = 2147483648)
AddLong: No exception at 0 + long.MinValue (value = 0)
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 18446744073709551615)
AddInt: No exception at 0 + int.MinValue (value = 18446744071562067968)

AddLong: No exception at 0 + (-1) (value = 18446744073709551615)
AddLong: No exception at 0 + int.MinValue (value = 18446744071562067968)
AddLong: No exception at 0 + long.MinValue (value = 9223372036854775808)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: expectedOutput);
        }

        // Negative numbers, size = 4
        [Fact]
        public void CheckedNumericAdditionOverflow4()
        {
            var text = MakeNumericOverflowTest(NegativeNumericAdditionCasesTemplate, "S02", "Add", "+", "checked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 4294967292)
AddInt: Exception at 0 + int.MinValue

AddLong: No exception at 0 + (-1) (value = 4294967292)
AddLong: No exception at 0 + int.MinValue (value = 0)
AddLong: Exception at 0 + long.MinValue
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 18446744073709551612)
AddInt: No exception at 0 + int.MinValue (value = 18446744065119617024)

AddLong: No exception at 0 + (-1) (value = 18446744073709551612)
AddLong: No exception at 0 + int.MinValue (value = 18446744065119617024)
AddLong: Exception at 0 + long.MinValue
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Positive numbers, size = 1
        [Fact]
        public void CheckedNumericSubtractionOverflow1()
        {
            var text = MakeNumericOverflowTest(PositiveNumericSubtractionCasesTemplate, "S00", "Sub", "-", "checked");

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"
SubInt: Exception at 0 - 1
SubInt: Exception at 0 - int.MaxValue

SubUInt: Exception at 0 - 1
SubUInt: Exception at 0 - int.MaxValue
SubUInt: Exception at 0 - uint.MaxValue

SubLong: Exception at 0 - 1
SubLong: Exception at 0 - int.MaxValue
SubLong: Exception at 0 - uint.MaxValue
SubLong: Exception at 0 - long.MaxValue

SubULong: Exception at 0 - 1
SubULong: Exception at 0 - int.MaxValue
SubULong: Exception at 0 - uint.MaxValue
SubULong: Exception at 0 - long.MaxValue
SubULong: Exception at 0 - ulong.MaxValue
");
        }

        // Positive numbers, size = 4
        [Fact]
        public void CheckedNumericSubtractionOverflow2()
        {
            var text = MakeNumericOverflowTest(PositiveNumericSubtractionCasesTemplate, "S02", "Sub", "-", "checked");

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"
SubInt: Exception at 0 - 1
SubInt: Exception at 0 - int.MaxValue

SubUInt: Exception at 0 - 1
SubUInt: Exception at 0 - int.MaxValue
SubUInt: Exception at 0 - uint.MaxValue

SubLong: Exception at 0 - 1
SubLong: Exception at 0 - int.MaxValue
SubLong: Exception at 0 - uint.MaxValue
SubLong: Exception at 0 - long.MaxValue

SubULong: Exception at 0 - 1
SubULong: Exception at 0 - int.MaxValue
SubULong: Exception at 0 - uint.MaxValue
SubULong: Exception at 0 - long.MaxValue
SubULong: Exception at 0 - ulong.MaxValue
");
        }

        // Negative numbers, size = 1
        [Fact]
        public void CheckedNumericSubtractionOverflow3()
        {
            var text = MakeNumericOverflowTest(NegativeNumericSubtractionCasesTemplate, "S00", "Sub", "-", "checked");
            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
SubInt: Exception at 0 - -1
SubInt: Exception at 0 - int.MinValue
SubInt: Exception at 0 - -int.MaxValue

SubLong: Exception at 0 - -1
SubLong: Exception at 0 - int.MinValue
SubLong: No exception at 0 - long.MinValue (value = 0)
SubLong: Exception at 0 - -int.MaxValue
SubLong: Exception at 0 - -uint.MaxValue
SubLong: Exception at 0 - -long.MaxValue
SubLong: Exception at 0 - -ulong.MaxValue
SubLong: Exception at 0 - -int.MinValue
";
            }
            else
            {
                expectedOutput = @"
SubInt: Exception at 0 - -1
SubInt: Exception at 0 - int.MinValue
SubInt: Exception at 0 - -int.MaxValue

SubLong: Exception at 0 - -1
SubLong: Exception at 0 - int.MinValue
SubLong: Exception at 0 - long.MinValue
SubLong: Exception at 0 - -int.MaxValue
SubLong: Exception at 0 - -uint.MaxValue
SubLong: Exception at 0 - -long.MaxValue
SubLong: Exception at 0 - -ulong.MaxValue
SubLong: Exception at 0 - -int.MinValue
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Negative numbers, size = 4
        [Fact]
        public void CheckedNumericSubtractionOverflow4()
        {
            var text = MakeNumericOverflowTest(NegativeNumericSubtractionCasesTemplate, "S02", "Sub", "-", "checked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
SubInt: Exception at 0 - -1
SubInt: Exception at 0 - int.MinValue
SubInt: Exception at 0 - -int.MaxValue

SubLong: Exception at 0 - -1
SubLong: No exception at 0 - int.MinValue (value = 0)
SubLong: Exception at 0 - long.MinValue
SubLong: Exception at 0 - -int.MaxValue
SubLong: Exception at 0 - -uint.MaxValue
SubLong: Exception at 0 - -long.MaxValue
SubLong: Exception at 0 - -ulong.MaxValue
SubLong: No exception at 0 - -int.MinValue (value = 0)
";
            }
            else
            {
                expectedOutput = @"
SubInt: Exception at 0 - -1
SubInt: Exception at 0 - int.MinValue
SubInt: Exception at 0 - -int.MaxValue

SubLong: Exception at 0 - -1
SubLong: Exception at 0 - int.MinValue
SubLong: Exception at 0 - long.MinValue
SubLong: Exception at 0 - -int.MaxValue
SubLong: Exception at 0 - -uint.MaxValue
SubLong: Exception at 0 - -long.MaxValue
SubLong: Exception at 0 - -ulong.MaxValue
SubLong: Exception at 0 - -int.MinValue
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        [Fact]
        public void CheckedNumericSubtractionQuirk()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        checked
        {
            S* p;
            p = (S*)0 + (-1);
            System.Console.WriteLine(""No exception from addition"");
            try
            {
                p = (S*)0 - 1;
            }
            catch (OverflowException)
            {
                System.Console.WriteLine(""Exception from subtraction"");
            }
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Passes, expectedOutput: @"
No exception from addition
Exception from subtraction
");
        }

        [Fact]
        public void CheckedNumericAdditionQuirk()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        checked
        {
            S* p;
            p = (S*)1 + int.MaxValue;
            System.Console.WriteLine(""No exception for pointer + int"");
            try
            {
                p = int.MaxValue + (S*)1;
            }
            catch (OverflowException)
            {
                System.Console.WriteLine(""Exception for int + pointer"");
            }
        }
    }
}
";
            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
No exception for pointer + int
Exception for int + pointer
";
            }
            else
            {
                expectedOutput = @"
No exception for pointer + int
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Passes);
        }

        [Fact]
        public void CheckedPointerSubtractionQuirk()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S* p = (S*)uint.MinValue;
        S* q = (S*)uint.MaxValue;
        checked
        {
            Console.Write(p - q);
        }
        unchecked
        {
            Console.Write(p - q);
        }
    }
}
";
            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"11";
            }
            else
            {
                expectedOutput = @"-4294967295-4294967295";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        [Fact]
        public void CheckedPointerElementAccessQuirk()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        fixed (byte* p = new byte[2])
        {
            p[0] = 12;

            // Take a pointer to the second element of the array.
            byte* q = p + 1;

            // Compute the offset that will wrap around all the way to the preceding byte of memory.
            // We do this so that we can overflow, but still end up in valid memory.
            ulong offset = sizeof(IntPtr) == sizeof(int) ? uint.MaxValue : ulong.MaxValue;

            checked
            {
                Console.WriteLine(q[offset]);
                System.Console.WriteLine(""No exception for element access"");
                try
                {
                    Console.WriteLine(*(q + offset));
                }
                catch (OverflowException)
                {
                    System.Console.WriteLine(""Exception for add-then-dereference"");
                }
            }
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"
12
No exception for element access
Exception for add-then-dereference
");
        }

        #endregion Checked pointer arithmetic overflow tests

        #region Unchecked pointer arithmetic overflow tests

        // Positive numbers, size = 1
        [Fact]
        public void UncheckedNumericAdditionOverflow1()
        {
            var text = MakeNumericOverflowTest(PositiveNumericAdditionCasesTemplate, "S00", "Add", "+", "unchecked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: No exception at uint.MaxValue + 1 (value = 0)

AddUInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddUInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddUInt: No exception at 0 + uint.MaxValue (value = 4294967295)
AddUInt: No exception at 1 + uint.MaxValue (value = 0)
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: No exception at uint.MaxValue + 1 (value = 0)

AddLong: No exception at 0 + int.MaxValue (value = 2147483647)
AddLong: No exception at 1 + int.MaxValue (value = 2147483648)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483648)
AddLong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddLong: No exception at 1 + uint.MaxValue (value = 0)
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: No exception at uint.MaxValue + 1 (value = 0)
AddLong: No exception at 0 + long.MaxValue (value = 4294967295)
AddLong: No exception at 1 + long.MaxValue (value = 0)

AddULong: No exception at 0 + int.MaxValue (value = 2147483647)
AddULong: No exception at 1 + int.MaxValue (value = 2147483648)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483648)
AddULong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddULong: No exception at 1 + uint.MaxValue (value = 0)
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: No exception at uint.MaxValue + 1 (value = 0)
AddULong: No exception at 0 + long.MaxValue (value = 4294967295)
AddULong: No exception at 1 + long.MaxValue (value = 0)
AddULong: No exception at 0 + ulong.MaxValue (value = 4294967295)
AddULong: No exception at 1 + ulong.MaxValue (value = 0)
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: No exception at uint.MaxValue + 1 (value = 4294967296)

AddUInt: No exception at 0 + int.MaxValue (value = 2147483647)
AddUInt: No exception at 1 + int.MaxValue (value = 2147483648)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483648)
AddUInt: No exception at 0 + uint.MaxValue (value = 4294967295)
AddUInt: No exception at 1 + uint.MaxValue (value = 4294967296)
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: No exception at uint.MaxValue + 1 (value = 4294967296)

AddLong: No exception at 0 + int.MaxValue (value = 2147483647)
AddLong: No exception at 1 + int.MaxValue (value = 2147483648)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483648)
AddLong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddLong: No exception at 1 + uint.MaxValue (value = 4294967296)
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: No exception at uint.MaxValue + 1 (value = 4294967296)
AddLong: No exception at 0 + long.MaxValue (value = 9223372036854775807)
AddLong: No exception at 1 + long.MaxValue (value = 9223372036854775808)

AddULong: No exception at 0 + int.MaxValue (value = 2147483647)
AddULong: No exception at 1 + int.MaxValue (value = 2147483648)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483648)
AddULong: No exception at 0 + uint.MaxValue (value = 4294967295)
AddULong: No exception at 1 + uint.MaxValue (value = 4294967296)
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: No exception at uint.MaxValue + 1 (value = 4294967296)
AddULong: No exception at 0 + long.MaxValue (value = 9223372036854775807)
AddULong: No exception at 1 + long.MaxValue (value = 9223372036854775808)
AddULong: No exception at 0 + ulong.MaxValue (value = 18446744073709551615)
AddULong: No exception at 1 + ulong.MaxValue (value = 0)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Positive numbers, size = 4
        [Fact]
        public void UncheckedNumericAdditionOverflow2()
        {
            var text = MakeNumericOverflowTest(PositiveNumericAdditionCasesTemplate, "S02", "Add", "+", "unchecked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: No exception at 0 + int.MaxValue (value = 4294967292)
AddInt: No exception at 1 + int.MaxValue (value = 4294967293)
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: No exception at uint.MaxValue + 1 (value = 3)

AddUInt: No exception at 0 + int.MaxValue (value = 4294967292)
AddUInt: No exception at 1 + int.MaxValue (value = 4294967293)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddUInt: No exception at 0 + uint.MaxValue (value = 4294967292)
AddUInt: No exception at 1 + uint.MaxValue (value = 4294967293)
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: No exception at uint.MaxValue + 1 (value = 3)

AddLong: No exception at 0 + int.MaxValue (value = 4294967292)
AddLong: No exception at 1 + int.MaxValue (value = 4294967293)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483651)
AddLong: No exception at 0 + uint.MaxValue (value = 4294967292)
AddLong: No exception at 1 + uint.MaxValue (value = 4294967293)
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: No exception at uint.MaxValue + 1 (value = 3)
AddLong: No exception at 0 + long.MaxValue (value = 4294967292)
AddLong: No exception at 1 + long.MaxValue (value = 4294967293)

AddULong: No exception at 0 + int.MaxValue (value = 4294967292)
AddULong: No exception at 1 + int.MaxValue (value = 4294967293)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483651)
AddULong: No exception at 0 + uint.MaxValue (value = 4294967292)
AddULong: No exception at 1 + uint.MaxValue (value = 4294967293)
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: No exception at uint.MaxValue + 1 (value = 3)
AddULong: No exception at 0 + long.MaxValue (value = 4294967292)
AddULong: No exception at 1 + long.MaxValue (value = 4294967293)
AddULong: No exception at 0 + ulong.MaxValue (value = 4294967292)
AddULong: No exception at 1 + ulong.MaxValue (value = 4294967293)
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + int.MaxValue (value = 8589934588)
AddInt: No exception at 1 + int.MaxValue (value = 8589934589)
AddInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddInt: No exception at uint.MaxValue + 1 (value = 4294967299)

AddUInt: No exception at 0 + int.MaxValue (value = 8589934588)
AddUInt: No exception at 1 + int.MaxValue (value = 8589934589)
AddUInt: No exception at int.MaxValue + 0 (value = 2147483647)
AddUInt: No exception at int.MaxValue + 1 (value = 2147483651)
AddUInt: No exception at 0 + uint.MaxValue (value = 17179869180)
AddUInt: No exception at 1 + uint.MaxValue (value = 17179869181)
AddUInt: No exception at uint.MaxValue + 0 (value = 4294967295)
AddUInt: No exception at uint.MaxValue + 1 (value = 4294967299)

AddLong: No exception at 0 + int.MaxValue (value = 8589934588)
AddLong: No exception at 1 + int.MaxValue (value = 8589934589)
AddLong: No exception at int.MaxValue + 0 (value = 2147483647)
AddLong: No exception at int.MaxValue + 1 (value = 2147483651)
AddLong: No exception at 0 + uint.MaxValue (value = 17179869180)
AddLong: No exception at 1 + uint.MaxValue (value = 17179869181)
AddLong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddLong: No exception at uint.MaxValue + 1 (value = 4294967299)
AddLong: No exception at 0 + long.MaxValue (value = 18446744073709551612)
AddLong: No exception at 1 + long.MaxValue (value = 18446744073709551613)

AddULong: No exception at 0 + int.MaxValue (value = 8589934588)
AddULong: No exception at 1 + int.MaxValue (value = 8589934589)
AddULong: No exception at int.MaxValue + 0 (value = 2147483647)
AddULong: No exception at int.MaxValue + 1 (value = 2147483651)
AddULong: No exception at 0 + uint.MaxValue (value = 17179869180)
AddULong: No exception at 1 + uint.MaxValue (value = 17179869181)
AddULong: No exception at uint.MaxValue + 0 (value = 4294967295)
AddULong: No exception at uint.MaxValue + 1 (value = 4294967299)
AddULong: No exception at 0 + long.MaxValue (value = 18446744073709551612)
AddULong: No exception at 1 + long.MaxValue (value = 18446744073709551613)
AddULong: No exception at 0 + ulong.MaxValue (value = 18446744073709551612)
AddULong: No exception at 1 + ulong.MaxValue (value = 18446744073709551613)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Negative numbers, size = 1
        [Fact]
        public void UncheckedNumericAdditionOverflow3()
        {
            var text = MakeNumericOverflowTest(NegativeNumericAdditionCasesTemplate, "S00", "Add", "+", "unchecked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 4294967295)
AddInt: No exception at 0 + int.MinValue (value = 2147483648)

AddLong: No exception at 0 + (-1) (value = 4294967295)
AddLong: No exception at 0 + int.MinValue (value = 2147483648)
AddLong: No exception at 0 + long.MinValue (value = 0)
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 18446744073709551615)
AddInt: No exception at 0 + int.MinValue (value = 18446744071562067968)

AddLong: No exception at 0 + (-1) (value = 18446744073709551615)
AddLong: No exception at 0 + int.MinValue (value = 18446744071562067968)
AddLong: No exception at 0 + long.MinValue (value = 9223372036854775808)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Negative numbers, size = 4
        [Fact]
        public void UncheckedNumericAdditionOverflow4()
        {
            var text = MakeNumericOverflowTest(NegativeNumericAdditionCasesTemplate, "S02", "Add", "+", "unchecked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 4294967292)
AddInt: No exception at 0 + int.MinValue (value = 0)

AddLong: No exception at 0 + (-1) (value = 4294967292)
AddLong: No exception at 0 + int.MinValue (value = 0)
AddLong: No exception at 0 + long.MinValue (value = 0)
";
            }
            else
            {
                expectedOutput = @"
AddInt: No exception at 0 + (-1) (value = 18446744073709551612)
AddInt: No exception at 0 + int.MinValue (value = 18446744065119617024)

AddLong: No exception at 0 + (-1) (value = 18446744073709551612)
AddLong: No exception at 0 + int.MinValue (value = 18446744065119617024)
AddLong: No exception at 0 + long.MinValue (value = 0)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Positive numbers, size = 1
        [Fact]
        public void UncheckedNumericSubtractionOverflow1()
        {
            var text = MakeNumericOverflowTest(PositiveNumericSubtractionCasesTemplate, "S00", "Sub", "-", "unchecked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
SubInt: No exception at 0 - 1 (value = 4294967295)
SubInt: No exception at 0 - int.MaxValue (value = 2147483649)

SubUInt: No exception at 0 - 1 (value = 4294967295)
SubUInt: No exception at 0 - int.MaxValue (value = 2147483649)
SubUInt: No exception at 0 - uint.MaxValue (value = 1)

SubLong: No exception at 0 - 1 (value = 4294967295)
SubLong: No exception at 0 - int.MaxValue (value = 2147483649)
SubLong: No exception at 0 - uint.MaxValue (value = 1)
SubLong: No exception at 0 - long.MaxValue (value = 1)

SubULong: No exception at 0 - 1 (value = 4294967295)
SubULong: No exception at 0 - int.MaxValue (value = 2147483649)
SubULong: No exception at 0 - uint.MaxValue (value = 1)
SubULong: No exception at 0 - long.MaxValue (value = 1)
SubULong: No exception at 0 - ulong.MaxValue (value = 1)
";
            }
            else
            {
                expectedOutput = @"
SubInt: No exception at 0 - 1 (value = 18446744073709551615)
SubInt: No exception at 0 - int.MaxValue (value = 18446744071562067969)

SubUInt: No exception at 0 - 1 (value = 18446744073709551615)
SubUInt: No exception at 0 - int.MaxValue (value = 18446744071562067969)
SubUInt: No exception at 0 - uint.MaxValue (value = 18446744069414584321)

SubLong: No exception at 0 - 1 (value = 18446744073709551615)
SubLong: No exception at 0 - int.MaxValue (value = 18446744071562067969)
SubLong: No exception at 0 - uint.MaxValue (value = 18446744069414584321)
SubLong: No exception at 0 - long.MaxValue (value = 9223372036854775809)

SubULong: No exception at 0 - 1 (value = 18446744073709551615)
SubULong: No exception at 0 - int.MaxValue (value = 18446744071562067969)
SubULong: No exception at 0 - uint.MaxValue (value = 18446744069414584321)
SubULong: No exception at 0 - long.MaxValue (value = 9223372036854775809)
SubULong: No exception at 0 - ulong.MaxValue (value = 1)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Positive numbers, size = 4
        [Fact]
        public void UncheckedNumericSubtractionOverflow2()
        {
            var text = MakeNumericOverflowTest(PositiveNumericSubtractionCasesTemplate, "S02", "Sub", "-", "unchecked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
SubInt: No exception at 0 - 1 (value = 4294967292)
SubInt: No exception at 0 - int.MaxValue (value = 4)

SubUInt: No exception at 0 - 1 (value = 4294967292)
SubUInt: No exception at 0 - int.MaxValue (value = 4)
SubUInt: No exception at 0 - uint.MaxValue (value = 4)

SubLong: No exception at 0 - 1 (value = 4294967292)
SubLong: No exception at 0 - int.MaxValue (value = 4)
SubLong: No exception at 0 - uint.MaxValue (value = 4)
SubLong: No exception at 0 - long.MaxValue (value = 4)

SubULong: No exception at 0 - 1 (value = 4294967292)
SubULong: No exception at 0 - int.MaxValue (value = 4)
SubULong: No exception at 0 - uint.MaxValue (value = 4)
SubULong: No exception at 0 - long.MaxValue (value = 4)
SubULong: No exception at 0 - ulong.MaxValue (value = 4)
";
            }
            else
            {
                expectedOutput = @"
SubInt: No exception at 0 - 1 (value = 18446744073709551612)
SubInt: No exception at 0 - int.MaxValue (value = 18446744065119617028)

SubUInt: No exception at 0 - 1 (value = 18446744073709551612)
SubUInt: No exception at 0 - int.MaxValue (value = 18446744065119617028)
SubUInt: No exception at 0 - uint.MaxValue (value = 18446744056529682436)

SubLong: No exception at 0 - 1 (value = 18446744073709551612)
SubLong: No exception at 0 - int.MaxValue (value = 18446744065119617028)
SubLong: No exception at 0 - uint.MaxValue (value = 18446744056529682436)
SubLong: No exception at 0 - long.MaxValue (value = 4)

SubULong: No exception at 0 - 1 (value = 18446744073709551612)
SubULong: No exception at 0 - int.MaxValue (value = 18446744065119617028)
SubULong: No exception at 0 - uint.MaxValue (value = 18446744056529682436)
SubULong: No exception at 0 - long.MaxValue (value = 4)
SubULong: No exception at 0 - ulong.MaxValue (value = 4)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Negative numbers, size = 1
        [Fact]
        public void UncheckedNumericSubtractionOverflow3()
        {
            var text = MakeNumericOverflowTest(NegativeNumericSubtractionCasesTemplate, "S00", "Sub", "-", "unchecked");

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
SubInt: No exception at 0 - -1 (value = 1)
SubInt: No exception at 0 - int.MinValue (value = 2147483648)
SubInt: No exception at 0 - -int.MaxValue (value = 2147483647)

SubLong: No exception at 0 - -1 (value = 1)
SubLong: No exception at 0 - int.MinValue (value = 2147483648)
SubLong: No exception at 0 - long.MinValue (value = 0)
SubLong: No exception at 0 - -int.MaxValue (value = 2147483647)
SubLong: No exception at 0 - -uint.MaxValue (value = 4294967295)
SubLong: No exception at 0 - -long.MaxValue (value = 4294967295)
SubLong: No exception at 0 - -ulong.MaxValue (value = 4294967295)
SubLong: No exception at 0 - -int.MinValue (value = 2147483648)
";
            }
            else
            {
                expectedOutput = @"
SubInt: No exception at 0 - -1 (value = 1)
SubInt: No exception at 0 - int.MinValue (value = 2147483648)
SubInt: No exception at 0 - -int.MaxValue (value = 2147483647)

SubLong: No exception at 0 - -1 (value = 1)
SubLong: No exception at 0 - int.MinValue (value = 2147483648)
SubLong: No exception at 0 - long.MinValue (value = 9223372036854775808)
SubLong: No exception at 0 - -int.MaxValue (value = 2147483647)
SubLong: No exception at 0 - -uint.MaxValue (value = 4294967295)
SubLong: No exception at 0 - -long.MaxValue (value = 9223372036854775807)
SubLong: No exception at 0 - -ulong.MaxValue (value = 9223372036854775807)
SubLong: No exception at 0 - -int.MinValue (value = 18446744071562067968)
";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        // Negative numbers, size = 4
        [Fact]
        public void UncheckedNumericSubtractionOverflow4()
        {
            var text = MakeNumericOverflowTest(NegativeNumericSubtractionCasesTemplate, "S02", "Sub", "-", "unchecked");
            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
SubInt: No exception at 0 - -1 (value = 4)
SubInt: No exception at 0 - int.MinValue (value = 0)
SubInt: No exception at 0 - -int.MaxValue (value = 4294967292)

SubLong: No exception at 0 - -1 (value = 4)
SubLong: No exception at 0 - int.MinValue (value = 0)
SubLong: No exception at 0 - long.MinValue (value = 0)
SubLong: No exception at 0 - -int.MaxValue (value = 4294967292)
SubLong: No exception at 0 - -uint.MaxValue (value = 4294967292)
SubLong: No exception at 0 - -long.MaxValue (value = 4294967292)
SubLong: No exception at 0 - -ulong.MaxValue (value = 4294967292)
SubLong: No exception at 0 - -int.MinValue (value = 0)";
            }
            else
            {
                expectedOutput = @"
SubInt: No exception at 0 - -1 (value = 4)
SubInt: No exception at 0 - int.MinValue (value = 8589934592)
SubInt: No exception at 0 - -int.MaxValue (value = 8589934588)

SubLong: No exception at 0 - -1 (value = 4)
SubLong: No exception at 0 - int.MinValue (value = 8589934592)
SubLong: No exception at 0 - long.MinValue (value = 0)
SubLong: No exception at 0 - -int.MaxValue (value = 8589934588)
SubLong: No exception at 0 - -uint.MaxValue (value = 17179869180)
SubLong: No exception at 0 - -long.MaxValue (value = 18446744073709551612)
SubLong: No exception at 0 - -ulong.MaxValue (value = 18446744073709551612)
SubLong: No exception at 0 - -int.MinValue (value = 18446744065119617024)";
            }

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        #endregion Unchecked pointer arithmetic overflow tests

        #region Pointer comparison tests

        [Fact]
        public void PointerComparisonSameType()
        {
            var text = @"
using System;

unsafe struct S
{
    static void Main()
    {
        S* p = (S*)0;
        S* q = (S*)1;

        unchecked
        {
            Write(p == q);
            Write(p != q);
            Write(p <= q);
            Write(p >= q);
            Write(p < q);
            Write(p > q);
        }

        checked
        {
            Write(p == q);
            Write(p != q);
            Write(p <= q);
            Write(p >= q);
            Write(p < q);
            Write(p > q);
        }
    }

    static void Write(bool b)
    {
        Console.Write(b ? 1 : 0);
    }
}
";
            // NOTE: all comparisons unsigned.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "011010011010", verify: Verification.Fails).VerifyIL("S.Main", @"
{
  // Code size      133 (0x85)
  .maxstack  2
  .locals init (S* V_0, //p
                S* V_1) //q
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  conv.i
  IL_0005:  stloc.1
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  ceq
  IL_000a:  call       ""void S.Write(bool)""
  IL_000f:  ldloc.0
  IL_0010:  ldloc.1
  IL_0011:  ceq
  IL_0013:  ldc.i4.0
  IL_0014:  ceq
  IL_0016:  call       ""void S.Write(bool)""
  IL_001b:  ldloc.0
  IL_001c:  ldloc.1
  IL_001d:  cgt.un
  IL_001f:  ldc.i4.0
  IL_0020:  ceq
  IL_0022:  call       ""void S.Write(bool)""
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  clt.un
  IL_002b:  ldc.i4.0
  IL_002c:  ceq
  IL_002e:  call       ""void S.Write(bool)""
  IL_0033:  ldloc.0
  IL_0034:  ldloc.1
  IL_0035:  clt.un
  IL_0037:  call       ""void S.Write(bool)""
  IL_003c:  ldloc.0
  IL_003d:  ldloc.1
  IL_003e:  cgt.un
  IL_0040:  call       ""void S.Write(bool)""
  IL_0045:  ldloc.0
  IL_0046:  ldloc.1
  IL_0047:  ceq
  IL_0049:  call       ""void S.Write(bool)""
  IL_004e:  ldloc.0
  IL_004f:  ldloc.1
  IL_0050:  ceq
  IL_0052:  ldc.i4.0
  IL_0053:  ceq
  IL_0055:  call       ""void S.Write(bool)""
  IL_005a:  ldloc.0
  IL_005b:  ldloc.1
  IL_005c:  cgt.un
  IL_005e:  ldc.i4.0
  IL_005f:  ceq
  IL_0061:  call       ""void S.Write(bool)""
  IL_0066:  ldloc.0
  IL_0067:  ldloc.1
  IL_0068:  clt.un
  IL_006a:  ldc.i4.0
  IL_006b:  ceq
  IL_006d:  call       ""void S.Write(bool)""
  IL_0072:  ldloc.0
  IL_0073:  ldloc.1
  IL_0074:  clt.un
  IL_0076:  call       ""void S.Write(bool)""
  IL_007b:  ldloc.0
  IL_007c:  ldloc.1
  IL_007d:  cgt.un
  IL_007f:  call       ""void S.Write(bool)""
  IL_0084:  ret
}
");
        }

        [Fact, WorkItem(49639, "https://github.com/dotnet/roslyn/issues/49639")]
        public void CompareToNullWithNestedUnconstrainedTypeParameter()
        {
            var verifier = CompileAndVerify(@"
using System;
unsafe
{
    test<int>(null);
    S<int> s = default;
    test<int>(&s);

    static void test<T>(S<T>* s)
    {
        Console.WriteLine(s == null);
        Console.WriteLine(s is null);
    }
}

struct S<T> {}
", options: TestOptions.UnsafeReleaseExe, expectedOutput: @"
True
True
False
False", verify: Verification.Skipped);

            verifier.VerifyIL("Program.<<Main>$>g__test|0_0<T>(S<T>*)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  conv.u
  IL_0003:  ceq
  IL_0005:  call       ""void System.Console.WriteLine(bool)""
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  conv.u
  IL_000d:  ceq
  IL_000f:  call       ""void System.Console.WriteLine(bool)""
  IL_0014:  ret
}
");
        }

        [Fact, WorkItem(49639, "https://github.com/dotnet/roslyn/issues/49639")]
        public void CompareToNullWithPointerToUnmanagedTypeParameter()
        {
            var verifier = CompileAndVerify(@"
using System;
unsafe
{
    test<int>(null);
    int i = 0;
    test<int>(&i);

    static void test<T>(T* t) where T : unmanaged
    {
        Console.WriteLine(t == null);
        Console.WriteLine(t is null);
    }
}
", options: TestOptions.UnsafeReleaseExe, expectedOutput: @"
True
True
False
False", verify: Verification.Skipped);

            verifier.VerifyIL("Program.<<Main>$>g__test|0_0<T>(T*)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  conv.u
  IL_0003:  ceq
  IL_0005:  call       ""void System.Console.WriteLine(bool)""
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  conv.u
  IL_000d:  ceq
  IL_000f:  call       ""void System.Console.WriteLine(bool)""
  IL_0014:  ret
}
");
        }

        [Theory]
        [InlineData("int*")]
        [InlineData("int*[]")]
        [InlineData("delegate*<void>")]
        [InlineData("T*")]
        [InlineData("delegate*<T>")]
        public void CompareToNullInPatternOutsideUnsafe(string pointerType)
        {
            var comp = CreateCompilation($@"
var c = default(S<int>);
_ = c.Field is null;
unsafe struct S<T> where T : unmanaged
{{
#pragma warning disable CS0649 // Field is unassigned
    public {pointerType} Field;
}}
", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // _ = c.Field is null;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "c.Field").WithLocation(3, 5)
            );
        }

        #endregion Pointer comparison tests

        #region stackalloc tests

        [Fact]
        public void SimpleStackAlloc()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int count = 1;
        checked
        {
            int* p = stackalloc int[2];
            char* q = stackalloc char[count];

            Use(p);
            Use(q);
        }
        unchecked
        {
            int* p = stackalloc int[2];
            char* q = stackalloc char[count];

            Use(p);
            Use(q);
        }
    }

    static void Use(int * ptr)
    {        
    }

    static void Use(char * ptr)
    {        
    }
}
";
            // NOTE: conversion is always unchecked, multiplication is always checked.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (int V_0, //count
                int* V_1, //p
                int* V_2) //p
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.8
  IL_0003:  conv.u
  IL_0004:  localloc
  IL_0006:  stloc.1
  IL_0007:  ldloc.0
  IL_0008:  conv.u
  IL_0009:  ldc.i4.2
  IL_000a:  mul.ovf.un
  IL_000b:  localloc
  IL_000d:  ldloc.1
  IL_000e:  call       ""void C.Use(int*)""
  IL_0013:  call       ""void C.Use(char*)""
  IL_0018:  ldc.i4.8
  IL_0019:  conv.u
  IL_001a:  localloc
  IL_001c:  stloc.2
  IL_001d:  ldloc.0
  IL_001e:  conv.u
  IL_001f:  ldc.i4.2
  IL_0020:  mul.ovf.un
  IL_0021:  localloc
  IL_0023:  ldloc.2
  IL_0024:  call       ""void C.Use(int*)""
  IL_0029:  call       ""void C.Use(char*)""
  IL_002e:  ret
}
");

        }

        [Fact]
        public void StackAllocConversion()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        void* p = stackalloc int[2];
        C q = stackalloc int[2];
    }

    public static implicit operator C(int* p)
    {
        return null;
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.M", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (void* V_0) //p
  IL_0000:  ldc.i4.8
  IL_0001:  conv.u
  IL_0002:  localloc
  IL_0004:  stloc.0
  IL_0005:  ldc.i4.8
  IL_0006:  conv.u
  IL_0007:  localloc
  IL_0009:  call       ""C C.op_Implicit(int*)""
  IL_000e:  pop
  IL_000f:  ret
}
");
        }

        [Fact]
        public void StackAllocConversionZero()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        void* p = stackalloc int[0];
        C q = stackalloc int[0];
    }

    public static implicit operator C(int* p)
    {
        return null;
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.FailsPEVerify).VerifyIL("C.M", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (void* V_0) //p
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  conv.u
  IL_0005:  call       ""C C.op_Implicit(int*)""
  IL_000a:  pop
  IL_000b:  ret
}
");
        }

        [Fact]
        public void StackAllocSpecExample() //Section 18.8
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        Console.WriteLine(IntToString(123));
        Console.WriteLine(IntToString(-456));
    }

	static string IntToString(int value) {
		int n = value >= 0? value: -value;
		unsafe {
			char* buffer = stackalloc char[16];
			char* p = buffer + 16;
			do {
				*--p = (char)(n % 10 + '0');
				n /= 10;
			} while (n != 0);
			if (value < 0) *--p = '-';
			return new string(p, 0, (int)(buffer + 16 - p));
		}
	}
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"123
-456
");
        }

        // See MethodToClassRewriter.VisitAssignmentOperator for an explanation.
        [Fact]
        public void StackAllocIntoHoistedLocal1()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        var p = stackalloc int[2];
        var q = stackalloc int[2];

        Action a = () =>
        {
            var r = stackalloc int[2];
            var s = stackalloc int[2];

            Action b = () =>
            {
                p = null; //capture p
                r = null; //capture r
            };
            
            Use(s);
        };

        Use(q);
    }

    static void Use(int * ptr)
    {        
    }
}
";
            var verifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);

            // Note that the stackalloc for p is written into a temp *before* the receiver (i.e. "this")
            // for C.<>c__DisplayClass0.p is pushed onto the stack.
            verifier.VerifyIL("C.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int* V_1)
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.8
  IL_0007:  conv.u
  IL_0008:  localloc
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  stfld      ""int* C.<>c__DisplayClass0_0.p""
  IL_0012:  ldc.i4.8
  IL_0013:  conv.u
  IL_0014:  localloc
  IL_0016:  call       ""void C.Use(int*)""
  IL_001b:  ret
}
");

            // Check that the same thing works inside a lambda.
            verifier.VerifyIL("C.<>c__DisplayClass0_0.<Main>b__0", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                int* V_1)
  IL_0000:  newobj     ""C.<>c__DisplayClass0_1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_000d:  ldc.i4.8
  IL_000e:  conv.u
  IL_000f:  localloc
  IL_0011:  stloc.1
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  stfld      ""int* C.<>c__DisplayClass0_1.r""
  IL_0019:  ldc.i4.8
  IL_001a:  conv.u
  IL_001b:  localloc
  IL_001d:  call       ""void C.Use(int*)""
  IL_0022:  ret
}
");
        }

        // See MethodToClassRewriter.VisitAssignmentOperator for an explanation.
        [Fact]
        public void StackAllocIntoHoistedLocal2()
        {
            // From native bug #59454 (in DevDiv collection)
            var text = @"
unsafe class T 
{ 
    delegate int D(); 

    static void Main() 
    { 
        int* v = stackalloc int[1]; 
        D d = delegate { return *v; }; 
        System.Console.WriteLine(d()); 
    } 
} 
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "0", verify: Verification.Fails).VerifyIL("T.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (T.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                int* V_1)
  IL_0000:  newobj     ""T.<>c__DisplayClass1_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.4
  IL_0007:  conv.u
  IL_0008:  localloc
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  stfld      ""int* T.<>c__DisplayClass1_0.v""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int T.<>c__DisplayClass1_0.<Main>b__0()""
  IL_0019:  newobj     ""T.D..ctor(object, System.IntPtr)""
  IL_001e:  callvirt   ""int T.D.Invoke()""
  IL_0023:  call       ""void System.Console.WriteLine(int)""
  IL_0028:  ret
}
");
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "0", verify: Verification.Fails).VerifyIL("T.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (T.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                int* V_1)
  IL_0000:  newobj     ""T.<>c__DisplayClass1_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.4
  IL_0007:  conv.u
  IL_0008:  localloc
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  stfld      ""int* T.<>c__DisplayClass1_0.v""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int T.<>c__DisplayClass1_0.<Main>b__0()""
  IL_0019:  newobj     ""T.D..ctor(object, System.IntPtr)""
  IL_001e:  callvirt   ""int T.D.Invoke()""
  IL_0023:  call       ""void System.Console.WriteLine(int)""
  IL_0028:  ret
}
");
        }

        [Fact]
        public void CSLegacyStackallocUse32bitChecked()
        {
            // This is from C# Legacy test where it uses Perl script to call ildasm and check 'mul.ovf' emitted
            // $Roslyn\Main\LegacyTest\CSharp\Source\csharp\Source\Conformance\unsafecode\stackalloc\regr001.cs
            var text = @"// <Title>Should checked affect stackalloc?</Title>
// <Description>
// The lower level localloc MSIL instruction takes an unsigned native int as input; however the higher level 
// stackalloc uses only 32-bits. The example shows the operation overflowing the 32-bit multiply which leads to 
// a curious edge condition.
// If compile with /checked we insert a mul.ovf instruction, and this causes a system overflow exception at runtime.
// </Description>
// <RelatedBugs>VSW:489857</RelatedBugs>

using System;

public class C
{
    private static unsafe int Main()
    {
        Int64* intArray = stackalloc Int64[0x7fffffff];
        return (int)intArray[0];
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);
            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4     0x7fffffff
  IL_0005:  conv.u
  IL_0006:  ldc.i4.8
  IL_0007:  mul.ovf.un
  IL_0008:  localloc
  IL_000a:  ldind.i8
  IL_000b:  conv.i4
  IL_000c:  ret
}
");
        }

        #endregion stackalloc tests

        #region Functional tests

        [Fact]
        public void BubbleSort()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main() 
    {
        BubbleSort();
        BubbleSort(1);
        BubbleSort(2, 1);
        BubbleSort(3, 1, 2);
        BubbleSort(3, 1, 4, 2);
    }

    static void BubbleSort(params int[] array)
    {
        if (array == null)
        {
            return;
        }

        fixed (int* begin = array)
        {
            BubbleSort(begin, end: begin + array.Length);
        }

        Console.WriteLine(string.Join("", "", array));
    }

    private static void BubbleSort(int* begin, int* end)
    {
        for (int* firstUnsorted = begin; firstUnsorted < end; firstUnsorted++)
        {
            for (int* current = firstUnsorted; current + 1 < end; current++)
            {
                if (current[0] > current[1])
                {
                    SwapWithNext(current);
                }
            }
        }
    }

    static void SwapWithNext(int* p)
    {
        int temp = *p;
        p[0] = p[1];
        p[1] = temp;
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"
1
1, 2
1, 2, 3
1, 2, 3, 4");
        }

        [Fact]
        public void BigStructs()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        void* v;

        CheckOverflow(""(S15*)0 + sizeof(S15)"", () => v = checked((S15*)0 + sizeof(S15)));
        CheckOverflow(""(S15*)0 + sizeof(S16)"", () => v = checked((S15*)0 + sizeof(S16)));
        CheckOverflow(""(S16*)0 + sizeof(S15)"", () => v = checked((S16*)0 + sizeof(S15)));
    }

    static void CheckOverflow(string description, System.Action operation)
    {
        try
        {
            operation();
            System.Console.WriteLine(""No overflow from {0}"", description);
        }
        catch (System.OverflowException)
        {
            System.Console.WriteLine(""Overflow from {0}"", description);
        }
    }
}
" + SizedStructs;

            bool isx86 = (IntPtr.Size == 4);
            string expectedOutput;

            if (isx86)
            {
                expectedOutput = @"
No overflow from (S15*)0 + sizeof(S15)
Overflow from (S15*)0 + sizeof(S16)
Overflow from (S16*)0 + sizeof(S15)";
            }
            else
            {
                expectedOutput = @"
No overflow from (S15*)0 + sizeof(S15)
No overflow from (S15*)0 + sizeof(S16)
No overflow from (S16*)0 + sizeof(S15)";
            }
            // PEVerify:
            // [ : C+<>c__DisplayClass0_0::<Main>b__0][mdToken=0x6000005][offset 0x00000012][found Native Int][expected unmanaged pointer] Unexpected type on the stack.
            // [ : C+<> c__DisplayClass0_0::< Main > b__1][mdToken= 0x6000006][offset 0x00000012][found Native Int][expected unmanaged pointer] Unexpected type on the stack.
            // [ : C +<> c__DisplayClass0_0::< Main > b__2][mdToken = 0x6000007][offset 0x00000012][found Native Int][expected unmanaged pointer] Unexpected type on the stack.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void LambdaConversion()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        Goo(x => { });
    }

    static void Goo(F1 f) { Console.WriteLine(1); }
    static void Goo(F2 f) { Console.WriteLine(2); }
}

unsafe delegate void F1(int* x);
delegate void F2(int x);
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"2", verify: Verification.Passes);
        }

        [Fact]
        public void LambdaConversion_PointerArray()
        {
            var text = @"
using System;

class C<T> { }

class Program
{
    static void Main()
    {
        M(x => { });
    }

    static void M(F1 f) { throw null; }
    static void M(F2 f) { Console.WriteLine(2); }
}

unsafe delegate void F1(C<int*[]> x);
delegate void F2(int x);
";

            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67330")]
        public void ParameterContainsPointer()
        {
            var source = """
class C<T> { }
class D
{
    public static void M1()
    {
        var lam1 = (int* ptr) => ptr; // 1
    }
    public static void M2()
    {
        var lam2 = (int*[] a) => a; // 2
    }
    public static void M3()
    {
        var lam3 = (delegate*<void> ptr) => ptr; // 3
    }
    public static void M4()
    {
        var lam4 = (C<delegate*<void>[]> a) => a; // 4
    }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (6,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam1 = (int* ptr) => ptr; // 1
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(6, 21),
                // (6,26): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam1 = (int* ptr) => ptr; // 1
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(6, 26),
                // (6,34): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam1 = (int* ptr) => ptr; // 1
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(6, 34),
                // (10,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam2 = (int*[] a) => a; // 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(10, 21),
                // (10,28): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam2 = (int*[] a) => a; // 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(10, 28),
                // (10,34): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam2 = (int*[] a) => a; // 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(10, 34),
                // (14,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam3 = (delegate*<void> ptr) => ptr; // 3
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(14, 21),
                // (14,37): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam3 = (delegate*<void> ptr) => ptr; // 3
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(14, 37),
                // (14,45): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam3 = (delegate*<void> ptr) => ptr; // 3
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(14, 45),
                // (18,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam4 = (C<delegate*<void>[]> a) => a; // 4
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(18, 23),
                // (18,42): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam4 = (C<delegate*<void>[]> a) => a; // 4
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(18, 42),
                // (18,48): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var lam4 = (C<delegate*<void>[]> a) => a; // 4
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(18, 48)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67330")]
        public void DelegateConversionContainsPointer()
        {
            var source = """
class C<T> { }
unsafe delegate int* D1(int* ptr);
unsafe delegate int*[] D2(int*[] a);
unsafe delegate delegate*<void> D3(delegate*<void> ptr);
unsafe delegate C<delegate*<void>[]> D4(C<delegate*<void>[]> a);

class D
{
    public static D1 M1()
    {
        return (ptr) => ptr; // 1
    }
    public static D2 M2()
    {
        return (a) => a; // 2
    }
    public static D3 M3()
    {
        return (ptr) => ptr; // 3
    }
    public static D4 M4()
    {
        return (a) => a; // 4
    }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (11,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (ptr) => ptr; // 1
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(11, 17),
                // (11,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (ptr) => ptr; // 1
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(11, 25),
                // (15,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (a) => a; // 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(15, 17),
                // (15,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (a) => a; // 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(15, 23),
                // (19,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (ptr) => ptr; // 3
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(19, 17),
                // (19,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (ptr) => ptr; // 3
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr").WithLocation(19, 25),
                // (23,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (a) => a; // 4
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(23, 17),
                // (23,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (a) => a; // 4
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a").WithLocation(23, 23)
                );
        }

        [Fact]
        public void LocalVariableReuse()
        {
            var text = @"
unsafe class C
{
    int this[string s] { get { return 0; } set { } }

    void Test()
    {
        {
            this[""not pinned"".ToString()] += 2; //creates an unpinned string local (for the argument)
        }

        fixed (char* p = ""pinned"") //creates a pinned string local
        {
        }

        {
            this[""not pinned"".ToString()] += 2; //reuses the unpinned string local
        }

        fixed (char* p = ""pinned"") //reuses the pinned string local
        {
        }
    }
}
";
            // NOTE: one pinned string temp and one unpinned string temp.
            // That is, pinned temps are reused in by other pinned temps
            // but not by unpinned temps and vice versa.
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("C.Test", @"
{
  // Code size       99 (0x63)
  .maxstack  4
  .locals init (string V_0,
                char* V_1, //p
                pinned string V_2,
                char* V_3) //p
  IL_0000:  ldstr      ""not pinned""
  IL_0005:  callvirt   ""string object.ToString()""
  IL_000a:  stloc.0
  IL_000b:  ldarg.0
  IL_000c:  ldloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldloc.0
  IL_000f:  call       ""int C.this[string].get""
  IL_0014:  ldc.i4.2
  IL_0015:  add
  IL_0016:  call       ""void C.this[string].set""
  IL_001b:  ldstr      ""pinned""
  IL_0020:  stloc.2
  IL_0021:  ldloc.2
  IL_0022:  conv.u
  IL_0023:  stloc.1
  IL_0024:  ldloc.1
  IL_0025:  brfalse.s  IL_002f
  IL_0027:  ldloc.1
  IL_0028:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_002d:  add
  IL_002e:  stloc.1
  IL_002f:  ldnull
  IL_0030:  stloc.2
  IL_0031:  ldstr      ""not pinned""
  IL_0036:  callvirt   ""string object.ToString()""
  IL_003b:  stloc.0
  IL_003c:  ldarg.0
  IL_003d:  ldloc.0
  IL_003e:  ldarg.0
  IL_003f:  ldloc.0
  IL_0040:  call       ""int C.this[string].get""
  IL_0045:  ldc.i4.2
  IL_0046:  add
  IL_0047:  call       ""void C.this[string].set""
  IL_004c:  ldstr      ""pinned""
  IL_0051:  stloc.2
  IL_0052:  ldloc.2
  IL_0053:  conv.u
  IL_0054:  stloc.3
  IL_0055:  ldloc.3
  IL_0056:  brfalse.s  IL_0060
  IL_0058:  ldloc.3
  IL_0059:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_005e:  add
  IL_005f:  stloc.3
  IL_0060:  ldnull
  IL_0061:  stloc.2
  IL_0062:  ret
}");
        }

        [WorkItem(544229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544229")]
        [Fact]
        public void UnsafeTypeAsAttributeArgument()
        {
            var template = @"
using System;
 
namespace System
{{
    class Int32 {{ }}
}}
 
 
[A(Type = typeof({0}))]
class A : Attribute
{{
    public Type Type;
    static void Main()
    {{
        var a = (A)typeof(A).GetCustomAttributes(false)[0];
        Console.WriteLine(a.Type == typeof({0}));
    }}
}}
";
            CompileAndVerify(string.Format(template, "int"), options: TestOptions.UnsafeReleaseExe, expectedOutput: @"True", verify: Verification.Passes);
            CompileAndVerify(string.Format(template, "int*"), options: TestOptions.UnsafeReleaseExe, expectedOutput: @"True", verify: Verification.Passes);
            CompileAndVerify(string.Format(template, "int**"), options: TestOptions.UnsafeReleaseExe, expectedOutput: @"True", verify: Verification.Passes);
            CompileAndVerify(string.Format(template, "int[]"), options: TestOptions.UnsafeReleaseExe, expectedOutput: @"True", verify: Verification.Passes);
            CompileAndVerify(string.Format(template, "int[][]"), options: TestOptions.UnsafeReleaseExe, expectedOutput: @"True", verify: Verification.Passes);
            CompileAndVerify(string.Format(template, "int*[]"), options: TestOptions.UnsafeReleaseExe, expectedOutput: @"True", verify: Verification.Passes);
        }

        #endregion Functional tests

        #region Regression tests

        [WorkItem(545026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545026")]
        [Fact]
        public void MixedSafeAndUnsafeFields()
        {
            var text =
@"struct Perf_Contexts
{
    int data;
    private int SuppressUnused(int x) { data = x; return data; }
}

public sealed class ChannelServices
{
    static unsafe Perf_Contexts* GetPrivateContextsPerfCounters() { return null; }
    private static int I1 = 12;
    unsafe private static Perf_Contexts* perf_Contexts = GetPrivateContextsPerfCounters();
    private static int I2 = 13;
    private static int SuppressUnused(int x) { return I1 + I2; }
}

public class Test
{
    public static void Main()
    {
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.FailsPEVerify with
            {
                PEVerifyMessage = """
                    [ : ChannelServices::.cctor][offset 0x0000000C][found unmanaged pointer][expected unmanaged pointer] Unexpected type on the stack.
                    [ : ChannelServices::GetPrivateContextsPerfCounters][offset 0x00000002][found Native Int][expected unmanaged pointer] Unexpected type on the stack.
                    """,
            }).VerifyDiagnostics();
        }

        [WorkItem(545026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545026")]
        [Fact]
        public void SafeFieldBeforeUnsafeField()
        {
            var text = @"
class C
{
    int x = 1;
    unsafe int* p = (int*)2;
}
";
            var c = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.FailsPEVerify with
            {
                PEVerifyMessage = "[ : C::.ctor][offset 0x0000000A][found Native Int][expected unmanaged pointer] Unexpected type on the stack."
            });

            c.VerifyDiagnostics(
                // (4,9): warning CS0414: The field 'C.x' is assigned but its value is never used
                //     int x = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x").WithArguments("C.x"));
        }

        [WorkItem(545026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545026")]
        [Fact]
        public void SafeFieldAfterUnsafeField()
        {
            var text = @"
class C
{
    unsafe int* p = (int*)2;
    int x = 1;
}
";
            var c = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.FailsPEVerify with
            {
                PEVerifyMessage = "[ : C::.ctor][offset 0x00000003][found Native Int][expected unmanaged pointer] Unexpected type on the stack."
            });

            c.VerifyDiagnostics(
                // (5,9): warning CS0414: The field 'C.x' is assigned but its value is never used
                //     int x = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x").WithArguments("C.x"));
        }

        [WorkItem(545026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545026"), WorkItem(598170, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598170")]
        [Fact]
        public void FixedPassByRef()
        {
            var text = @"
class Test
{
    unsafe static int printAddress(out int* pI)
    {
        pI = null;
        System.Console.WriteLine((ulong)pI);
        return 1;
    }

    unsafe static int printAddress1(ref int* pI)
    {
        pI = null;
        System.Console.WriteLine((ulong)pI);
        return 1;
    }

    static int Main()
    {
        int retval = 0;
        S s = new S();
        unsafe
        {
            retval = Test.printAddress(out s.i);
            retval = Test.printAddress1(ref s.i);
        }

        if (retval == 0)
            System.Console.WriteLine(""Failed."");

        return retval;
    }
}
unsafe struct S
{
    public fixed int i[1];
}

";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (24,44): error CS1510: A ref or out argument must be an assignable variable
                //             retval = Test.printAddress(out s.i);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "s.i"),
                // (25,45): error CS1510: A ref or out argument must be an assignable variable
                //             retval = Test.printAddress1(ref s.i);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "s.i"));
        }

        [Fact, WorkItem(545293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545293"), WorkItem(881188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/881188")]
        public void EmptyAndFixedBufferStructIsInitialized()
        {
            var text = @"
public struct EmptyStruct { }
unsafe public struct FixedStruct { fixed char c[10]; }

public struct OuterStruct 
{
    EmptyStruct ES;
    FixedStruct FS;
    override public string ToString() { return (ES.ToString() + FS.ToString()); }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes).VerifyDiagnostics(
                // (8,17): warning CS0649: Field 'OuterStruct.FS' is never assigned to, and will always have its default value 
                //     FixedStruct FS;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "FS").WithArguments("OuterStruct.FS", "").WithLocation(8, 17),
                // (7,17): warning CS0649: Field 'OuterStruct.ES' is never assigned to, and will always have its default value 
                //     EmptyStruct ES;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "ES").WithArguments("OuterStruct.ES", "").WithLocation(7, 17)
                );
        }

        [Fact, WorkItem(545296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545296"), WorkItem(545999, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545999")]
        public void FixedBufferAndStatementWithFixedArrayElementAsInitializer()
        {
            var text = @"
unsafe public struct FixedStruct 
{
    fixed int i[1];
    fixed char c[10];
    override public string ToString()  { 
        fixed (char* pc = this.c) { return pc[0].ToString(); } 
    }
}
";
            var comp = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyDiagnostics();

            comp.VerifyIL("FixedStruct.ToString", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (pinned char& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""char* FixedStruct.c""
  IL_0006:  ldflda     ""char FixedStruct.<c>e__FixedBuffer.FixedElementField""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  conv.u
  IL_000e:  call       ""string char.ToString()""
  IL_0013:  ret
}
");
        }

        [Fact]
        public void FixedBufferAndStatementWithFixedArrayElementAsInitializerExe()
        {
            var text = @"
    class Program
    {
        unsafe static void Main(string[] args)
        {
            FixedStruct s = new FixedStruct();

            s.c[0] = 'A';
            s.c[1] = 'B';
            s.c[2] = 'C';

            FixedStruct[] arr = { s };

            System.Console.Write(arr[0].ToString());
        }
    }

    unsafe public struct FixedStruct
    {
        public fixed char c[10];

        override public string ToString()
        {
            fixed (char* pc = this.c)
            {
                System.Console.Write(pc[0]);
                System.Console.Write(pc[1].ToString());
                return pc[2].ToString();
            }
        }
    }";
            var comp = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "ABC", verify: Verification.Fails).VerifyDiagnostics();

            comp.VerifyIL("FixedStruct.ToString", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (pinned char& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""char* FixedStruct.c""
  IL_0006:  ldflda     ""char FixedStruct.<c>e__FixedBuffer.FixedElementField""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  conv.u
  IL_000e:  dup
  IL_000f:  ldind.u2
  IL_0010:  call       ""void System.Console.Write(char)""
  IL_0015:  dup
  IL_0016:  ldc.i4.2
  IL_0017:  add
  IL_0018:  call       ""string char.ToString()""
  IL_001d:  call       ""void System.Console.Write(string)""
  IL_0022:  ldc.i4.2
  IL_0023:  conv.i
  IL_0024:  ldc.i4.2
  IL_0025:  mul
  IL_0026:  add
  IL_0027:  call       ""string char.ToString()""
  IL_002c:  ret
}
");
        }

        [Fact, WorkItem(545299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545299")]
        public void FixedStatementInlambda()
        {
            var text = @"
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

unsafe class C<T> where T : struct
{
    public void Goo()
    {
        Func<T, char> d = delegate
        {
            fixed (char* p = ""blah"")
            {
                for (char* pp = p; pp != null; pp++)
                    return *pp;
            }
            return 'c';
        };

        Console.WriteLine(d(default(T)));
    }
}

class A
{
    static void Main()
    {
        new C<int>().Goo();
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "b", verify: Verification.Fails);
        }

        [Fact, WorkItem(546865, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546865")]
        public void DontStackScheduleLocalPerformingPointerConversion()
        {
            var text = @"
using System;

unsafe struct S1
{
    public char* charPointer;
}

unsafe class Test
{
    static void Main()
    {
        S1 s1 = new S1();
        fixed (char* p = ""hello"")
        {
            s1.charPointer = p;
            ulong UserData = (ulong)&s1;
            Test1(UserData);
        }
    }

    static void Test1(ulong number)
    {
        S1* structPointer = (S1*)number;
        Console.WriteLine(new string(structPointer->charPointer));
    }

    static ulong Test2()
    {
        S1* structPointer = (S1*)null; // null to pointer
        int* intPointer = (int*)structPointer; // pointer to pointer
        void* voidPointer = (void*)intPointer; // pointer to void
        ulong number = (ulong)voidPointer; // pointer to integer
        return number;
    }
}
";

            var verifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "hello", verify: Verification.Fails);

            // Note that the pointer local is not scheduled on the stack.
            verifier.VerifyIL("Test.Test1", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1* V_0) //structPointer
  IL_0000:  ldarg.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldfld      ""char* S1.charPointer""
  IL_0009:  newobj     ""string..ctor(char*)""
  IL_000e:  call       ""void System.Console.WriteLine(string)""
  IL_0013:  ret
}");

            // All locals retained.
            verifier.VerifyIL("Test.Test2", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (S1* V_0, //structPointer
  int* V_1, //intPointer
                void* V_2) //voidPointer
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  stloc.1
  IL_0005:  ldloc.1
  IL_0006:  stloc.2
  IL_0007:  ldloc.2
  IL_0008:  conv.u8
  IL_0009:  ret
}");
        }

        [Fact, WorkItem(546807, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546807")]
        public void PointerMemberAccessReadonlyField()
        {
            var text = @"
using System;

unsafe class C
{
    public S1* S1;
}

unsafe struct S1
{
    public readonly int* X;
    public int* Y;
}

unsafe class Test
{
    static void Main()
    {
        S1 s1 = new S1();
        C c = new C();
        c.S1 = &s1;
        Console.WriteLine(null == c.S1->X);
        Console.WriteLine(null == c.S1->Y);
    }
}
";

            var verifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"
True
True");

            // NOTE: ldobj before ldfld S1.X, but not before ldfld S1.Y.
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (S1 V_0, //s1
  C V_1) //c
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  IL_0008:  newobj     ""C..ctor()""
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  ldloca.s   V_0
  IL_0011:  conv.u
  IL_0012:  stfld      ""S1* C.S1""
  IL_0017:  ldc.i4.0
  IL_0018:  conv.u
  IL_0019:  ldloc.1
  IL_001a:  ldfld      ""S1* C.S1""
  IL_001f:  ldfld      ""int* S1.X""
  IL_0024:  ceq
  IL_0026:  call       ""void System.Console.WriteLine(bool)""
  IL_002b:  ldc.i4.0
  IL_002c:  conv.u
  IL_002d:  ldloc.1
  IL_002e:  ldfld      ""S1* C.S1""
  IL_0033:  ldfld      ""int* S1.Y""
  IL_0038:  ceq
  IL_003a:  call       ""void System.Console.WriteLine(bool)""
  IL_003f:  ret
}
");
        }

        [Fact, WorkItem(546807, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546807")]
        public void PointerMemberAccessCall()
        {
            var text = @"
using System;

unsafe class C
{
    public S1* S1;
}

unsafe struct S1
{
    public int X;

    public void Instance()
    {
        Console.WriteLine(this.X);
    }
}

static class Extensions
{
    public static void Extension(this S1 s1)
    {
        Console.WriteLine(s1.X);
    }
}

unsafe class Test
{
    static void Main()
    {
        S1 s1 = new S1 { X = 2 };
        C c = new C();
        c.S1 = &s1;
        c.S1->Instance();
        c.S1->Extension();
    }
}
";

            var verifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"
2
2");

            // NOTE: ldobj before extension call, but not before instance call.
            verifier.VerifyIL("Test.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (S1 V_0, //s1
  S1 V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S1""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S1.X""
  IL_0010:  ldloc.1
  IL_0011:  stloc.0
  IL_0012:  newobj     ""C..ctor()""
  IL_0017:  dup
  IL_0018:  ldloca.s   V_0
  IL_001a:  conv.u
  IL_001b:  stfld      ""S1* C.S1""
  IL_0020:  dup
  IL_0021:  ldfld      ""S1* C.S1""
  IL_0026:  call       ""void S1.Instance()""
  IL_002b:  ldfld      ""S1* C.S1""
  IL_0030:  ldobj      ""S1""
  IL_0035:  call       ""void Extensions.Extension(S1)""
  IL_003a:  ret
}");
        }

        [Fact, WorkItem(531327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531327")]
        public void PointerParameter()
        {
            var text = @"
using System;

unsafe struct S1
{
    static void M(N.S2* ps2){}
}
namespace N
{
  public struct S2
  {
    public int F;
  }
}
";

            var verifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll.WithConcurrentBuild(false), verify: Verification.Passes);
        }

        [Fact, WorkItem(531327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531327")]
        public void PointerReturn()
        {
            var text = @"
using System;

namespace N
{
  public struct S2
  {
    public int F;
  }
}

unsafe struct S1
{
    static N.S2* M(int ps2){return null;}
}

";

            var verifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll.WithConcurrentBuild(false), verify: Verification.FailsPEVerify);
        }

        [Fact, WorkItem(748530, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/748530")]
        public void Repro748530()
        {
            var text = @"
unsafe class A
{
    public unsafe struct ListNode
    {
        internal ListNode(int data, ListNode* pNext)
        {
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();
        }

        [WorkItem(682584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682584")]
        [Fact]
        public void UnsafeMathConv()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main(string[] args)
    {
        byte* data = (byte*)0x76543210;
        uint offset = 0x80000000;
        byte* wrong = data + offset;
        Console.WriteLine(""{0:X}"", (ulong)wrong);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "F6543210", verify: Verification.Fails);
            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (byte* V_0, //data
                uint V_1, //offset
                byte* V_2) //wrong
  IL_0000:  ldc.i4     0x76543210
  IL_0005:  conv.i
  IL_0006:  stloc.0
  IL_0007:  ldc.i4     0x80000000
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  conv.u
  IL_0010:  add
  IL_0011:  stloc.2
  IL_0012:  ldstr      ""{0:X}""
  IL_0017:  ldloc.2
  IL_0018:  conv.u8
  IL_0019:  box        ""ulong""
  IL_001e:  call       ""void System.Console.WriteLine(string, object)""
  IL_0023:  ret
}
");
        }

        [WorkItem(682584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682584")]
        [Fact]
        public void UnsafeMathConv001()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main(string[] args)
    {
        short* data = (short*)0x76543210;
        uint offset = 0x40000000;
        short* wrong = data + offset;
        Console.WriteLine(""{0:X}"", (ulong)wrong);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "F6543210", verify: Verification.Fails);
            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  3
  .locals init (short* V_0, //data
                uint V_1, //offset
                short* V_2) //wrong
  IL_0000:  ldc.i4     0x76543210
  IL_0005:  conv.i
  IL_0006:  stloc.0
  IL_0007:  ldc.i4     0x40000000
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  conv.u8
  IL_0010:  ldc.i4.2
  IL_0011:  conv.i8
  IL_0012:  mul
  IL_0013:  conv.i
  IL_0014:  add
  IL_0015:  stloc.2
  IL_0016:  ldstr      ""{0:X}""
  IL_001b:  ldloc.2
  IL_001c:  conv.u8
  IL_001d:  box        ""ulong""
  IL_0022:  call       ""void System.Console.WriteLine(string, object)""
  IL_0027:  ret
}
");
        }

        [WorkItem(682584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682584")]
        [Fact]
        public void UnsafeMathConv002()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main(string[] args)
    {
        byte* data = (byte*)0x76543210;
        byte* wrong = data + 0x80000000u;
        Console.WriteLine(""{0:X}"", (ulong)wrong);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "F6543210", verify: Verification.Fails);
            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (byte* V_0, //data
                byte* V_1) //wrong
  IL_0000:  ldc.i4     0x76543210
  IL_0005:  conv.i
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4     0x80000000
  IL_000d:  conv.u
  IL_000e:  add
  IL_000f:  stloc.1
  IL_0010:  ldstr      ""{0:X}""
  IL_0015:  ldloc.1
  IL_0016:  conv.u8
  IL_0017:  box        ""ulong""
  IL_001c:  call       ""void System.Console.WriteLine(string, object)""
  IL_0021:  ret
}
");
        }

        [WorkItem(682584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682584")]
        [Fact]
        public void UnsafeMathConv002a()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main(string[] args)
    {
        byte* data = (byte*)0x76543210;
        byte* wrong = data + 0x7FFFFFFFu;
        Console.WriteLine(""{0:X}"", (ulong)wrong);
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "F654320F", verify: Verification.Fails);
            compVerifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (byte* V_0, //data
                byte* V_1) //wrong
  IL_0000:  ldc.i4     0x76543210
  IL_0005:  conv.i
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4     0x7fffffff
  IL_000d:  add
  IL_000e:  stloc.1
  IL_000f:  ldstr      ""{0:X}""
  IL_0014:  ldloc.1
  IL_0015:  conv.u8
  IL_0016:  box        ""ulong""
  IL_001b:  call       ""void System.Console.WriteLine(string, object)""
  IL_0020:  ret
}
");
        }

        [WorkItem(857598, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/857598")]
        [Fact]
        public void VoidToNullable()
        {
            var text = @"
unsafe class C
{    
	public int? x = (int?)(void*)0;
}

class c1
{
	public static void Main()
	{
		var x = new C();
		System.Console.WriteLine(x.x);
	}
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "0", verify: Verification.Passes);
            compVerifier.VerifyIL("C..ctor", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  conv.i
  IL_0003:  conv.i4
  IL_0004:  newobj     ""int?..ctor(int)""
  IL_0009:  stfld      ""int? C.x""
  IL_000e:  ldarg.0
  IL_000f:  call       ""object..ctor()""
  IL_0014:  ret
}
");
        }

        [WorkItem(907771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907771")]
        [Fact]
        public void UnsafeBeforeReturn001()
        {
            var text = @"

using System;
 
public unsafe class C
{
    private static readonly byte[] _emptyArray = new byte[0];
 
    public static void Main()
    {
        System.Console.WriteLine(ToManagedByteArray(2));
    }
 
    public static byte[] ToManagedByteArray(uint byteCount)
    {
        if (byteCount == 0)
        {
            return _emptyArray; // degenerate case
        }
        else
        {
            byte[] bytes = new byte[byteCount];
            fixed (byte* pBytes = bytes)
            {
 
            }
            return bytes;
        }
    }
}

";
            var compVerifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "System.Byte[]", verify: Verification.Fails);
            compVerifier.VerifyIL("C.ToManagedByteArray", @"
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (byte* V_0, //pBytes
                pinned byte[] V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldsfld     ""byte[] C._emptyArray""
  IL_0008:  ret
  IL_0009:  ldarg.0
  IL_000a:  newarr     ""byte""
  IL_000f:  dup
  IL_0010:  dup
  IL_0011:  stloc.1
  IL_0012:  brfalse.s  IL_0019
  IL_0014:  ldloc.1
  IL_0015:  ldlen
  IL_0016:  conv.i4
  IL_0017:  brtrue.s   IL_001e
  IL_0019:  ldc.i4.0
  IL_001a:  conv.u
  IL_001b:  stloc.0
  IL_001c:  br.s       IL_0027
  IL_001e:  ldloc.1
  IL_001f:  ldc.i4.0
  IL_0020:  ldelema    ""byte""
  IL_0025:  conv.u
  IL_0026:  stloc.0
  IL_0027:  ldnull
  IL_0028:  stloc.1
  IL_0029:  ret
}
");
        }

        [WorkItem(907771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907771")]
        [Fact]
        public void UnsafeBeforeReturn002()
        {
            var text = @"

using System;
 
public unsafe class C
{
    private static readonly byte[] _emptyArray = new byte[0];
 
    public static void Main()
    {
        System.Console.WriteLine(ToManagedByteArray(2));
    }
 
    public static byte[] ToManagedByteArray(uint byteCount)
    {
        if (byteCount == 0)
        {
            return _emptyArray; // degenerate case
        }
        else
        {
            byte[] bytes = new byte[byteCount];
            fixed (byte* pBytes = bytes)
            {
 
            }
            return bytes;
        }
    }
}

";
            var v = CompileAndVerify(text, options: TestOptions.UnsafeDebugExe, expectedOutput: "System.Byte[]", verify: Verification.Fails);
            v.VerifyIL("C.ToManagedByteArray", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (bool V_0,
                byte[] V_1,
                byte[] V_2, //bytes
                byte* V_3, //pBytes
                pinned byte[] V_4)
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  ceq
  IL_0005:  stloc.0
 ~IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0012
 -IL_0009:  nop
 -IL_000a:  ldsfld     ""byte[] C._emptyArray""
  IL_000f:  stloc.1
  IL_0010:  br.s       IL_003e
 -IL_0012:  nop
 -IL_0013:  ldarg.0
  IL_0014:  newarr     ""byte""
  IL_0019:  stloc.2
 -IL_001a:  ldloc.2
  IL_001b:  dup
  IL_001c:  stloc.s    V_4
  IL_001e:  brfalse.s  IL_0026
  IL_0020:  ldloc.s    V_4
  IL_0022:  ldlen
  IL_0023:  conv.i4
  IL_0024:  brtrue.s   IL_002b
  IL_0026:  ldc.i4.0
  IL_0027:  conv.u
  IL_0028:  stloc.3
  IL_0029:  br.s       IL_0035
  IL_002b:  ldloc.s    V_4
  IL_002d:  ldc.i4.0
  IL_002e:  ldelema    ""byte""
  IL_0033:  conv.u
  IL_0034:  stloc.3
 -IL_0035:  nop
 -IL_0036:  nop
 ~IL_0037:  ldnull
  IL_0038:  stloc.s    V_4
 -IL_003a:  ldloc.2
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_003e
 -IL_003e:  ldloc.1
  IL_003f:  ret
}
", sequencePoints: "C.ToManagedByteArray");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void SystemIntPtrInSignature_BreakingChange()
        {
            // NOTE: the IL is intentionally not compliant with ECMA spec 
            //       in particular Metadata spec II.23.2.16 (Short form signatures) says that 
            //       [mscorlib]System.IntPtr   is not supposed to be used in metadata
            //       and short-version   'native int' is supposed to be used instead.
            var ilSource =
@"
.class public AddressHelper{
    .method public hidebysig static valuetype [mscorlib]System.IntPtr AddressOf<T>(!!0& t){
        ldarg 0
        ldind.i
        ret
    }    
}
";
            var csharpSource =
@"
    class Program
    {
        static void Main(string[] args)
        {
            var s = string.Empty;
            var i = AddressHelper.AddressOf(ref s);
            System.Console.WriteLine(i);
        }
    }
";
            var cscomp = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);

            var expected = new[] {
                // (7,35): error CS0570: 'AddressHelper.AddressOf<T>(?)' is not supported by the language
                //             var i = AddressHelper.AddressOf(ref s);
                Diagnostic(ErrorCode.ERR_BindToBogus, "AddressOf").WithArguments("AddressHelper.AddressOf<T>(?)").WithLocation(7, 35)
            };

            cscomp.VerifyDiagnostics(expected);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void SystemIntPtrInSignature_BreakingChange_001()
        {
            var ilSource =
@"
.class public AddressHelper{
    .method public hidebysig static native int AddressOf<T>(!!0& t){
        ldc.i4.5
	    conv.u
	    ret
    }    
}
";
            var csharpSource =
@"
    class Program
    {
        static void Main(string[] args)
        {
            var s = string.Empty;
            var i = AddressHelper.AddressOf(ref s);
            System.Console.WriteLine(i);
        }
    }
";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource, targetFramework: TargetFramework.Mscorlib40, options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics();

            var result = CompileAndVerify(compilation, expectedOutput: "5");
        }

        [Fact, WorkItem(7550, "https://github.com/dotnet/roslyn/issues/7550")]
        public void EnsureNullPointerIsPoppedIfUnused()
        {
            string source = @"
public class A
{
    public unsafe byte* Ptr;

    static void Main()
    {
        unsafe
        {
            var x = new A();
            byte* ptr = (x == null) ? null : x.Ptr;
        }
        System.Console.WriteLine(""OK"");
    }
}
";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: "OK", verify: Verification.Passes);
        }

        [Fact, WorkItem(40768, "https://github.com/dotnet/roslyn/issues/40768")]
        public void DoesNotEmitArrayDotEmptyForEmptyPointerArrayParams()
        {
            var source = @"

using System;

public static class Program
{
   public static unsafe void Main()
   {
      Console.WriteLine(Test());
   }
    
   public static unsafe int Test(params int*[] types)
   {
       return types.Length;
   }
}";
            // PEVerify:
            // [ : Program::Main][mdToken= 0x6000001][offset 0x00000001] Unmanaged pointers are not a verifiable type.
            // [ : Program::Main][mdToken = 0x6000001][offset 0x00000001] Unable to resolve token.
            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: "0", verify: Verification.FailsPEVerify);
            comp.VerifyIL("Program.Main", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int*""
  IL_0006:  call       ""int Program.Test(params int*[])""
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void DoesEmitArrayDotEmptyForEmptyPointerArrayArrayParams()
        {
            var source = @"

using System;

public static class Program
{
   public static unsafe void Main()
   {
      Console.WriteLine(Test());
   }
    
   public static unsafe int Test(params int*[][] types)
   {
       return types.Length;
   }
}";
            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: "0");
            comp.VerifyIL("Program.Main", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  call       ""int*[][] System.Array.Empty<int*[]>()""
  IL_0005:  call       ""int Program.Test(params int*[][])""
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  ret
}");
        }

        #endregion
    }
}
