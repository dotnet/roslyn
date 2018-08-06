﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenReadOnlySpanConstructionTest : CSharpTestBase
    {
        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void EmptyOrNullStringConv()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
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

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "TrueTrue", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (System.ReadOnlySpan<char> V_0, //s1
                System.ReadOnlySpan<char> V_1) //s2
  IL_0000:  ldstr      """"
  IL_0005:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldstr      """"
  IL_0010:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0024:  ceq
  IL_0026:  call       ""void System.Console.Write(bool)""
  IL_002b:  ldnull
  IL_002c:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_0031:  stloc.0
  IL_0032:  ldnull
  IL_0033:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
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
  IL_0009:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.0304DE2B7DF2D15400D2997C7318A0237A5E33D3""
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
  IL_0009:  ldtoken    ""long <PrivateImplementationDetails>.7CF9F8998983B6C88C228229964D73D4717979C1""
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
  IL_0002:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.0C7A623FD2BBC05B06423BE359E4021D36E721AD""
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
  IL_0029:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.0C7A623FD2BBC05B06423BE359E4021D36E721AD""
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
        public void EnumArrayCtorPEverify()
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

        [Fact]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        public void ConvInMethodCall()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
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

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "P10", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  3
  IL_0000:  ldstr      ""QWERTYUIOP""
  IL_0005:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_000a:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.C5391E308AF25B42D5934D6A201A34E898D255C6""
  IL_000f:  ldc.i4.s   10
  IL_0011:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_0016:  call       ""void Test.Test1<char, byte>(System.ReadOnlySpan<char>, System.ReadOnlySpan<byte>)""
  IL_001b:  ret
}");
        }

    }
}
