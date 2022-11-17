﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenReadOnlySpanConstructionTest : CSharpTestBase
    {
        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void EmptyOrNullStringConv()
        {
            var comp = CreateCompilation(@"
using System;

class Test
{
    public static void Main()
    {       
        var s1 = (ReadOnlySpan<char>)"""";
        var s2 = (ReadOnlySpan<char>)"""";

        Console.Write(s1.Length == s2.Length);

        s1 = (ReadOnlySpan<char>)(string)null;
        s2 = (ReadOnlySpan<char>)(string)null;

        Console.Write(s1.Length == s2.Length);
    }
}

", targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "TrueTrue", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (System.ReadOnlySpan<char> V_0, //s1
                System.ReadOnlySpan<char> V_1) //s2
  IL_0000:  ldstr      """"
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldstr      """"
  IL_0010:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0024:  ceq
  IL_0026:  call       ""void System.Console.Write(bool)""
  IL_002b:  ldnull
  IL_002c:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_0031:  stloc.0
  IL_0032:  ldnull
  IL_0033:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_0038:  stloc.1
  IL_0039:  ldloca.s   V_0
  IL_003b:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0040:  ldloca.s   V_1
  IL_0042:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0047:  ceq
  IL_0049:  call       ""void System.Console.Write(bool)""
  IL_004e:  ret
}");
        }

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void EmptyOrNullArrayConv()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var s1 = (ReadOnlySpan<byte>)new byte[]{};
        var s2 = (ReadOnlySpan<byte>)new byte[]{};

        Console.Write(s1.Length == s2.Length);

        s1 = (ReadOnlySpan<byte>)(byte[])null;
        s2 = (ReadOnlySpan<byte>)(byte[])null;

        Console.Write(s1.Length == s2.Length);
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "TrueTrue", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       77 (0x4d)
  .maxstack  2
  .locals init (System.ReadOnlySpan<byte> V_0, //s1
                System.ReadOnlySpan<byte> V_1, //s2
                System.ReadOnlySpan<byte> V_2)
  IL_0000:  ldloca.s   V_2
  IL_0002:  initobj    ""System.ReadOnlySpan<byte>""
  IL_0008:  ldloc.2
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_2
  IL_000c:  initobj    ""System.ReadOnlySpan<byte>""
  IL_0012:  ldloc.2
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       ""int System.ReadOnlySpan<byte>.Length.get""
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       ""int System.ReadOnlySpan<byte>.Length.get""
  IL_0022:  ceq
  IL_0024:  call       ""void System.Console.Write(bool)""
  IL_0029:  ldnull
  IL_002a:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
  IL_002f:  stloc.0
  IL_0030:  ldnull
  IL_0031:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
  IL_0036:  stloc.1
  IL_0037:  ldloca.s   V_0
  IL_0039:  call       ""int System.ReadOnlySpan<byte>.Length.get""
  IL_003e:  ldloca.s   V_1
  IL_0040:  call       ""int System.ReadOnlySpan<byte>.Length.get""
  IL_0045:  ceq
  IL_0047:  call       ""void System.Console.Write(bool)""
  IL_004c:  ret
}");
        }

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void EmptyArrayCtor()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        // inplace inits
        var s1 = new ReadOnlySpan<sbyte>(new sbyte[]{});
        var s2 = new ReadOnlySpan<sbyte>(new sbyte[]{});

        Console.Write(s1.Length == s2.Length);

        // make an instance
        Console.Write(s1.Length == new ReadOnlySpan<sbyte>(new sbyte[]{}).Length);
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "TrueTrue", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (System.ReadOnlySpan<sbyte> V_0, //s1
                System.ReadOnlySpan<sbyte> V_1, //s2
                System.ReadOnlySpan<sbyte> V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.ReadOnlySpan<sbyte>""
  IL_0008:  ldloca.s   V_1
  IL_000a:  initobj    ""System.ReadOnlySpan<sbyte>""
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""int System.ReadOnlySpan<sbyte>.Length.get""
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""int System.ReadOnlySpan<sbyte>.Length.get""
  IL_001e:  ceq
  IL_0020:  call       ""void System.Console.Write(bool)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""int System.ReadOnlySpan<sbyte>.Length.get""
  IL_002c:  ldloca.s   V_2
  IL_002e:  initobj    ""System.ReadOnlySpan<sbyte>""
  IL_0034:  ldloc.2
  IL_0035:  stloc.2
  IL_0036:  ldloca.s   V_2
  IL_0038:  call       ""int System.ReadOnlySpan<sbyte>.Length.get""
  IL_003d:  ceq
  IL_003f:  call       ""void System.Console.Write(bool)""
  IL_0044:  ret
}");
        }

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void NotConstArrayCtor()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    static int[] arr = new int[]{1, 2, int.Parse(""3""), 4}; 

    public static void Main()
    {       
        var s1 = new ReadOnlySpan<int>(new int[]{1, 2, int.Parse(""3""), 4});
        var s2 = new ReadOnlySpan<int>(arr);

        Console.Write(s1[2] == s2[2]);
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "True", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       75 (0x4b)
  .maxstack  5
  .locals init (System.ReadOnlySpan<int> V_0, //s1
                System.ReadOnlySpan<int> V_1) //s2
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.4
  IL_0003:  newarr     ""int""
  IL_0008:  dup
  IL_0009:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.4A8C2B3FDBE4BA9BAB0F5168A74E3370B85D6A418160E46C55C26B8EADCBE89F""
  IL_000e:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0013:  dup
  IL_0014:  ldc.i4.2
  IL_0015:  ldstr      ""3""
  IL_001a:  call       ""int int.Parse(string)""
  IL_001f:  stelem.i4
  IL_0020:  call       ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_0025:  ldloca.s   V_1
  IL_0027:  ldsfld     ""int[] Test.arr""
  IL_002c:  call       ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_0031:  ldloca.s   V_0
  IL_0033:  ldc.i4.2
  IL_0034:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0039:  ldind.i4
  IL_003a:  ldloca.s   V_1
  IL_003c:  ldc.i4.2
  IL_003d:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0042:  ldind.i4
  IL_0043:  ceq
  IL_0045:  call       ""void System.Console.Write(bool)""
  IL_004a:  ret
}");
        }

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void NotConstArrayCtorByte()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    static byte[] arr = new byte[]{1, 2, byte.Parse(""3""), 4, 5, 6, 7, 8}; 

    public static void Main()
    {       
        var s1 = new ReadOnlySpan<byte>(new byte[]{1, 2, byte.Parse(""3""), 4, 5, 6, 7, 8});
        var s2 = new ReadOnlySpan<byte>(arr);

        Console.Write(s1[2] == s2[2]);
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "True", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       75 (0x4b)
  .maxstack  5
  .locals init (System.ReadOnlySpan<byte> V_0, //s1
                System.ReadOnlySpan<byte> V_1) //s2
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.8
  IL_0003:  newarr     ""byte""
  IL_0008:  dup
  IL_0009:  ldtoken    ""long <PrivateImplementationDetails>.314FBB53F9F65BE9B88C66C76B51D81399A1035DEDE102E26DFE2E23A227D365""
  IL_000e:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0013:  dup
  IL_0014:  ldc.i4.2
  IL_0015:  ldstr      ""3""
  IL_001a:  call       ""byte byte.Parse(string)""
  IL_001f:  stelem.i1
  IL_0020:  call       ""System.ReadOnlySpan<byte>..ctor(byte[])""
  IL_0025:  ldloca.s   V_1
  IL_0027:  ldsfld     ""byte[] Test.arr""
  IL_002c:  call       ""System.ReadOnlySpan<byte>..ctor(byte[])""
  IL_0031:  ldloca.s   V_0
  IL_0033:  ldc.i4.2
  IL_0034:  call       ""ref readonly byte System.ReadOnlySpan<byte>.this[int].get""
  IL_0039:  ldind.u1
  IL_003a:  ldloca.s   V_1
  IL_003c:  ldc.i4.2
  IL_003d:  call       ""ref readonly byte System.ReadOnlySpan<byte>.this[int].get""
  IL_0042:  ldind.u1
  IL_0043:  ceq
  IL_0045:  call       ""void System.Console.Write(bool)""
  IL_004a:  ret
}");
        }

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void NotBlittableArrayConv()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var s1 = (ReadOnlySpan<object>)new object[]{1, 2, int.Parse(""3""), 4};

        Console.Write(s1[2]);
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "3", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       72 (0x48)
  .maxstack  4
  .locals init (System.ReadOnlySpan<object> V_0) //s1
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""object""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  box        ""int""
  IL_000e:  stelem.ref
  IL_000f:  dup
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.2
  IL_0012:  box        ""int""
  IL_0017:  stelem.ref
  IL_0018:  dup
  IL_0019:  ldc.i4.2
  IL_001a:  ldstr      ""3""
  IL_001f:  call       ""int int.Parse(string)""
  IL_0024:  box        ""int""
  IL_0029:  stelem.ref
  IL_002a:  dup
  IL_002b:  ldc.i4.3
  IL_002c:  ldc.i4.4
  IL_002d:  box        ""int""
  IL_0032:  stelem.ref
  IL_0033:  call       ""System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])""
  IL_0038:  stloc.0
  IL_0039:  ldloca.s   V_0
  IL_003b:  ldc.i4.2
  IL_003c:  call       ""ref readonly object System.ReadOnlySpan<object>.this[int].get""
  IL_0041:  ldind.ref
  IL_0042:  call       ""void System.Console.Write(object)""
  IL_0047:  ret
}");
        }

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void EnumArrayCtor()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {
        // inplace
        var s1 = new ReadOnlySpan<Color>(new Color[] {Color.Red, Color.Green, Color.Blue});

        Console.Write(s1[2]);

        // new instance
        Console.Write(s1[1] == new ReadOnlySpan<Color>(new Color[] { Color.Red, Color.Green, Color.Blue })[1]);
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "BlueTrue", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       70 (0x46)
  .maxstack  3
  .locals init (System.ReadOnlySpan<System.Color> V_0, //s1
                System.ReadOnlySpan<System.Color> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.AE4B3280E56E2FAF83F414A6E3DABE9D5FBE18976544C05FED121ACCB85B53FC""
  IL_0007:  ldc.i4.3
  IL_0008:  call       ""System.ReadOnlySpan<System.Color>..ctor(void*, int)""
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.2
  IL_0010:  call       ""ref readonly System.Color System.ReadOnlySpan<System.Color>.this[int].get""
  IL_0015:  ldind.i1
  IL_0016:  box        ""System.Color""
  IL_001b:  call       ""void System.Console.Write(object)""
  IL_0020:  ldloca.s   V_0
  IL_0022:  ldc.i4.1
  IL_0023:  call       ""ref readonly System.Color System.ReadOnlySpan<System.Color>.this[int].get""
  IL_0028:  ldind.i1
  IL_0029:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.AE4B3280E56E2FAF83F414A6E3DABE9D5FBE18976544C05FED121ACCB85B53FC""
  IL_002e:  ldc.i4.3
  IL_002f:  newobj     ""System.ReadOnlySpan<System.Color>..ctor(void*, int)""
  IL_0034:  stloc.1
  IL_0035:  ldloca.s   V_1
  IL_0037:  ldc.i4.1
  IL_0038:  call       ""ref readonly System.Color System.ReadOnlySpan<System.Color>.this[int].get""
  IL_003d:  ldind.i1
  IL_003e:  ceq
  IL_0040:  call       ""void System.Console.Write(bool)""
  IL_0045:  ret
}");
        }

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void EnumArrayCtorPEVerify()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {
        // inplace
        var s1 = new ReadOnlySpan<Color>(new Color[] {Color.Red, Color.Green, Color.Blue});

        Console.Write(s1[2]);

        // new instance
        Console.Write(s1[1] == new ReadOnlySpan<Color>(new Color[] { Color.Red, Color.Green, Color.Blue })[1]);
    }
}

", TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            CompileAndVerify(comp, expectedOutput: "BlueTrue", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       86 (0x56)
  .maxstack  5
  .locals init (System.ReadOnlySpan<System.Color> V_0, //s1
                System.ReadOnlySpan<System.Color> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  newarr     ""System.Color""
  IL_0008:  dup
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.1
  IL_000b:  stelem.i1
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  ldc.i4.2
  IL_000f:  stelem.i1
  IL_0010:  call       ""System.ReadOnlySpan<System.Color>..ctor(System.Color[])""
  IL_0015:  ldloca.s   V_0
  IL_0017:  ldc.i4.2
  IL_0018:  call       ""ref readonly System.Color System.ReadOnlySpan<System.Color>.this[int].get""
  IL_001d:  ldind.i1
  IL_001e:  box        ""System.Color""
  IL_0023:  call       ""void System.Console.Write(object)""
  IL_0028:  ldloca.s   V_0
  IL_002a:  ldc.i4.1
  IL_002b:  call       ""ref readonly System.Color System.ReadOnlySpan<System.Color>.this[int].get""
  IL_0030:  ldind.i1
  IL_0031:  ldc.i4.3
  IL_0032:  newarr     ""System.Color""
  IL_0037:  dup
  IL_0038:  ldc.i4.1
  IL_0039:  ldc.i4.1
  IL_003a:  stelem.i1
  IL_003b:  dup
  IL_003c:  ldc.i4.2
  IL_003d:  ldc.i4.2
  IL_003e:  stelem.i1
  IL_003f:  newobj     ""System.ReadOnlySpan<System.Color>..ctor(System.Color[])""
  IL_0044:  stloc.1
  IL_0045:  ldloca.s   V_1
  IL_0047:  ldc.i4.1
  IL_0048:  call       ""ref readonly System.Color System.ReadOnlySpan<System.Color>.this[int].get""
  IL_004d:  ldind.i1
  IL_004e:  ceq
  IL_0050:  call       ""void System.Console.Write(bool)""
  IL_0055:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void ConvInMethodCall()
        {
            var comp = CreateCompilation(@"
using System;

class Test
{
    public static void Main()
    {
        Test1<char, byte>(""QWERTYUIOP"", new byte[]{1,2,3,4,5,6,7,8,9,10});
    }

    public static void Test1<T, U>(ReadOnlySpan<T> arg1, ReadOnlySpan<U> arg2)
    {
        Console.Write(arg1[arg1.Length - 1]);
        Console.Write(arg2[arg1.Length - 1]);
    }
}

", targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "P10", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  3
  IL_0000:  ldstr      ""QWERTYUIOP""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.C848E1013F9F04A9D63FA43CE7FD4AF035152C7C669A4A404B67107CEE5F2E4E""
  IL_000f:  ldc.i4.s   10
  IL_0011:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_0016:  call       ""void Test.Test1<char, byte>(System.ReadOnlySpan<char>, System.ReadOnlySpan<byte>)""
  IL_001b:  ret
}");
        }

        [Fact]
        [WorkItem(31685, "https://github.com/dotnet/roslyn/issues/31685")]
        public void ImplicitSpanConversionInLambdaInGenericMethod_01()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {
    }

    static void M1<T>(T[] a)
    {
        // case 1: lambda
        Action<T[]> f = a2 =>
        {
            ReadOnlySpan<T> span;
            span = a2;
            T datum = span[0];
        };
    }

    // case 2: iterator method
    System.Collections.Generic.IEnumerator<T> M2<T>(T[] a)
    {
        ReadOnlySpan<T> span;
        span = a;
        T datum = span[0];
        yield break;
    }
}
", WithNullableEnable(TestOptions.ReleaseExe));
            var cv = CompileAndVerify(comp, expectedOutput: "", verify: Verification.Passes);
            cv.VerifyIL("Test.<>c__1<T>.<M1>b__1_0(T[])", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (System.ReadOnlySpan<T> V_0) //span
  IL_0000:  ldarg.1
  IL_0001:  call       ""System.ReadOnlySpan<T> System.ReadOnlySpan<T>.op_Implicit(T[])""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.0
  IL_000a:  call       ""ref readonly T System.ReadOnlySpan<T>.this[int].get""
  IL_000f:  pop
  IL_0010:  ret
}");
            cv.VerifyIL("Test.<M2>d__2<T>.System.Collections.IEnumerator.MoveNext()", @"{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0,
                System.ReadOnlySpan<T> V_1) //span
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<M2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_000c
  IL_000a:  ldc.i4.0
  IL_000b:  ret
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.m1
  IL_000e:  stfld      ""int Test.<M2>d__2<T>.<>1__state""
  IL_0013:  ldarg.0
  IL_0014:  ldfld      ""T[] Test.<M2>d__2<T>.a""
  IL_0019:  call       ""System.ReadOnlySpan<T> System.ReadOnlySpan<T>.op_Implicit(T[])""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldc.i4.0
  IL_0022:  call       ""ref readonly T System.ReadOnlySpan<T>.this[int].get""
  IL_0027:  pop
  IL_0028:  ldc.i4.0
  IL_0029:  ret
}");
        }

        [Fact]
        [WorkItem(31685, "https://github.com/dotnet/roslyn/issues/31685")]
        public void ImplicitSpanConversionInLambdaInGenericMethod_02()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

