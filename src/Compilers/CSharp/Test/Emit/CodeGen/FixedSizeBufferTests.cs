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
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12")
                .VerifyIL("Program.Main",
@"{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (S V_0, //s
  int* V_1) //p
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldflda     ""int* S.x""" /* Note: IL dumper displays the language view of the symbol's type, not the type in the generated metadata */ + @"
  IL_0007:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_000c:  conv.u
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldflda     ""int* S.x""
  IL_0015:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_001a:  conv.u
  IL_001b:  ldc.i4.s   12
  IL_001d:  stind.i4
  IL_001e:  ldloc.1
  IL_001f:  ldc.i4.4
  IL_0020:  add
  IL_0021:  ldloc.1
  IL_0022:  ldind.i4
  IL_0023:  stind.i4
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     ""int* S.x""
  IL_002b:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_0030:  conv.u
  IL_0031:  ldc.i4.4
  IL_0032:  add
  IL_0033:  ldind.i4
  IL_0034:  call       ""void System.Console.WriteLine(int)""
  IL_0039:  ret
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
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12")
                .VerifyIL("Program.Main",
@"{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (pinned int& V_0) //p
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldflda     ""S C.s""
  IL_000a:  ldflda     ""int* S.x""
  IL_000f:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""" /* Note the absence of conv.u here */ + @"
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  conv.i
  IL_0017:  ldc.i4.s   12
  IL_0019:  stind.i4
  IL_001a:  ldloc.0
  IL_001b:  conv.i
  IL_001c:  ldc.i4.4
  IL_001d:  add
  IL_001e:  ldloc.0
  IL_001f:  conv.i
  IL_0020:  ldind.i4
  IL_0021:  stind.i4
  IL_0022:  ldloc.0
  IL_0023:  conv.i
  IL_0024:  ldc.i4.4
  IL_0025:  add
  IL_0026:  ldind.i4
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  ldc.i4.0
  IL_002d:  conv.u
  IL_002e:  stloc.0
  IL_002f:  ret
}");
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
            var comp1 = CompileAndVerify(s1, options: TestOptions.UnsafeReleaseDll).Compilation;

            var comp2 = CompileAndVerify(s2,
                options: TestOptions.UnsafeReleaseExe,
                additionalRefs: new MetadataReference[] { MetadataReference.CreateFromStream(comp1.EmitToStream()) },
                expectedOutput: "12").Compilation;

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
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "12")
                .VerifyIL("Program.Main",
@"{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  dup
  IL_0004:  ldflda     ""int* S.x""
  IL_0009:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_000e:  conv.u
  IL_000f:  ldc.i4.s   12
  IL_0011:  stind.i4
  IL_0012:  ldflda     ""int* S.x""
  IL_0017:  ldflda     ""int S.<x>e__FixedBuffer.FixedElementField""
  IL_001c:  conv.u
  IL_001d:  ldind.i4
  IL_001e:  call       ""void System.Console.WriteLine(int)""
  IL_0023:  ret
}");
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
            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
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
}
";
            // CONSIDER: Dev11 reports CS1666 (ERR_FixedBufferNotFixed), but that's no more helpful.
            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,17): error CS0120: An object reference is required for the non-static field, method, or property 'S.G'
                //     fixed int F[G];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "G").WithArguments("S.G"));
        }

        [Fact]
        [WorkItem(586977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586977")]
        public void FixedSizeBufferInFixedSizeBufferSize_Cycle()
        {
            var source = @"
unsafe struct S
{
    fixed int F[F];
}
";
            // CONSIDER: Dev11 also reports CS0110 (ERR_CircConstValue), but Roslyn doesn't regard this as a cycle:
            // F has no initializer, so it has no constant value, so the constant value of F is "null" - not "the 
            // constant value of F" (i.e. cyclic).
            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,17): error CS0120: An object reference is required for the non-static field, method, or property 'S.F'
                //     fixed int F[F];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F").WithArguments("S.F"));
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
            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
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
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: "133")
                .VerifyIL("Program.Test",
@"{
  // Code size       20 (0x14)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""uint* AssemblyRecord.StartOfTables""
  IL_0006:  ldflda     ""uint AssemblyRecord.<StartOfTables>e__FixedBuffer.FixedElementField""
  IL_000b:  conv.u
  IL_000c:  ldc.i4.s   11
  IL_000e:  conv.i
  IL_000f:  ldc.i4.4
  IL_0010:  mul
  IL_0011:  add
  IL_0012:  ldind.u4
  IL_0013:  ret
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
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
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
                    CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll,
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
                CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll,
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
    }
}
