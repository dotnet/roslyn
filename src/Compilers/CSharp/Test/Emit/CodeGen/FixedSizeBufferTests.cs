// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class FixedSizeBufferTests : EmitMetadataTestBase
    {
        [Fact]
        [WorkItem(26351, "https://github.com/dotnet/roslyn/pull/26351")]
        public void NestedStructFixed()
        {
            var verifier = CompileAndVerify(@"
class  X {
    internal struct CaseRange {
        public int Lo, Hi;
        public unsafe fixed int Delta [3];

        public CaseRange (int lo, int hi, int d1, int d2, int d3)
        {
            Lo = lo;
            Hi = hi;
            unsafe {
                fixed (int *p = Delta) {
                    p [0] = d1;
                    p [1] = d2;
                    p [2] = d3;
                }
            }
        }
    }

    static void Main ()
    {
        var a = new CaseRange (0, 0, 0, 0, 0);
    }
}", options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);
            verifier.VerifyIL("X.CaseRange..ctor", @"
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (pinned int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int X.CaseRange.Lo""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int X.CaseRange.Hi""
  IL_000e:  ldarg.0
  IL_000f:  ldflda     ""int* X.CaseRange.Delta""
  IL_0014:  ldflda     ""int X.CaseRange.<Delta>e__FixedBuffer.FixedElementField""
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  conv.u
  IL_001c:  dup
  IL_001d:  ldarg.3
  IL_001e:  stind.i4
  IL_001f:  dup
  IL_0020:  ldc.i4.4
  IL_0021:  add
  IL_0022:  ldarg.s    V_4
  IL_0024:  stind.i4
  IL_0025:  ldc.i4.2
  IL_0026:  conv.i
  IL_0027:  ldc.i4.4
  IL_0028:  mul
  IL_0029:  add
  IL_002a:  ldarg.s    V_5
  IL_002c:  stind.i4
  IL_002d:  ldc.i4.0
  IL_002e:  conv.u
  IL_002f:  stloc.0
  IL_0030:  ret
}");
        }

        [Fact]
        public void SimpleFixedBuffer()
        {
            var text =
@"using System;
unsafe struct S
{
    public fixed int x[10];
}

class Program
{
    static void Main()
    {
        S s;
        unsafe
        {
            int* p = s.x;
            s.x[0] = 12;
            p[1] = p[0];
            Console.WriteLine(s.x[1]);
        }
        S t = s;
    }
}";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Fails)
                .VerifyIL("Program.Main", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (S V_0, //s
                int* V_1) //p
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldflda     ""int* S.x""
  IL_0007:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_000c:  conv.u
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldflda     ""int* S.x""
  IL_0015:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_001a:  ldc.i4.s   12
  IL_001c:  stind.i4
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4.4
  IL_001f:  add
  IL_0020:  ldloc.1
  IL_0021:  ldind.i4
  IL_0022:  stind.i4
  IL_0023:  ldloca.s   V_0
  IL_0025:  ldflda     ""int* S.x""
  IL_002a:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_002f:  ldc.i4.4
  IL_0030:  add
  IL_0031:  ldind.i4
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  ret
}");
        }

        [Fact]
        public void SimpleFixedBufferNestedField()
        {
            var text =
@"
using System;
unsafe struct S
{
    public fixed int x[10];
}

struct  S1
{
    public S field;
}

class Program
{
    unsafe static void Main()
    {
        S1 c = new S1();
        c.field.x[0] = 12;
        ref int i = ref c.field.x[0];
        Console.WriteLine(i);
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Passes)
                .VerifyIL("Program.Main", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (S1 V_0) //c
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldflda     ""S S1.field""
  IL_000f:  ldflda     ""int* S.x""
  IL_0014:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0019:  ldc.i4.s   12
  IL_001b:  stind.i4
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldflda     ""S S1.field""
  IL_0023:  ldflda     ""int* S.x""
  IL_0028:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_002d:  ldind.i4
  IL_002e:  call       ""void System.Console.WriteLine(int)""
  IL_0033:  ret
}");
        }

        [Fact]
        public void SimpleFixedBufferNestedFieldClass()
        {
            var text =
@"
using System;
unsafe struct S
{
    public fixed int x[10];
}

class  C1
{
    public S field;
}

class Program
{
    unsafe static void Main()
    {
        C1 c = new C1();
        c.field.x[0] = 12;
        ref int i = ref c.field.x[0];
        Console.WriteLine(i);
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Passes)
                .VerifyIL("Program.Main", @"
{
  // Code size       46 (0x2e)
  .maxstack  3
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""S C1.field""
  IL_000b:  ldflda     ""int* S.x""
  IL_0010:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0015:  ldc.i4.s   12
  IL_0017:  stind.i4
  IL_0018:  ldflda     ""S C1.field""
  IL_001d:  ldflda     ""int* S.x""
  IL_0022:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0027:  ldind.i4
  IL_0028:  call       ""void System.Console.WriteLine(int)""
  IL_002d:  ret
}");
        }

        [Fact]
        public void SimpleFixedBufferNestedFieldClassInit()
        {
            var text =
@"
using System;
unsafe struct S
{
    public fixed int x[10];
}

class  C1
{
    public S field;
}

class Program
{
    unsafe static void Main()
    {
        C1 c;
        
        // test that 'this' is properly lowered
        (c = new C1() {field = default}).field.x[0] = 12;

        ref int i = ref c.field.x[0];
        Console.WriteLine(i);
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Passes)
                .VerifyIL("Program.Main", @"
{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""S C1.field""
  IL_000b:  initobj    ""S""
  IL_0011:  dup
  IL_0012:  ldflda     ""S C1.field""
  IL_0017:  ldflda     ""int* S.x""
  IL_001c:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0021:  ldc.i4.s   12
  IL_0023:  stind.i4
  IL_0024:  ldflda     ""S C1.field""
  IL_0029:  ldflda     ""int* S.x""
  IL_002e:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0033:  ldind.i4
  IL_0034:  call       ""void System.Console.WriteLine(int)""
  IL_0039:  ret
}");
        }

        [Fact]
        public void SimpleFixedBufferOfRefStructErr()
        {
            var source =
@"
ref struct S1
{
    public void Use() { }
}

unsafe struct S
{
    public fixed S1 x[10];
}

class Program
{
    unsafe static void Main()
    {
        S s = new S();
        S1 s1 = s.x[3];
        s1.Use();
    }
}
";

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,18): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                //     public fixed S1 x[10];
                Diagnostic(ErrorCode.ERR_IllegalFixedType, "S1").WithLocation(9, 18)
                );
        }

        [Fact]
        public void SimpleFixedBufferIndexingNoUnsafe()
        {
            var source =
@"

unsafe struct S
{
    public fixed int x[10];
}

class Program
{
    static void Main()
    {
        S s = new S();
        // indexing in unmovable context
        System.Console.WriteLine(s.x[3]);

        S[] a = new[]{new S()};
        // indexing in movable context
        System.Console.WriteLine(a[0].x[3]);
    }
}
";

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (14,34): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         System.Console.WriteLine(s.x[3]);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s.x").WithLocation(14, 34),
                // (18,34): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         System.Console.WriteLine(a[0].x[3]);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a[0].x").WithLocation(18, 34)
                );
        }

        [Fact]
        public void SimpleFixedBufferNestedFieldClassRo()
        {
            var source =
@"
unsafe struct S
{
    public fixed int x[10];
}

class  S1
{
    public readonly S field;
}

class Program
{
    unsafe static void Main()
    {
        S1 c = new S1();
        c.field.x[0] = 12;
        ref readonly int iro = ref c.field.x[0];
        ref int irw = ref c.field.x[0];
    }
}
";

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (17,9): error CS1648: Members of readonly field 'S1.field' cannot be modified (except in a constructor or a variable initializer)
                //         c.field.x[0] = 12;
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "c.field.x[0]").WithArguments("S1.field").WithLocation(17, 9),
                // (19,27): error CS1649: Members of readonly field 'S1.field' cannot be used as a ref or out value (except in a constructor)
                //         ref int irw = ref c.field.x[0];
                Diagnostic(ErrorCode.ERR_RefReadonly2, "c.field.x[0]").WithArguments("S1.field").WithLocation(19, 27)
                );
        }

        [Fact]
        public void SimpleFixedBufferNestedFieldClassRoFixed()
        {
            var text =
@"
using System;
unsafe struct S
{
    public fixed int x[10];
}

class S1
{
    public readonly S field;
}

class Program
{
    unsafe static void Main()
    {
        S1 c = new S1();
        fixed (int* ptr = c.field.x)
        {
            ptr[3] = 12;
        }

        ref readonly int i = ref c.field.x[3];
        Console.WriteLine(i);
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Fails)
                .VerifyIL("Program.Main", @"
{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (pinned int& V_0)
  IL_0000:  newobj     ""S1..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""S S1.field""
  IL_000b:  ldflda     ""int* S.x""
  IL_0010:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  conv.u
  IL_0018:  ldc.i4.3
  IL_0019:  conv.i
  IL_001a:  ldc.i4.4
  IL_001b:  mul
  IL_001c:  add
  IL_001d:  ldc.i4.s   12
  IL_001f:  stind.i4
  IL_0020:  ldc.i4.0
  IL_0021:  conv.u
  IL_0022:  stloc.0
  IL_0023:  ldflda     ""S S1.field""
  IL_0028:  ldflda     ""int* S.x""
  IL_002d:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0032:  ldc.i4.3
  IL_0033:  conv.i
  IL_0034:  ldc.i4.4
  IL_0035:  mul
  IL_0036:  add
  IL_0037:  ldind.i4
  IL_0038:  call       ""void System.Console.WriteLine(int)""
  IL_003d:  ret
}");
        }

        [Fact]
        public void FixedStatementOnFixedBuffer()
        {
            var text =
@"using System;
unsafe struct S
{
    public fixed int x[10];
}
class C
{
    public S s;
}

class Program
{
    unsafe static void Main()
    {
        C c = new C();
        fixed (int *p = c.s.x)
        {
            p[0] = 12;
            p[1] = p[0];
            Console.WriteLine(p[1]);
        }
    }
}";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Fails)
                .VerifyIL("Program.Main",
@"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (int* V_0, //p
                pinned int& V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldflda     ""S C.s""
  IL_000a:  ldflda     ""int* S.x""
  IL_000f:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0014:  stloc.1
  IL_0015:  ldloc.1
  IL_0016:  conv.u
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.s   12
  IL_001b:  stind.i4
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.4
  IL_001e:  add
  IL_001f:  ldloc.0
  IL_0020:  ldind.i4
  IL_0021:  stind.i4
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.4
  IL_0024:  add
  IL_0025:  ldind.i4
  IL_0026:  call       ""void System.Console.WriteLine(int)""
  IL_002b:  ldc.i4.0
  IL_002c:  conv.u
  IL_002d:  stloc.1
  IL_002e:  ret
}
");
        }

        [Fact]
        public void SeparateCompilation()
        {
            // Here we test round tripping - emitting a fixed-size buffer into metadata, and then reimporting that
            // fixed-size buffer from metadata and using it in another compilation.
            var s1 =
@"public unsafe struct S
{
    public fixed int x[10];
}";
            var s2 =
@"using System;
class Program
{
    static void Main()
    {
        S s;
        unsafe
        {
            int* p = s.x;
            s.x[0] = 12;
            p[1] = p[0];
            Console.WriteLine(s.x[1]);
        }
        S t = s;
    }
}";
            var comp1 = CompileAndVerify(s1, options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes).Compilation;

            var comp2 = CompileAndVerify(s2,
                options: TestOptions.UnsafeReleaseExe,
                references: new MetadataReference[] { MetadataReference.CreateFromStream(comp1.EmitToStream()) },
                expectedOutput: "12", verify: Verification.Fails).Compilation;

            var f = (FieldSymbol)comp2.GlobalNamespace.GetTypeMembers("S")[0].GetMembers("x")[0];
            Assert.Equal("x", f.Name);
            Assert.True(f.IsFixed);
            Assert.Equal("int*", f.Type.ToString());
            Assert.Equal(10, f.FixedSize);
        }

        [Fact]
        [WorkItem(531407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531407")]
        public void FixedBufferPointer()
        {
            var text =
@"using System;
unsafe struct S
{
    public fixed int x[10];
}

class Program
{
    static void Main()
    {
        S s;
        unsafe
        {
            S* p = &s;
            p->x[0] = 12;
            Console.WriteLine(p->x[0]);
        }
    }
}";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12", verify: Verification.Fails)
                .VerifyIL("Program.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  dup
  IL_0004:  ldflda     ""int* S.x""
  IL_0009:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_000e:  ldc.i4.s   12
  IL_0010:  stind.i4
  IL_0011:  ldflda     ""int* S.x""
  IL_0016:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_001b:  ldind.i4
  IL_001c:  call       ""void System.Console.WriteLine(int)""
  IL_0021:  ret
}
");
        }

        [Fact]
        [WorkItem(587119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/587119")]
        public void FixedSizeBufferInFixedSizeBufferSize_Class()
        {
            var source = @"
unsafe class C
{
    fixed int F[G];
    fixed int G[1];
}
";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (5,15): error CS1642: Fixed size buffer fields may only be members of structs
                //     fixed int G[1];
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "G"),
                // (4,15): error CS1642: Fixed size buffer fields may only be members of structs
                //     fixed int F[G];
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "F"),
                // (4,17): error CS0120: An object reference is required for the non-static field, method, or property 'C.G'
                //     fixed int F[G];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "G").WithArguments("C.G"));
        }

        [Fact]
        [WorkItem(587119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/587119")]
        public void FixedSizeBufferInFixedSizeBufferSize_Struct()
        {
            var source = @"
unsafe struct S
{
    fixed int F[G];
    fixed int G[1];
    fixed int F1[(new S()).G];
}
";
            // CONSIDER: Dev11 reports CS1666 (ERR_FixedBufferNotFixed), but that's no more helpful.
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,18): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //     fixed int F1[(new S()).G];
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "(new S()).G").WithLocation(6, 18),
                // (4,17): error CS0120: An object reference is required for the non-static field, method, or property 'S.G'
                //     fixed int F[G];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "G").WithArguments("S.G").WithLocation(4, 17),
                // (4,17): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //     fixed int F[G];
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "G").WithLocation(4, 17)
                );
        }

        [Fact]
        [WorkItem(586977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586977")]
        public void FixedSizeBufferInFixedSizeBufferSize_Cycle()
        {
            var source = @"
unsafe struct S
{
    fixed int F[F];
    fixed int G[default(S).G];
}
";
            // CONSIDER: Dev11 also reports CS0110 (ERR_CircConstValue), but Roslyn doesn't regard this as a cycle:
            // F has no initializer, so it has no constant value, so the constant value of F is "null" - not "the 
            // constant value of F" (i.e. cyclic).
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (5,17): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //     fixed int G[default(S).G];
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "default(S).G").WithLocation(5, 17),
                // (4,17): error CS0120: An object reference is required for the non-static field, method, or property 'S.F'
                //     fixed int F[F];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F").WithArguments("S.F").WithLocation(4, 17),
                // (4,17): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //     fixed int F[F];
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "F").WithLocation(4, 17)
                );
        }

        [Fact]
        [WorkItem(587000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/587000")]
        public void SingleDimensionFixedBuffersOnly()
        {
            var source = @"
unsafe struct S
{
    fixed int F[3, 4];
}";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,16): error CS7092: A fixed buffer may only have one dimension.
                //     fixed int F[3, 4];
                Diagnostic(ErrorCode.ERR_FixedBufferTooManyDimensions, "[3, 4]"));
        }

        [Fact, WorkItem(1171076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1171076")]
        public void UIntFixedBuffer()
        {
            var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind.Sequential )]
public unsafe struct AssemblyRecord
{
    public fixed byte Marker[ 8 ];
    public fixed UInt32 StartOfTables[ 16 ];
}

class Program
{
    static unsafe void Main( string[ ] args )
    {
        UInt32 [] arr = new UInt32[18];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = (uint)(120 + i);
        } 

        fixed (uint *p = arr)
        {
            AssemblyRecord * record = (AssemblyRecord*)p;
            Console.WriteLine(Test(record));
        }
    }

    private static unsafe uint Test( AssemblyRecord* pStruct )
    {
        return pStruct->StartOfTables[ 11 ];
    }
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "133", verify: Verification.Fails)
                .VerifyIL("Program.Test", @"
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""uint* AssemblyRecord.StartOfTables""
  IL_0006:  ldflda     ""uint AssemblyRecord.<StartOfTables>e__FixedBuffer.FixedElementField""
  IL_000b:  ldc.i4.s   11
  IL_000d:  conv.i
  IL_000e:  ldc.i4.4
  IL_000f:  mul
  IL_0010:  add
  IL_0011:  ldind.u4
  IL_0012:  ret
}");
        }

        [Fact, WorkItem(1171076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1171076")]
        public void ReadonlyFixedBuffer()
        {
            var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind.Sequential )]
public unsafe struct AssemblyRecord
{
    public readonly fixed UInt32 StartOfTables[ 16 ];
}

class Program
{
    private static unsafe uint Test( AssemblyRecord* pStruct )
    {
        return pStruct->StartOfTables[ 11 ];
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
    // (8,34): error CS0106: The modifier 'readonly' is not valid for this item
    //     public readonly fixed UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "StartOfTables").WithArguments("readonly").WithLocation(8, 34)
                );
        }

        [Fact, WorkItem(1171076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1171076")]
        public void StaticFixedBuffer()
        {
            var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind.Sequential )]
public unsafe struct AssemblyRecord
{
    public static fixed UInt32 StartOfTables[ 16 ];
}

class Program
{
    private static unsafe uint Test( AssemblyRecord* pStruct )
    {
        return pStruct->StartOfTables[ 11 ];
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
    // (8,32): error CS0106: The modifier 'static' is not valid for this item
    //     public static fixed UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "StartOfTables").WithArguments("static").WithLocation(8, 32)
                );
        }

        [Fact, WorkItem(1171076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1171076")]
        public void ConstFixedBuffer_01()
        {
            var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind.Sequential )]
public unsafe struct AssemblyRecord
{
    public fixed const UInt32 StartOfTables[ 16 ];
}

class Program
{
    private static unsafe uint Test( AssemblyRecord* pStruct )
    {
        return pStruct->StartOfTables[ 11 ];
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
    // (8,18): error CS1031: Type expected
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_TypeExpected, "const").WithLocation(8, 18),
    // (8,18): error CS1001: Identifier expected
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "const").WithLocation(8, 18),
    // (8,18): error CS1003: Syntax error, '[' expected
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_SyntaxError, "const").WithArguments("[", "const").WithLocation(8, 18),
    // (8,18): error CS1003: Syntax error, ']' expected
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_SyntaxError, "const").WithArguments("]", "const").WithLocation(8, 18),
    // (8,18): error CS0443: Syntax error; value expected
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_ValueExpected, "const").WithLocation(8, 18),
    // (8,18): error CS1002: ; expected
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "const").WithLocation(8, 18),
    // (8,44): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[ 16 ]").WithLocation(8, 44),
    // (8,46): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "16").WithLocation(8, 46),
    // (8,18): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
    //     public fixed const UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_IllegalFixedType, "").WithLocation(8, 18),
    // (15,25): error CS0122: 'AssemblyRecord.StartOfTables' is inaccessible due to its protection level
    //         return pStruct->StartOfTables[ 11 ];
    Diagnostic(ErrorCode.ERR_BadAccess, "StartOfTables").WithArguments("AssemblyRecord.StartOfTables").WithLocation(15, 25)
                );
        }

        [Fact, WorkItem(1171076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1171076")]
        public void ConstFixedBuffer_02()
        {
            var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind.Sequential )]
public unsafe struct AssemblyRecord
{
    public const fixed UInt32 StartOfTables[ 16 ];
}

class Program
{
    private static unsafe uint Test( AssemblyRecord* pStruct )
    {
        return pStruct->StartOfTables[ 11 ];
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
    // (8,18): error CS1031: Type expected
    //     public const fixed UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_TypeExpected, "fixed").WithLocation(8, 18),
    // (8,18): error CS1001: Identifier expected
    //     public const fixed UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "fixed").WithLocation(8, 18),
    // (8,18): error CS0145: A const field requires a value to be provided
    //     public const fixed UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_ConstValueRequired, "fixed").WithLocation(8, 18),
    // (8,18): error CS1002: ; expected
    //     public const fixed UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "fixed").WithLocation(8, 18),
    // (15,25): error CS0122: 'AssemblyRecord.StartOfTables' is inaccessible due to its protection level
    //         return pStruct->StartOfTables[ 11 ];
    Diagnostic(ErrorCode.ERR_BadAccess, "StartOfTables").WithArguments("AssemblyRecord.StartOfTables").WithLocation(15, 25)
                );
        }

        [Fact, WorkItem(1171076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1171076")]
        public void VolatileFixedBuffer()
        {
            var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind.Sequential )]
public unsafe struct AssemblyRecord
{
    public volatile fixed UInt32 StartOfTables[ 16 ];
}

class Program
{
    private static unsafe uint Test( AssemblyRecord* pStruct )
    {
        return pStruct->StartOfTables[ 11 ];
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
    // (8,34): error CS0106: The modifier 'volatile' is not valid for this item
    //     public volatile fixed UInt32 StartOfTables[ 16 ];
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "StartOfTables").WithArguments("volatile").WithLocation(8, 34)
                );
        }

        [Fact, WorkItem(3392, "https://github.com/dotnet/roslyn/issues/3392")]
        public void StructLayout_01()
        {
            foreach (var layout in new[] { LayoutKind.Auto, LayoutKind.Explicit, LayoutKind.Sequential })
            {
                foreach (var charSet in new[] { CharSet.Ansi, CharSet.Auto, CharSet.None, CharSet.Unicode })
                {
                    var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind." + layout.ToString() + ", CharSet = CharSet." + charSet.ToString() + @")]
public unsafe struct Test
{
    " + (layout == LayoutKind.Explicit ? "[FieldOffset(0)]" : "") + @"public fixed UInt32 Field[ 16 ];
}
";
                    CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes, 
                        symbolValidator: (m) =>
                        {
                            var test = m.GlobalNamespace.GetTypeMember("Test");
                            Assert.Equal(layout, test.Layout.Kind);
                            Assert.Equal(charSet == CharSet.None ? CharSet.Ansi : charSet, test.MarshallingCharSet);

                            var bufferType = test.GetTypeMembers().Single();
                            Assert.Equal("Test.<Field>e__FixedBuffer", bufferType.ToTestDisplayString());
                            Assert.Equal(LayoutKind.Sequential, bufferType.Layout.Kind);
                            Assert.Equal(charSet == CharSet.None ? CharSet.Ansi : charSet, bufferType.MarshallingCharSet);
                        });
                }
            }
        }

        [Fact, WorkItem(3392, "https://github.com/dotnet/roslyn/issues/3392")]
        public void StructLayout_02()
        {
            foreach (var layout in new[] { LayoutKind.Auto, LayoutKind.Explicit, LayoutKind.Sequential })
            {
                var text =
@"
using System;
using System.Runtime.InteropServices;

[StructLayout( LayoutKind." + layout.ToString() + @")]
public unsafe struct Test
{
    " + (layout == LayoutKind.Explicit ? "[FieldOffset(0)]" : "") + @"public fixed UInt32 Field[ 16 ];
}
";
                CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll, verify: Verification.Passes,
                    symbolValidator: (m) =>
                    {
                        var test = m.GlobalNamespace.GetTypeMember("Test");
                        Assert.Equal(layout, test.Layout.Kind);
                        Assert.Equal(CharSet.Ansi, test.MarshallingCharSet);

                        var bufferType = test.GetTypeMembers().Single();
                        Assert.Equal("Test.<Field>e__FixedBuffer", bufferType.ToTestDisplayString());
                        Assert.Equal(LayoutKind.Sequential, bufferType.Layout.Kind);
                        Assert.Equal(CharSet.Ansi, bufferType.MarshallingCharSet);
                    });
            }
        }

        [Fact, WorkItem(26688, "https://github.com/dotnet/roslyn/issues/26688")]
        public void FixedFieldDoesNotRequirePinningWithThis()
        {
            CompileAndVerify(@"
using System;
unsafe struct Foo
{
    public fixed int Bar[2];

    public Foo(int value1, int value2)
    {
        this.Bar[0] = value1;
        this.Bar[1] = value2;
    }

    public int M1 => this.Bar[0];
    public int M2 => Bar[1];
}
class Program
{
    static void Main()
    {
        Foo foo = new Foo(1, 2);

        Console.WriteLine(foo.M1);
        Console.WriteLine(foo.M2);
    }
}", options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput: @"
1
2");
        }

        [Fact, WorkItem(26743, "https://github.com/dotnet/roslyn/issues/26743")]
        public void FixedFieldDoesNotAllowAddressOfOperator()
        {
            CreateCompilation(@"
unsafe struct Foo
{
    private fixed int Bar[2];

    public int* M1 => &this.Bar[0];
    public int* M2 => &Bar[1];
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,23): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     public int* M1 => &this.Bar[0];
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&this.Bar[0]").WithLocation(6, 23),
                // (7,23): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     public int* M2 => &Bar[1];
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&Bar[1]").WithLocation(7, 23));
        }
    }
}