public class X
{
    public static Func<int, TSrc> Outer<TSrc>(TSrc[] a)
    {
        return (int x) => {
            ReadOnlySpan<TSrc> s = a;
            return s[x];
        };
    }

    public static void Main()
    {
        int[] i = new int[] { 0, 1, 100 };
        var d = Outer<int>(i);
        System.Console.WriteLine(d(2));
    }
}
", WithNullableEnable(TestOptions.ReleaseExe));
            var cv = CompileAndVerify(comp, expectedOutput: "100", verify: Verification.Passes);
            cv.VerifyIL("X.<>c__DisplayClass0_0<TSrc>.<Outer>b__0(int)", @"{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.ReadOnlySpan<TSrc> V_0) //s
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""TSrc[] X.<>c__DisplayClass0_0<TSrc>.a""
  IL_0006:  call       ""System.ReadOnlySpan<TSrc> System.ReadOnlySpan<TSrc>.op_Implicit(TSrc[])""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldarg.1
  IL_000f:  call       ""ref readonly TSrc System.ReadOnlySpan<TSrc>.this[int].get""
  IL_0014:  ldobj      ""TSrc""
  IL_0019:  ret
}");
        }

        [Fact]
        [WorkItem(24621, "https://github.com/dotnet/roslyn/issues/24621")]
        public void StaticFieldIsUsedForSpanCreatedFromArrayWithInitializer_Verifiable()
        {
            var csharp = @"
using System;

public class Test
{
    public static ReadOnlySpan<byte> StaticData => new byte[] { 10, 20 };

    public static void Main()
    {
        foreach (var item in StaticData)
        {
            Console.Write(item + "";"");
        }
    }
}";
            var compilationOptions = TestOptions.ReleaseExe;
            var parseOptions = CSharpParseOptions.Default.WithPEVerifyCompatFeature();
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, compilationOptions, parseOptions);
            var verifier = CompileAndVerify(compilation, expectedOutput: "10;20;", verify: Verification.Skipped);
            verifier.VerifyIL("Test.StaticData.get", @"{
  // Code size       22 (0x16)
  .maxstack  4
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   10
  IL_000a:  stelem.i1
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.s   20
  IL_000f:  stelem.i1
  IL_0010:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
  IL_0015:  ret
}");
        }

        [Fact]
        [WorkItem(24621, "https://github.com/dotnet/roslyn/issues/24621")]
        public void StaticFieldIsUsedForSpanCreatedFromArrayWithInitializer_01()
        {
            var csharp = @"
using System;

public class Test
{
    public static ReadOnlySpan<byte> StaticData => new byte[] { 10, 20 };

    public static void Main()
    {
        foreach (var item in StaticData)
        {
            Console.Write(item + "";"");
        }
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "10;20;", verify: Verification.Skipped);
            verifier.VerifyIL("Test.StaticData.get", @"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""short <PrivateImplementationDetails>.C330FA753AC5BE3B8FCB52745062F781CC9E0F4FA981A2BD06FCB969355B9469""
  IL_0005:  ldc.i4.2
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}");
        }

        [Fact]
        public void StaticFieldIsUsedForSpanCreatedFromArrayWithInitializer_02()
        {
            // This IL applies CompilerFeatureRequiredAttribute to WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer.
            // That should prevent its usage during code gen, as if the member doesn't exist.
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit System.ReadOnlySpan`1<T>
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 52 54 79 70 65 73 20 77 69 74 68 20 65 6d
        62 65 64 64 65 64 20 72 65 66 65 72 65 6e 63 65
        73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f 72
        74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72 73
        69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d 70
        69 6c 65 72 2e 01 00 00
    )
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
        01 00 00 00
    )
    .pack 0
    .size 1

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            void* pointer,
            int32 length
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 04 54 65 73 74 00 00
        )

        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            !T[] arr
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static 
        valuetype System.ReadOnlySpan`1<!T> op_Implicit (
            !T[] 'array'
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] valuetype System.ReadOnlySpan`1<!T>
        )

        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
     extends [mscorlib]System.Attribute
 {
     .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
         01 00 ff 7f 00 00 02 00 54 02 0d 41 6c 6c 6f 77
         4d 75 6c 74 69 70 6c 65 01 54 02 09 49 6e 68 65
         72 69 74 65 64 00
     )
     // Fields
     .field private initonly string '<FeatureName>k__BackingField'
     .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
         01 00 00 00
     )
     .field private initonly bool '<IsOptional>k__BackingField'
     .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
         01 00 00 00
     )

     .field public static literal string RefStructs = ""RefStructs""
     .field public static literal string RequiredMembers = ""RequiredMembers""
 
     // Methods
     .method public hidebysig specialname rtspecialname 
         instance void .ctor (
             string featureName
         ) cil managed 
     {
         ldarg.0
         call instance void [mscorlib]System.Attribute::.ctor()
         ldarg.0
         ldarg.1
         stfld string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<FeatureName>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::.ctor
 
     .method public hidebysig specialname 
         instance string get_FeatureName () cil managed 
     {
         ldarg.0
         ldfld string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<FeatureName>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::get_FeatureName
 
     .method public hidebysig specialname 
         instance bool get_IsOptional () cil managed 
     {
         ldarg.0
         ldfld bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<IsOptional>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::get_IsOptional
 
     .method public hidebysig specialname 
         instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) set_IsOptional (
             bool 'value'
         ) cil managed 
     {
         ldarg.0
         ldarg.1
         stfld bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<IsOptional>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::set_IsOptional
 
     // Properties
     .property instance string FeatureName()
     {
         .get instance string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::get_FeatureName()
     }
     .property instance bool IsOptional()
     {
         .get instance bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::get_IsOptional()
         .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::set_IsOptional(bool)
     }
 
 } // end of class System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
";

            var csharp = @"
using System;

public class Test
{
    public static ReadOnlySpan<byte> StaticData => new byte[] { 10, 20 };

    public static void Main()
    {
    }
}";

            var compilation = CreateCompilationWithIL(csharp, ilSource);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);

            var expected =
@"
{
  // Code size       22 (0x16)
  .maxstack  4
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   10
  IL_000a:  stelem.i1
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.s   20
  IL_000f:  stelem.i1
  IL_0010:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
  IL_0015:  ret
}
";
            // Verify emitted IL with "bad" WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer
            verifier.VerifyIL("Test.StaticData.get", expected);

            // We should get the same IL with regular ReadOnlySpan implementation,
            // but with WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer missing
            compilation = CreateCompilationWithMscorlibAndSpan(csharp);
            compilation.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer);
            verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.StaticData.get", expected);
        }
    }
}
