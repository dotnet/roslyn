// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenReadOnlySpanConstructionTest : CSharpTestBase
    {
        private const string RuntimeHelpersCreateSpan = @"
namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static ReadOnlySpan<T> CreateSpan<T>(RuntimeFieldHandle fldHandle) => default;
    }
}";

        private const string CompilerFeatureRequiredAttributeIL = @"
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
  IL_0005:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000a:  stloc.0
  IL_000b:  ldstr      """"
  IL_0010:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0024:  ceq
  IL_0026:  call       ""void System.Console.Write(bool)""
  IL_002b:  ldnull
  IL_002c:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0031:  stloc.0
  IL_0032:  ldnull
  IL_0033:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
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

        [Theory]
        [WorkItem(23358, "https://github.com/dotnet/roslyn/issues/23358")]
        [InlineData("byte")]
        [InlineData("char")]
        [InlineData("uint")]
        [InlineData("long")]
        public void EmptyOrNullArrayConv(string type)
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@$"
using System;

class Test
{{
    public static void Main()
    {{       
        var s1 = (ReadOnlySpan<{type}>)new {type}[]{{}};
        var s2 = (ReadOnlySpan<{type}>)new {type}[]{{}};

        Console.Write(s1.Length == s2.Length);

        s1 = (ReadOnlySpan<{type}>)({type}[])null;
        s2 = (ReadOnlySpan<{type}>)({type}[])null;

        Console.Write(s1.Length == s2.Length);
    }}
}}" + (type != "byte" ? RuntimeHelpersCreateSpan : ""), TestOptions.ReleaseExe);

            CompileAndVerify(comp,
                expectedOutput: type == "byte" ? "TrueTrue" : null,
                verify: type == "byte" ? Verification.Passes : Verification.Skipped).VerifyIL("Test.Main", @$"
{{
  // Code size       77 (0x4d)
  .maxstack  2
  .locals init (System.ReadOnlySpan<{type}> V_0, //s1
                System.ReadOnlySpan<{type}> V_1, //s2
                System.ReadOnlySpan<{type}> V_2)
  IL_0000:  ldloca.s   V_2
  IL_0002:  initobj    ""System.ReadOnlySpan<{type}>""
  IL_0008:  ldloc.2
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_2
  IL_000c:  initobj    ""System.ReadOnlySpan<{type}>""
  IL_0012:  ldloc.2
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       ""int System.ReadOnlySpan<{type}>.Length.get""
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       ""int System.ReadOnlySpan<{type}>.Length.get""
  IL_0022:  ceq
  IL_0024:  call       ""void System.Console.Write(bool)""
  IL_0029:  ldnull
  IL_002a:  call       ""System.ReadOnlySpan<{type}> System.ReadOnlySpan<{type}>.op_Implicit({type}[])""
  IL_002f:  stloc.0
  IL_0030:  ldnull
  IL_0031:  call       ""System.ReadOnlySpan<{type}> System.ReadOnlySpan<{type}>.op_Implicit({type}[])""
  IL_0036:  stloc.1
  IL_0037:  ldloca.s   V_0
  IL_0039:  call       ""int System.ReadOnlySpan<{type}>.Length.get""
  IL_003e:  ldloca.s   V_1
  IL_0040:  call       ""int System.ReadOnlySpan<{type}>.Length.get""
  IL_0045:  ceq
  IL_0047:  call       ""void System.Console.Write(bool)""
  IL_004c:  ret
}}");
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
  IL_0005:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
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
            var ilSource = CompilerFeatureRequiredAttributeIL + @"
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

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        public void StaticFieldIsUsedForSpanCreatedFromArrayWithInitializerWithExplicitLength(int length, bool valid)
        {
            var csharp = @$"
using System;

public class Test
{{
    public static ReadOnlySpan<byte> StaticData => new ReadOnlySpan<byte>(new byte[] {{ 10, 20 }}, 0, {length});
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.StaticData.get", valid ? @$"{{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""short <PrivateImplementationDetails>.C330FA753AC5BE3B8FCB52745062F781CC9E0F4FA981A2BD06FCB969355B9469""
  IL_0005:  ldc.i4.{length}
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}}" : @$"{{
  // Code size       24 (0x18)
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
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.{length}
  IL_0012:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[], int, int)""
  IL_0017:  ret
}}");
        }

        public static IEnumerable<object[]> NonSize1Type_HasCreateSpan_CreateSpanUsed_MemberData()
        {
            foreach (bool withCtor in new[] { false, true })
            {
                // A primitive can be used for the type of the blob
                yield return new object[] { withCtor, "ushort", "1", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=2_Align=2 <PrivateImplementationDetails>.47DC540C94CEB704A23875C11273E16BB0B8A87AED84DE911F2133568115F2542" };
                yield return new object[] { withCtor, "ushort", "1, 2", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=4_Align=2 <PrivateImplementationDetails>.7B11C1133330CD161071BF23A0C9B6CE5320A8F3A0F83620035A72BE46DF41042" };
                yield return new object[] { withCtor, "ushort", "1, 2, 3, 4", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=8_Align=2 <PrivateImplementationDetails>.EA99F710D9D0B8BA192295C969A63ED7CE8FC5743DA20D2057FA2B6D2C404BFB2" };
                yield return new object[] { withCtor, "uint", "1", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=4_Align=4 <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA4504" };
                yield return new object[] { withCtor, "uint", "1, 2", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=8_Align=4 <PrivateImplementationDetails>.34FB5C825DE7CA4AEA6E712F19D439C1DA0C92C37B423936C5F618545CA4FA1F4" };
                yield return new object[] { withCtor, "ulong", "1", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=8_Align=8 <PrivateImplementationDetails>.7C9FA136D4413FA6173637E883B6998D32E1D675F88CDDFF9DCBCF331820F4B88" };

                // Require a new type to be synthesized for the blob
                yield return new object[] { withCtor, "char", "'a', 'b', 'c'", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6_Align=2 <PrivateImplementationDetails>.13E228567E8249FCE53337F25D7970DE3BD68AB2653424C7B8F9FD05E33CAEDF2" };
                yield return new object[] { withCtor, "int", "1, 2, 3", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D4" };
                yield return new object[] { withCtor, "uint", "1, 2, 3", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D4" };
                yield return new object[] { withCtor, "short", "1, 2, 3", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6_Align=2 <PrivateImplementationDetails>.047DBF5366372631BA7E3E02520E651446B899C96C4B64663BAC378A298A7BF72" };
                yield return new object[] { withCtor, "ushort", "1, 2, 3", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6_Align=2 <PrivateImplementationDetails>.047DBF5366372631BA7E3E02520E651446B899C96C4B64663BAC378A298A7BF72" };
                yield return new object[] { withCtor, "long", "1, 2, 3", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24_Align=8 <PrivateImplementationDetails>.E2E2033AE7E19D680599D4EB0A1359A2B48EC5BAAC75066C317FBF85159C54EF8" };
                yield return new object[] { withCtor, "ulong", "1, 2, 3", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24_Align=8 <PrivateImplementationDetails>.E2E2033AE7E19D680599D4EB0A1359A2B48EC5BAAC75066C317FBF85159C54EF8" };
                yield return new object[] { withCtor, "float", "1.0f, 2.0f, 3.0f", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.8E628779E6A74EE0B36991C10158F63CAFEC7D340AD4E075592502C8708524DD4" };
                yield return new object[] { withCtor, "double", "1.0, 2.0, 3.0", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24_Align=8 <PrivateImplementationDetails>.A68DE4B5E96A60C8CEB3C7B7EF93461725BDBBFF3516B136585A743B5C0EC6648" };
                yield return new object[] { withCtor, "MyColor_Int16", "MyColor_Int16.Red, MyColor_Int16.Blue", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=4_Align=2 <PrivateImplementationDetails>.72034DE8A594B12DE51205FEBA7ADE26899D8425E81EAC7F8C296BF974A51C602" };
                yield return new object[] { withCtor, "MyColor_UInt16", "MyColor_UInt16.Red, MyColor_UInt16.Blue", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=4_Align=2 <PrivateImplementationDetails>.72034DE8A594B12DE51205FEBA7ADE26899D8425E81EAC7F8C296BF974A51C602" };
                yield return new object[] { withCtor, "MyColor_Int32", "MyColor_Int32.Red, MyColor_Int32.Blue", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=8_Align=4 <PrivateImplementationDetails>.1B03AB083D0FB41E44D480F48D5BBA181C623C0594BDA1AA8EA71A3B67DBF3B14" };
                yield return new object[] { withCtor, "MyColor_UInt32", "MyColor_UInt32.Red, MyColor_UInt32.Blue", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=8_Align=4 <PrivateImplementationDetails>.1B03AB083D0FB41E44D480F48D5BBA181C623C0594BDA1AA8EA71A3B67DBF3B14" };
                yield return new object[] { withCtor, "MyColor_Int64", "MyColor_Int64.Red, MyColor_Int64.Blue", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16_Align=8 <PrivateImplementationDetails>.F7548C023E431138B11357593F5CCEB9DD35EB0B0A2041F0B1560212EEB6F13E8" };
                yield return new object[] { withCtor, "MyColor_UInt64", "MyColor_UInt64.Red, MyColor_UInt64.Blue", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16_Align=8 <PrivateImplementationDetails>.F7548C023E431138B11357593F5CCEB9DD35EB0B0A2041F0B1560212EEB6F13E8" };
            }
        }

        [Theory]
        [MemberData(nameof(NonSize1Type_HasCreateSpan_CreateSpanUsed_MemberData))]
        public void NonSize1Type_HasCreateSpan_CreateSpanUsed(bool withCtor, string type, string args, string ldtokenArg)
        {
            string csharp = RuntimeHelpersCreateSpan + @"
public enum MyColor_Byte : byte { Red, Orange, Yellow, Green, Blue }
public enum MyColor_SByte : sbyte { Red, Orange, Yellow, Green, Blue }
public enum MyColor_UInt16 : ushort { Red, Orange, Yellow, Green, Blue }
public enum MyColor_Int16 : short { Red, Orange, Yellow, Green, Blue }
public enum MyColor_UInt32 : uint { Red, Orange, Yellow, Green, Blue }
public enum MyColor_Int32 : int { Red, Orange, Yellow, Green, Blue }
public enum MyColor_UInt64 : ulong { Red, Orange, Yellow, Green, Blue }
public enum MyColor_Int64 : long { Red, Orange, Yellow, Green, Blue }

public class Test
{";
            csharp += withCtor ?
                $@"    public static System.ReadOnlySpan<{type}> StaticData => new System.ReadOnlySpan<{type}>(new {type}[] {{ {args} }});" :
                $@"    public static System.ReadOnlySpan<{type}> StaticData => new {type}[] {{ {args} }};";
            csharp += "}";

            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, options: TestOptions.UnsafeReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.StaticData.get", @$"{{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldtoken    ""{ldtokenArg}""
  IL_0005:  call       ""System.ReadOnlySpan<{type}> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<{type}>(System.RuntimeFieldHandle)""
  IL_000a:  ret
}}");
        }

        [Fact]
        public void NonSize1Type_HasCreateSpan_InPlaceConstructionAttempted()
        {
            string csharp = RuntimeHelpersCreateSpan + $@"

public class Test
{{
    public static int M()
    {{
        System.ReadOnlySpan<int> span = new System.ReadOnlySpan<int>(new int[] {{ 1, 2, 3, 4, 5, 6, 7, 8 }});
        return span[7];
    }}
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, options: TestOptions.UnsafeReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.M", @$"
{{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0) //span
  IL_0000:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=4 <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A184""
  IL_0005:  call       ""System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldc.i4.7
  IL_000e:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0013:  ldind.i4
  IL_0014:  ret
}}
");
        }

        [Fact]
        public void NonSize1Type_HasCreateSpan_MultipleUsesOfSameData_ShareSameBlob()
        {
            string csharp = RuntimeHelpersCreateSpan + $@"

public class Test
{{
    public static int M()
    {{
        System.ReadOnlySpan<int> span1 = new int[] {{ 1, 2, 3, 4, 5, 6, 7, 8 }};
        int result = span1[1];

        System.ReadOnlySpan<int> span2 = new int[] {{ 9, 10, 11, 12, 13, 14, 15, 16 }};
        result += span2[2];

        System.ReadOnlySpan<int> span3 = new int[] {{ 1, 2, 3, 4, 5, 6, 7, 8 }};
        result += span3[3];

        System.ReadOnlySpan<int> span4 = new int[] {{ 9, 10, 11, 12, 13, 14, 15, 16 }};
        result += span4[4];
        
        return result;
    }}
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, options: TestOptions.UnsafeReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.M", @$"
{{
    // Code size       93 (0x5d)
    .maxstack  3
    .locals init (System.ReadOnlySpan<int> V_0, //span1
                int V_1, //result
                System.ReadOnlySpan<int> V_2, //span2
                System.ReadOnlySpan<int> V_3, //span3
                System.ReadOnlySpan<int> V_4) //span4
    IL_0000:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=4 <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A184""
    IL_0005:  call       ""System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)""
    IL_000a:  stloc.0
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldc.i4.1
    IL_000e:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0013:  ldind.i4
    IL_0014:  stloc.1
    IL_0015:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=4 <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F894""
    IL_001a:  call       ""System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)""
    IL_001f:  stloc.2
    IL_0020:  ldloc.1
    IL_0021:  ldloca.s   V_2
    IL_0023:  ldc.i4.2
    IL_0024:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0029:  ldind.i4
    IL_002a:  add
    IL_002b:  stloc.1
    IL_002c:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=4 <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A184""
    IL_0031:  call       ""System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)""
    IL_0036:  stloc.3
    IL_0037:  ldloc.1
    IL_0038:  ldloca.s   V_3
    IL_003a:  ldc.i4.3
    IL_003b:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0040:  ldind.i4
    IL_0041:  add
    IL_0042:  stloc.1
    IL_0043:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=4 <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F894""
    IL_0048:  call       ""System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)""
    IL_004d:  stloc.s    V_4
    IL_004f:  ldloc.1
    IL_0050:  ldloca.s   V_4
    IL_0052:  ldc.i4.4
    IL_0053:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0058:  ldind.i4
    IL_0059:  add
    IL_005a:  stloc.1
    IL_005b:  ldloc.1
    IL_005c:  ret
}}
");
        }

        [Fact]
        public void NonSize1Type_NoCreateSpan_MultipleUsesOfSameData_ShareSameBlobAndArrays()
        {
            string csharp = $@"

public class Test
{{
    public static int M()
    {{
        System.ReadOnlySpan<int> span1 = new int[] {{ 1, 2, 3, 4, 5, 6, 7, 8 }};
        int result = span1[1];

        System.ReadOnlySpan<int> span2 = new int[] {{ 9, 10, 11, 12, 13, 14, 15, 16 }};
        result += span2[2];

        System.ReadOnlySpan<int> span3 = new int[] {{ 1, 2, 3, 4, 5, 6, 7, 8 }};
        result += span3[3];

        System.ReadOnlySpan<int> span4 = new int[] {{ 9, 10, 11, 12, 13, 14, 15, 16 }};
        result += span4[4];
        
        return result;
    }}
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, options: TestOptions.UnsafeReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.M", @$"
{{
  // Code size      201 (0xc9)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0, //span1
                int V_1, //result
                System.ReadOnlySpan<int> V_2, //span2
                System.ReadOnlySpan<int> V_3, //span3
                System.ReadOnlySpan<int> V_4) //span4
  IL_0000:  ldsfld     ""int[] <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A18_A6""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0020
  IL_0008:  pop
  IL_0009:  ldc.i4.8
  IL_000a:  newarr     ""int""
  IL_000f:  dup
  IL_0010:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A18""
  IL_0015:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_001a:  dup
  IL_001b:  stsfld     ""int[] <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A18_A6""
  IL_0020:  newobj     ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_0025:  stloc.0
  IL_0026:  ldloca.s   V_0
  IL_0028:  ldc.i4.1
  IL_0029:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_002e:  ldind.i4
  IL_002f:  stloc.1
  IL_0030:  ldsfld     ""int[] <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F89_A6""
  IL_0035:  dup
  IL_0036:  brtrue.s   IL_0050
  IL_0038:  pop
  IL_0039:  ldc.i4.8
  IL_003a:  newarr     ""int""
  IL_003f:  dup
  IL_0040:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F89""
  IL_0045:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_004a:  dup
  IL_004b:  stsfld     ""int[] <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F89_A6""
  IL_0050:  newobj     ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_0055:  stloc.2
  IL_0056:  ldloc.1
  IL_0057:  ldloca.s   V_2
  IL_0059:  ldc.i4.2
  IL_005a:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_005f:  ldind.i4
  IL_0060:  add
  IL_0061:  stloc.1
  IL_0062:  ldsfld     ""int[] <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A18_A6""
  IL_0067:  dup
  IL_0068:  brtrue.s   IL_0082
  IL_006a:  pop
  IL_006b:  ldc.i4.8
  IL_006c:  newarr     ""int""
  IL_0071:  dup
  IL_0072:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A18""
  IL_0077:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_007c:  dup
  IL_007d:  stsfld     ""int[] <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A18_A6""
  IL_0082:  newobj     ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_0087:  stloc.3
  IL_0088:  ldloc.1
  IL_0089:  ldloca.s   V_3
  IL_008b:  ldc.i4.3
  IL_008c:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0091:  ldind.i4
  IL_0092:  add
  IL_0093:  stloc.1
  IL_0094:  ldsfld     ""int[] <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F89_A6""
  IL_0099:  dup
  IL_009a:  brtrue.s   IL_00b4
  IL_009c:  pop
  IL_009d:  ldc.i4.8
  IL_009e:  newarr     ""int""
  IL_00a3:  dup
  IL_00a4:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F89""
  IL_00a9:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00ae:  dup
  IL_00af:  stsfld     ""int[] <PrivateImplementationDetails>.71729EA83D1490C8B1B1F870F7CBA7FFBB490C71AF78C9015B49938A22E42F89_A6""
  IL_00b4:  newobj     ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_00b9:  stloc.s    V_4
  IL_00bb:  ldloc.1
  IL_00bc:  ldloca.s   V_4
  IL_00be:  ldc.i4.4
  IL_00bf:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_00c4:  ldind.i4
  IL_00c5:  add
  IL_00c6:  stloc.1
  IL_00c7:  ldloc.1
  IL_00c8:  ret
}}
");
        }

        public static IEnumerable<object[]> NonSize1Types_NoCreateSpan_UsesCachedArray_MemberData()
        {
            yield return new object[] { "ushort", "ushort", "1", 1, "ldind.u2", "short", "<PrivateImplementationDetails>.47DC540C94CEB704A23875C11273E16BB0B8A87AED84DE911F2133568115F254", "_A13" };
            yield return new object[] { "ushort", "ushort", "1, 2", 2, "ldind.u2", "int", "<PrivateImplementationDetails>.7B11C1133330CD161071BF23A0C9B6CE5320A8F3A0F83620035A72BE46DF4104", "_A13" };
            yield return new object[] { "ushort", "ushort", "1, 2, 3, 4", 4, "ldind.u2", "long", "<PrivateImplementationDetails>.EA99F710D9D0B8BA192295C969A63ED7CE8FC5743DA20D2057FA2B6D2C404BFB", "_A13" };
            yield return new object[] { "uint", "uint", "1", 1, "ldind.u4", "int", "<PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450", "_A14" };
            yield return new object[] { "uint", "uint", "1, 2", 2, "ldind.u4", "long", "<PrivateImplementationDetails>.34FB5C825DE7CA4AEA6E712F19D439C1DA0C92C37B423936C5F618545CA4FA1F", "_A14" };
            yield return new object[] { "ulong", "ulong", "1", 1, "ldind.i8", "long", "<PrivateImplementationDetails>.7C9FA136D4413FA6173637E883B6998D32E1D675F88CDDFF9DCBCF331820F4B8", "_A15" };
            yield return new object[] { "char", "char", "'a', 'b', 'c'", 3, "ldind.u2", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6", "<PrivateImplementationDetails>.13E228567E8249FCE53337F25D7970DE3BD68AB2653424C7B8F9FD05E33CAEDF", "_A1" };
            yield return new object[] { "int", "int", "1, 2, 3", 3, "ldind.i4", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12", "<PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D", "_A6" };
            yield return new object[] { "uint", "uint", "1, 2, 3", 3, "ldind.u4", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12", "<PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D", "_A14" };
            yield return new object[] { "short", "short", "1, 2, 3", 3, "ldind.i2", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6", "<PrivateImplementationDetails>.047DBF5366372631BA7E3E02520E651446B899C96C4B64663BAC378A298A7BF7", "_A5" };
            yield return new object[] { "ushort", "ushort", "1, 2, 3", 3, "ldind.u2", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6", "<PrivateImplementationDetails>.047DBF5366372631BA7E3E02520E651446B899C96C4B64663BAC378A298A7BF7", "_A13" };
            yield return new object[] { "long", "long", "1, 2, 3", 3, "ldind.i8", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24", "<PrivateImplementationDetails>.E2E2033AE7E19D680599D4EB0A1359A2B48EC5BAAC75066C317FBF85159C54EF", "_A7" };
            yield return new object[] { "ulong", "ulong", "1, 2, 3", 3, "ldind.i8", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24", "<PrivateImplementationDetails>.E2E2033AE7E19D680599D4EB0A1359A2B48EC5BAAC75066C317FBF85159C54EF", "_A15" };
            yield return new object[] { "float", "float", "1.0f, 2.0f, 3.0f", 3, "ldind.r4", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12", "<PrivateImplementationDetails>.8E628779E6A74EE0B36991C10158F63CAFEC7D340AD4E075592502C8708524DD", "_A3" };
            yield return new object[] { "double", "double", "1.0, 2.0, 3.0", 3, "ldind.r8", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24", "<PrivateImplementationDetails>.A68DE4B5E96A60C8CEB3C7B7EF93461725BDBBFF3516B136585A743B5C0EC664", "_A4" };
            yield return new object[] { "MyColor_Int16", "short", "MyColor_Int16.Red, MyColor_Int16.Blue", 2, "ldind.i2", "int", "<PrivateImplementationDetails>.72034DE8A594B12DE51205FEBA7ADE26899D8425E81EAC7F8C296BF974A51C60", "_A5" };
            yield return new object[] { "MyColor_UInt16", "ushort", "MyColor_UInt16.Red, MyColor_UInt16.Blue", 2, "ldind.u2", "int", "<PrivateImplementationDetails>.72034DE8A594B12DE51205FEBA7ADE26899D8425E81EAC7F8C296BF974A51C60", "_A13" };
            yield return new object[] { "MyColor_Int32", "int", "MyColor_Int32.Red, MyColor_Int32.Blue", 2, "ldind.i4", "long", "<PrivateImplementationDetails>.1B03AB083D0FB41E44D480F48D5BBA181C623C0594BDA1AA8EA71A3B67DBF3B1", "_A6" };
            yield return new object[] { "MyColor_UInt32", "uint", "MyColor_UInt32.Red, MyColor_UInt32.Blue", 2, "ldind.u4", "long", "<PrivateImplementationDetails>.1B03AB083D0FB41E44D480F48D5BBA181C623C0594BDA1AA8EA71A3B67DBF3B1", "_A14" };
            yield return new object[] { "MyColor_Int64", "long", "MyColor_Int64.Red, MyColor_Int64.Blue", 2, "ldind.i8", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16", "<PrivateImplementationDetails>.F7548C023E431138B11357593F5CCEB9DD35EB0B0A2041F0B1560212EEB6F13E", "_A7" };
            yield return new object[] { "MyColor_UInt64", "ulong", "MyColor_UInt64.Red, MyColor_UInt64.Blue", 2, "ldind.i8", "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16", "<PrivateImplementationDetails>.F7548C023E431138B11357593F5CCEB9DD35EB0B0A2041F0B1560212EEB6F13E", "_A15" };
        }

        [Theory]
        [MemberData(nameof(NonSize1Types_NoCreateSpan_UsesCachedArray_MemberData))]
        public void NonSize1Types_NoCreateSpan_UsesCachedArray(string type, string underlyingType, string args, int length, string ldind, string fieldType, string fieldName, string arraySuffix)
        {
            string csharp = @$"
public enum MyColor_Byte : byte {{ Red, Orange, Yellow, Green, Blue }}
public enum MyColor_SByte : sbyte {{ Red, Orange, Yellow, Green, Blue }}
public enum MyColor_UInt16 : ushort {{ Red, Orange, Yellow, Green, Blue }}
public enum MyColor_Int16 : short {{ Red, Orange, Yellow, Green, Blue }}
public enum MyColor_UInt32 : uint {{ Red, Orange, Yellow, Green, Blue }}
public enum MyColor_Int32 : int {{ Red, Orange, Yellow, Green, Blue }}
public enum MyColor_UInt64 : ulong {{ Red, Orange, Yellow, Green, Blue }}
public enum MyColor_Int64 : long {{ Red, Orange, Yellow, Green, Blue }}

public class Test
{{
    public static {type} M()
    {{
        System.ReadOnlySpan<{type}> s = new {type}[] {{ {args} }};
        return s[0];
    }}
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseDll);
            compilation.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle);

            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.M", @$"{{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (System.ReadOnlySpan<{type}> V_0) //s
  IL_0000:  ldsfld     ""{underlyingType}[] {fieldName}{arraySuffix}""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0020
  IL_0008:  pop
  IL_0009:  ldc.i4.{length}
  IL_000a:  newarr     ""{underlyingType}""
  IL_000f:  dup
  IL_0010:  ldtoken    ""{fieldType} {fieldName}""
  IL_0015:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_001a:  dup
  IL_001b:  stsfld     ""{underlyingType}[] {fieldName}{arraySuffix}""
  IL_0020:  newobj     ""System.ReadOnlySpan<{type}>..ctor({type}[])""
  IL_0025:  stloc.0
  IL_0026:  ldloca.s   V_0
  IL_0028:  ldc.i4.0
  IL_0029:  call       ""ref readonly {type} System.ReadOnlySpan<{type}>.this[int].get""
  IL_002e:  {ldind}
  IL_002f:  ret
}}");
        }

        [Fact]
        public void NonSize1Type_NoCreateSpan_UsesCachedArray_EnumsAndUnderlyingTypeShareField()
        {
            string csharp = @$"
enum Alpha1 {{ A, B, C, D }}
enum Alpha2 {{ E, F, G, H }}

public class Test
{{
    public static void Main()
    {{
        System.ReadOnlySpan<Alpha1> s1 = new Alpha1[] {{ Alpha1.A, Alpha1.B, Alpha1.C, Alpha1.D, (Alpha1)4, (Alpha1)5, (Alpha1)6, (Alpha1)7 }};
        System.ReadOnlySpan<Alpha2> s2 = new Alpha2[] {{ Alpha2.E, Alpha2.F, Alpha2.G, Alpha2.H, (Alpha2)4, (Alpha2)5, (Alpha2)6, (Alpha2)7 }};
        System.ReadOnlySpan<int> s3 = new int[] {{ 0, 1, 2, 3, 4, 5, 6, 7 }};
        System.Console.Write(s1[0]);
        System.Console.Write(s2[1]);
        System.Console.Write(s3[2]);
        System.Console.Write(s1[3]);
        System.Console.Write(s2[4]);
        System.Console.Write(s3[5]);
        System.Console.Write(s1[6]);
        System.Console.Write(s2[7]);
    }}
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle);
            var verifier = CompileAndVerify(compilation, expectedOutput: "AF2D4567", verify: Verification.Skipped);
            verifier.VerifyIL("Test.Main", @$"{{
  // Code size      257 (0x101)
  .maxstack  3
  .locals init (System.ReadOnlySpan<Alpha1> V_0, //s1
                System.ReadOnlySpan<Alpha2> V_1, //s2
                System.ReadOnlySpan<int> V_2) //s3
  IL_0000:  ldsfld     ""int[] <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40_A6""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0020
  IL_0008:  pop
  IL_0009:  ldc.i4.8
  IL_000a:  newarr     ""int""
  IL_000f:  dup
  IL_0010:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40""
  IL_0015:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_001a:  dup
  IL_001b:  stsfld     ""int[] <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40_A6""
  IL_0020:  newobj     ""System.ReadOnlySpan<Alpha1>..ctor(Alpha1[])""
  IL_0025:  stloc.0
  IL_0026:  ldsfld     ""int[] <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40_A6""
  IL_002b:  dup
  IL_002c:  brtrue.s   IL_0046
  IL_002e:  pop
  IL_002f:  ldc.i4.8
  IL_0030:  newarr     ""int""
  IL_0035:  dup
  IL_0036:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40""
  IL_003b:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0040:  dup
  IL_0041:  stsfld     ""int[] <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40_A6""
  IL_0046:  newobj     ""System.ReadOnlySpan<Alpha2>..ctor(Alpha2[])""
  IL_004b:  stloc.1
  IL_004c:  ldsfld     ""int[] <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40_A6""
  IL_0051:  dup
  IL_0052:  brtrue.s   IL_006c
  IL_0054:  pop
  IL_0055:  ldc.i4.8
  IL_0056:  newarr     ""int""
  IL_005b:  dup
  IL_005c:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40""
  IL_0061:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0066:  dup
  IL_0067:  stsfld     ""int[] <PrivateImplementationDetails>.FF1F6EE5D67458CFAC950F62E93042E21FCB867E2234DCC8721801231064AD40_A6""
  IL_006c:  newobj     ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_0071:  stloc.2
  IL_0072:  ldloca.s   V_0
  IL_0074:  ldc.i4.0
  IL_0075:  call       ""ref readonly Alpha1 System.ReadOnlySpan<Alpha1>.this[int].get""
  IL_007a:  ldind.i4
  IL_007b:  box        ""Alpha1""
  IL_0080:  call       ""void System.Console.Write(object)""
  IL_0085:  ldloca.s   V_1
  IL_0087:  ldc.i4.1
  IL_0088:  call       ""ref readonly Alpha2 System.ReadOnlySpan<Alpha2>.this[int].get""
  IL_008d:  ldind.i4
  IL_008e:  box        ""Alpha2""
  IL_0093:  call       ""void System.Console.Write(object)""
  IL_0098:  ldloca.s   V_2
  IL_009a:  ldc.i4.2
  IL_009b:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_00a0:  ldind.i4
  IL_00a1:  call       ""void System.Console.Write(int)""
  IL_00a6:  ldloca.s   V_0
  IL_00a8:  ldc.i4.3
  IL_00a9:  call       ""ref readonly Alpha1 System.ReadOnlySpan<Alpha1>.this[int].get""
  IL_00ae:  ldind.i4
  IL_00af:  box        ""Alpha1""
  IL_00b4:  call       ""void System.Console.Write(object)""
  IL_00b9:  ldloca.s   V_1
  IL_00bb:  ldc.i4.4
  IL_00bc:  call       ""ref readonly Alpha2 System.ReadOnlySpan<Alpha2>.this[int].get""
  IL_00c1:  ldind.i4
  IL_00c2:  box        ""Alpha2""
  IL_00c7:  call       ""void System.Console.Write(object)""
  IL_00cc:  ldloca.s   V_2
  IL_00ce:  ldc.i4.5
  IL_00cf:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_00d4:  ldind.i4
  IL_00d5:  call       ""void System.Console.Write(int)""
  IL_00da:  ldloca.s   V_0
  IL_00dc:  ldc.i4.6
  IL_00dd:  call       ""ref readonly Alpha1 System.ReadOnlySpan<Alpha1>.this[int].get""
  IL_00e2:  ldind.i4
  IL_00e3:  box        ""Alpha1""
  IL_00e8:  call       ""void System.Console.Write(object)""
  IL_00ed:  ldloca.s   V_1
  IL_00ef:  ldc.i4.7
  IL_00f0:  call       ""ref readonly Alpha2 System.ReadOnlySpan<Alpha2>.this[int].get""
  IL_00f5:  ldind.i4
  IL_00f6:  box        ""Alpha2""
  IL_00fb:  call       ""void System.Console.Write(object)""
  IL_0100:  ret
}}
");
        }

        [Fact]
        public void NonSize1Type_NoCreateSpan_NoArrayCtor_NotOptimized()
        {
            string csharp = @$"
public class Test
{{
    public static int M()
    {{
        System.ReadOnlySpan<int> s = new int[] {{ 1, 2, 4, 8, 16, 32, 64, 128 }};
        return s[0];
    }}
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseDll);
            compilation.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle);
            compilation.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);

            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.M", @$"{{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0) //s
  IL_0000:  ldc.i4.8
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.D497FE3BD2BF635F521DD4F07BD17E285EB24A413CACA19647209909A5612ED1""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.op_Implicit(int[])""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.0
  IL_001a:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_001f:  ldind.i4
  IL_0020:  ret
}}
");
        }

        [Fact]
        public void NonSize1Type_UnusedSpan_NothingEmitted()
        {
            string csharp = @$"
public class Test
{{
    public static int M()
    {{
        System.ReadOnlySpan<int> s = new int[] {{ 1, 2, 4, 8, 16, 32, 64, 128 }};
        return 42;
    }}
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Passes);
            verifier.VerifyIL("Test.M", @$"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  ret
}}
");
        }

        [Fact]
        public void NonSize1Type_EmptyArray_InPlace_ArrayOptimizedAway()
        {
            string csharp = @$"
public class Test
{{
    public static void Main()
    {{
        var s1 = new System.ReadOnlySpan<byte>(new byte[0] {{ }});
        var s2 = new System.ReadOnlySpan<int>(new int[0] {{ }});
        for (int i = 0; i < 2; i++)
        {{
            M(s1);
            M(s2);
        }}

        M(new System.ReadOnlySpan<char>(new char[0] {{ }}));
        M(new System.ReadOnlySpan<long>(new long[0] {{ }}));
    }}

    private static void M<T>(System.ReadOnlySpan<T> span) => System.Console.Write(span.Length);
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "000000", verify: Verification.Passes);
            verifier.VerifyIL("Test.Main", @$"{{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.ReadOnlySpan<byte> V_0, //s1
                System.ReadOnlySpan<int> V_1, //s2
                int V_2, //i
                System.ReadOnlySpan<char> V_3,
                System.ReadOnlySpan<long> V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.ReadOnlySpan<byte>""
  IL_0008:  ldloca.s   V_1
  IL_000a:  initobj    ""System.ReadOnlySpan<int>""
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.2
  IL_0012:  br.s       IL_0024
  IL_0014:  ldloc.0
  IL_0015:  call       ""void Test.M<byte>(System.ReadOnlySpan<byte>)""
  IL_001a:  ldloc.1
  IL_001b:  call       ""void Test.M<int>(System.ReadOnlySpan<int>)""
  IL_0020:  ldloc.2
  IL_0021:  ldc.i4.1
  IL_0022:  add
  IL_0023:  stloc.2
  IL_0024:  ldloc.2
  IL_0025:  ldc.i4.2
  IL_0026:  blt.s      IL_0014
  IL_0028:  ldloca.s   V_3
  IL_002a:  initobj    ""System.ReadOnlySpan<char>""
  IL_0030:  ldloc.3
  IL_0031:  call       ""void Test.M<char>(System.ReadOnlySpan<char>)""
  IL_0036:  ldloca.s   V_4
  IL_0038:  initobj    ""System.ReadOnlySpan<long>""
  IL_003e:  ldloc.s    V_4
  IL_0040:  call       ""void Test.M<long>(System.ReadOnlySpan<long>)""
  IL_0045:  ret
}}
");
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void MultipleArrays_InPlaceAndUsed()
        {
            string csharp = @$"
public class Test
{{
    public static void Main()
    {{
        System.ReadOnlySpan<byte> s1;
        Print(s1 = new System.ReadOnlySpan<byte>(new byte[] {{ 1, 2, 3 }}));
        _ = s1.IsEmpty;

        System.ReadOnlySpan<int> s2;
        Print(s2 = new System.ReadOnlySpan<int>(new int[] {{ 1, 2, 3, 4 }}));
        _ = s2.IsEmpty;

        System.ReadOnlySpan<long> s3;
        Print(s3 = new System.ReadOnlySpan<long>(new long[0] {{ }}));
        _ = s3.IsEmpty;

        System.ReadOnlySpan<nint> s4;
        Print(s4 = new System.ReadOnlySpan<nint>(new nint[] {{ 42, 43 }}));
        _ = s4.IsEmpty;
    }}

    private static void Print<T>(System.ReadOnlySpan<T> s) => System.Console.Write(s.Length);
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle);

            var peVerifyMessage = "[ : Test::Main][offset 0x00000002] Cannot modify an imaged based (RVA) static";

            var ilVerifyMessage = """
                [Main]: Cannot change initonly field outside its .ctor. { Offset = 0x2 }
                [Main]: Unexpected type on the stack. { Offset = 0x8, Found = address of '<PrivateImplementationDetails>+__StaticArrayInitTypeSize=3', Expected = Native Int }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "3402", verify: Verification.Fails with { ILVerifyMessage = ilVerifyMessage, PEVerifyMessage = peVerifyMessage });
            verifier.VerifyIL("Test.Main", """
{
  // Code size      156 (0x9c)
  .maxstack  5
  .locals init (System.ReadOnlySpan<byte> V_0, //s1
                System.ReadOnlySpan<int> V_1, //s2
                System.ReadOnlySpan<long> V_2, //s3
                System.ReadOnlySpan<nint> V_3) //s4
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldsflda    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81"
  IL_0007:  ldc.i4.3
  IL_0008:  call       "System.ReadOnlySpan<byte>..ctor(void*, int)"
  IL_000d:  ldloc.0
  IL_000e:  call       "void Test.Print<byte>(System.ReadOnlySpan<byte>)"
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       "bool System.ReadOnlySpan<byte>.IsEmpty.get"
  IL_001a:  pop
  IL_001b:  ldsfld     "int[] <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72_A6"
  IL_0020:  dup
  IL_0021:  brtrue.s   IL_003b
  IL_0023:  pop
  IL_0024:  ldc.i4.4
  IL_0025:  newarr     "int"
  IL_002a:  dup
  IL_002b:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72"
  IL_0030:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0035:  dup
  IL_0036:  stsfld     "int[] <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72_A6"
  IL_003b:  newobj     "System.ReadOnlySpan<int>..ctor(int[])"
  IL_0040:  dup
  IL_0041:  stloc.1
  IL_0042:  call       "void Test.Print<int>(System.ReadOnlySpan<int>)"
  IL_0047:  ldloca.s   V_1
  IL_0049:  call       "bool System.ReadOnlySpan<int>.IsEmpty.get"
  IL_004e:  pop
  IL_004f:  ldloca.s   V_2
  IL_0051:  initobj    "System.ReadOnlySpan<long>"
  IL_0057:  ldloc.2
  IL_0058:  call       "void Test.Print<long>(System.ReadOnlySpan<long>)"
  IL_005d:  ldloca.s   V_2
  IL_005f:  call       "bool System.ReadOnlySpan<long>.IsEmpty.get"
  IL_0064:  pop
  IL_0065:  ldloca.s   V_3
  IL_0067:  ldsfld     "nint[] <PrivateImplementationDetails>.A52856308140261655B0EC09C0AC3BD6EA183729842D3B8029A1493EA881439B_B8"
  IL_006c:  dup
  IL_006d:  brtrue.s   IL_0088
  IL_006f:  pop
  IL_0070:  ldc.i4.2
  IL_0071:  newarr     "System.IntPtr"
  IL_0076:  dup
  IL_0077:  ldc.i4.0
  IL_0078:  ldc.i4.s   42
  IL_007a:  conv.i
  IL_007b:  stelem.i
  IL_007c:  dup
  IL_007d:  ldc.i4.1
  IL_007e:  ldc.i4.s   43
  IL_0080:  conv.i
  IL_0081:  stelem.i
  IL_0082:  dup
  IL_0083:  stsfld     "nint[] <PrivateImplementationDetails>.A52856308140261655B0EC09C0AC3BD6EA183729842D3B8029A1493EA881439B_B8"
  IL_0088:  call       "System.ReadOnlySpan<nint>..ctor(nint[])"
  IL_008d:  ldloc.3
  IL_008e:  call       "void Test.Print<nint>(System.ReadOnlySpan<nint>)"
  IL_0093:  ldloca.s   V_3
  IL_0095:  call       "bool System.ReadOnlySpan<nint>.IsEmpty.get"
  IL_009a:  pop
  IL_009b:  ret
}
""");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MultipleNonSize1Types_EqualDataBlobs_HasCreateSpan_EveryAlignmentGetsUniqueTypeAndBlob()
        {
            var source = @"
using System;

class Test
{
    public static void Main()
    {       
        var s1 = (ReadOnlySpan<sbyte>)new sbyte[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        var s2 = (ReadOnlySpan<byte>)new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
        var s3 = (ReadOnlySpan<short>)new short[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        var s4 = (ReadOnlySpan<ushort>)new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };
        var s5 = (ReadOnlySpan<int>)new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
        var s6 = (ReadOnlySpan<uint>)new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        var s7 = (ReadOnlySpan<long>)new long[] { -1, -1, -1, -1 };
        var s8 = (ReadOnlySpan<ulong>)new ulong[] { 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF };
        var s9 = (ReadOnlySpan<char>)new char[] { '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF' };

        long sum = 0;
        foreach (var v1 in s1) sum += v1;
        foreach (var v2 in s2) sum += v2;
        foreach (var v3 in s3) sum += v3;
        foreach (var v4 in s4) sum += v4;
        foreach (var v5 in s5) sum += v5;
        foreach (var v6 in s6) sum += v6;
        foreach (var v7 in s7) sum += v7;
        foreach (var v8 in s8) sum += (long)v8;
        foreach (var v9 in s9) sum += v9;

        Console.Write(sum);
    }
}
";
            CompileAndVerify(source, expectedOutput: "34361843576", verify: Verification.Skipped, targetFramework: TargetFramework.Net70).VerifyIL("Test.Main", @"
{
  // Code size      528 (0x210)
  .maxstack  2
  .locals init (System.ReadOnlySpan<sbyte> V_0, //s1
                System.ReadOnlySpan<byte> V_1, //s2
                System.ReadOnlySpan<short> V_2, //s3
                System.ReadOnlySpan<ushort> V_3, //s4
                System.ReadOnlySpan<int> V_4, //s5
                System.ReadOnlySpan<uint> V_5, //s6
                System.ReadOnlySpan<long> V_6, //s7
                System.ReadOnlySpan<ulong> V_7, //s8
                System.ReadOnlySpan<char> V_8, //s9
                long V_9, //sum
                System.ReadOnlySpan<sbyte> V_10,
                int V_11,
                sbyte V_12, //v1
                System.ReadOnlySpan<byte> V_13,
                byte V_14, //v2
                System.ReadOnlySpan<short> V_15,
                short V_16, //v3
                System.ReadOnlySpan<ushort> V_17,
                ushort V_18, //v4
                System.ReadOnlySpan<int> V_19,
                int V_20, //v5
                System.ReadOnlySpan<uint> V_21,
                uint V_22, //v6
                System.ReadOnlySpan<long> V_23,
                long V_24, //v7
                System.ReadOnlySpan<ulong> V_25,
                ulong V_26, //v8
                System.ReadOnlySpan<char> V_27,
                char V_28) //v9
  IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_0005:  ldc.i4.s   32
  IL_0007:  newobj     ""System.ReadOnlySpan<sbyte>..ctor(void*, int)""
  IL_000c:  stloc.0
  IL_000d:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_0012:  ldc.i4.s   32
  IL_0014:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_0019:  stloc.1
  IL_001a:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=2 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C40512""
  IL_001f:  call       ""System.ReadOnlySpan<short> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<short>(System.RuntimeFieldHandle)""
  IL_0024:  stloc.2
  IL_0025:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=2 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C40512""
  IL_002a:  call       ""System.ReadOnlySpan<ushort> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<ushort>(System.RuntimeFieldHandle)""
  IL_002f:  stloc.3
  IL_0030:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=4 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C40514""
  IL_0035:  call       ""System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)""
  IL_003a:  stloc.s    V_4
  IL_003c:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=4 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C40514""
  IL_0041:  call       ""System.ReadOnlySpan<uint> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<uint>(System.RuntimeFieldHandle)""
  IL_0046:  stloc.s    V_5
  IL_0048:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=8 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C40518""
  IL_004d:  call       ""System.ReadOnlySpan<long> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<long>(System.RuntimeFieldHandle)""
  IL_0052:  stloc.s    V_6
  IL_0054:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=8 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C40518""
  IL_0059:  call       ""System.ReadOnlySpan<ulong> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<ulong>(System.RuntimeFieldHandle)""
  IL_005e:  stloc.s    V_7
  IL_0060:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32_Align=2 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C40512""
  IL_0065:  call       ""System.ReadOnlySpan<char> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<char>(System.RuntimeFieldHandle)""
  IL_006a:  stloc.s    V_8
  IL_006c:  ldc.i4.0
  IL_006d:  conv.i8
  IL_006e:  stloc.s    V_9
  IL_0070:  ldloc.0
  IL_0071:  stloc.s    V_10
  IL_0073:  ldc.i4.0
  IL_0074:  stloc.s    V_11
  IL_0076:  br.s       IL_0092
  IL_0078:  ldloca.s   V_10
  IL_007a:  ldloc.s    V_11
  IL_007c:  call       ""ref readonly sbyte System.ReadOnlySpan<sbyte>.this[int].get""
  IL_0081:  ldind.i1
  IL_0082:  stloc.s    V_12
  IL_0084:  ldloc.s    V_9
  IL_0086:  ldloc.s    V_12
  IL_0088:  conv.i8
  IL_0089:  add
  IL_008a:  stloc.s    V_9
  IL_008c:  ldloc.s    V_11
  IL_008e:  ldc.i4.1
  IL_008f:  add
  IL_0090:  stloc.s    V_11
  IL_0092:  ldloc.s    V_11
  IL_0094:  ldloca.s   V_10
  IL_0096:  call       ""int System.ReadOnlySpan<sbyte>.Length.get""
  IL_009b:  blt.s      IL_0078
  IL_009d:  ldloc.1
  IL_009e:  stloc.s    V_13
  IL_00a0:  ldc.i4.0
  IL_00a1:  stloc.s    V_11
  IL_00a3:  br.s       IL_00bf
  IL_00a5:  ldloca.s   V_13
  IL_00a7:  ldloc.s    V_11
  IL_00a9:  call       ""ref readonly byte System.ReadOnlySpan<byte>.this[int].get""
  IL_00ae:  ldind.u1
  IL_00af:  stloc.s    V_14
  IL_00b1:  ldloc.s    V_9
  IL_00b3:  ldloc.s    V_14
  IL_00b5:  conv.u8
  IL_00b6:  add
  IL_00b7:  stloc.s    V_9
  IL_00b9:  ldloc.s    V_11
  IL_00bb:  ldc.i4.1
  IL_00bc:  add
  IL_00bd:  stloc.s    V_11
  IL_00bf:  ldloc.s    V_11
  IL_00c1:  ldloca.s   V_13
  IL_00c3:  call       ""int System.ReadOnlySpan<byte>.Length.get""
  IL_00c8:  blt.s      IL_00a5
  IL_00ca:  ldloc.2
  IL_00cb:  stloc.s    V_15
  IL_00cd:  ldc.i4.0
  IL_00ce:  stloc.s    V_11
  IL_00d0:  br.s       IL_00ec
  IL_00d2:  ldloca.s   V_15
  IL_00d4:  ldloc.s    V_11
  IL_00d6:  call       ""ref readonly short System.ReadOnlySpan<short>.this[int].get""
  IL_00db:  ldind.i2
  IL_00dc:  stloc.s    V_16
  IL_00de:  ldloc.s    V_9
  IL_00e0:  ldloc.s    V_16
  IL_00e2:  conv.i8
  IL_00e3:  add
  IL_00e4:  stloc.s    V_9
  IL_00e6:  ldloc.s    V_11
  IL_00e8:  ldc.i4.1
  IL_00e9:  add
  IL_00ea:  stloc.s    V_11
  IL_00ec:  ldloc.s    V_11
  IL_00ee:  ldloca.s   V_15
  IL_00f0:  call       ""int System.ReadOnlySpan<short>.Length.get""
  IL_00f5:  blt.s      IL_00d2
  IL_00f7:  ldloc.3
  IL_00f8:  stloc.s    V_17
  IL_00fa:  ldc.i4.0
  IL_00fb:  stloc.s    V_11
  IL_00fd:  br.s       IL_0119
  IL_00ff:  ldloca.s   V_17
  IL_0101:  ldloc.s    V_11
  IL_0103:  call       ""ref readonly ushort System.ReadOnlySpan<ushort>.this[int].get""
  IL_0108:  ldind.u2
  IL_0109:  stloc.s    V_18
  IL_010b:  ldloc.s    V_9
  IL_010d:  ldloc.s    V_18
  IL_010f:  conv.u8
  IL_0110:  add
  IL_0111:  stloc.s    V_9
  IL_0113:  ldloc.s    V_11
  IL_0115:  ldc.i4.1
  IL_0116:  add
  IL_0117:  stloc.s    V_11
  IL_0119:  ldloc.s    V_11
  IL_011b:  ldloca.s   V_17
  IL_011d:  call       ""int System.ReadOnlySpan<ushort>.Length.get""
  IL_0122:  blt.s      IL_00ff
  IL_0124:  ldloc.s    V_4
  IL_0126:  stloc.s    V_19
  IL_0128:  ldc.i4.0
  IL_0129:  stloc.s    V_11
  IL_012b:  br.s       IL_0147
  IL_012d:  ldloca.s   V_19
  IL_012f:  ldloc.s    V_11
  IL_0131:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0136:  ldind.i4
  IL_0137:  stloc.s    V_20
  IL_0139:  ldloc.s    V_9
  IL_013b:  ldloc.s    V_20
  IL_013d:  conv.i8
  IL_013e:  add
  IL_013f:  stloc.s    V_9
  IL_0141:  ldloc.s    V_11
  IL_0143:  ldc.i4.1
  IL_0144:  add
  IL_0145:  stloc.s    V_11
  IL_0147:  ldloc.s    V_11
  IL_0149:  ldloca.s   V_19
  IL_014b:  call       ""int System.ReadOnlySpan<int>.Length.get""
  IL_0150:  blt.s      IL_012d
  IL_0152:  ldloc.s    V_5
  IL_0154:  stloc.s    V_21
  IL_0156:  ldc.i4.0
  IL_0157:  stloc.s    V_11
  IL_0159:  br.s       IL_0175
  IL_015b:  ldloca.s   V_21
  IL_015d:  ldloc.s    V_11
  IL_015f:  call       ""ref readonly uint System.ReadOnlySpan<uint>.this[int].get""
  IL_0164:  ldind.u4
  IL_0165:  stloc.s    V_22
  IL_0167:  ldloc.s    V_9
  IL_0169:  ldloc.s    V_22
  IL_016b:  conv.u8
  IL_016c:  add
  IL_016d:  stloc.s    V_9
  IL_016f:  ldloc.s    V_11
  IL_0171:  ldc.i4.1
  IL_0172:  add
  IL_0173:  stloc.s    V_11
  IL_0175:  ldloc.s    V_11
  IL_0177:  ldloca.s   V_21
  IL_0179:  call       ""int System.ReadOnlySpan<uint>.Length.get""
  IL_017e:  blt.s      IL_015b
  IL_0180:  ldloc.s    V_6
  IL_0182:  stloc.s    V_23
  IL_0184:  ldc.i4.0
  IL_0185:  stloc.s    V_11
  IL_0187:  br.s       IL_01a2
  IL_0189:  ldloca.s   V_23
  IL_018b:  ldloc.s    V_11
  IL_018d:  call       ""ref readonly long System.ReadOnlySpan<long>.this[int].get""
  IL_0192:  ldind.i8
  IL_0193:  stloc.s    V_24
  IL_0195:  ldloc.s    V_9
  IL_0197:  ldloc.s    V_24
  IL_0199:  add
  IL_019a:  stloc.s    V_9
  IL_019c:  ldloc.s    V_11
  IL_019e:  ldc.i4.1
  IL_019f:  add
  IL_01a0:  stloc.s    V_11
  IL_01a2:  ldloc.s    V_11
  IL_01a4:  ldloca.s   V_23
  IL_01a6:  call       ""int System.ReadOnlySpan<long>.Length.get""
  IL_01ab:  blt.s      IL_0189
  IL_01ad:  ldloc.s    V_7
  IL_01af:  stloc.s    V_25
  IL_01b1:  ldc.i4.0
  IL_01b2:  stloc.s    V_11
  IL_01b4:  br.s       IL_01cf
  IL_01b6:  ldloca.s   V_25
  IL_01b8:  ldloc.s    V_11
  IL_01ba:  call       ""ref readonly ulong System.ReadOnlySpan<ulong>.this[int].get""
  IL_01bf:  ldind.i8
  IL_01c0:  stloc.s    V_26
  IL_01c2:  ldloc.s    V_9
  IL_01c4:  ldloc.s    V_26
  IL_01c6:  add
  IL_01c7:  stloc.s    V_9
  IL_01c9:  ldloc.s    V_11
  IL_01cb:  ldc.i4.1
  IL_01cc:  add
  IL_01cd:  stloc.s    V_11
  IL_01cf:  ldloc.s    V_11
  IL_01d1:  ldloca.s   V_25
  IL_01d3:  call       ""int System.ReadOnlySpan<ulong>.Length.get""
  IL_01d8:  blt.s      IL_01b6
  IL_01da:  ldloc.s    V_8
  IL_01dc:  stloc.s    V_27
  IL_01de:  ldc.i4.0
  IL_01df:  stloc.s    V_11
  IL_01e1:  br.s       IL_01fd
  IL_01e3:  ldloca.s   V_27
  IL_01e5:  ldloc.s    V_11
  IL_01e7:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_01ec:  ldind.u2
  IL_01ed:  stloc.s    V_28
  IL_01ef:  ldloc.s    V_9
  IL_01f1:  ldloc.s    V_28
  IL_01f3:  conv.u8
  IL_01f4:  add
  IL_01f5:  stloc.s    V_9
  IL_01f7:  ldloc.s    V_11
  IL_01f9:  ldc.i4.1
  IL_01fa:  add
  IL_01fb:  stloc.s    V_11
  IL_01fd:  ldloc.s    V_11
  IL_01ff:  ldloca.s   V_27
  IL_0201:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0206:  blt.s      IL_01e3
  IL_0208:  ldloc.s    V_9
  IL_020a:  call       ""void System.Console.Write(long)""
  IL_020f:  ret
}");
        }

        [Fact]
        public void MultipleNonSize1Types_EqualDataBlobs_NoCreateSpan_UsesCachedArray_EveryTypeGetsUniqueField()
        {
            var compilation = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var s1 = (ReadOnlySpan<sbyte>)new sbyte[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        var s2 = (ReadOnlySpan<byte>)new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
        var s3 = (ReadOnlySpan<short>)new short[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        var s4 = (ReadOnlySpan<ushort>)new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };
        var s5 = (ReadOnlySpan<int>)new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
        var s6 = (ReadOnlySpan<uint>)new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        var s7 = (ReadOnlySpan<long>)new long[] { -1, -1, -1, -1 };
        var s8 = (ReadOnlySpan<ulong>)new ulong[] { 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF };
        var s9 = (ReadOnlySpan<char>)new char[] { '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF', '\uFFFF' };

        long sum = 0;
        foreach (var v1 in s1) sum += v1;
        foreach (var v2 in s2) sum += v2;
        foreach (var v3 in s3) sum += v3;
        foreach (var v4 in s4) sum += v4;
        foreach (var v5 in s5) sum += v5;
        foreach (var v6 in s6) sum += v6;
        foreach (var v7 in s7) sum += v7;
        foreach (var v8 in s8) sum += (long)v8;
        foreach (var v9 in s9) sum += v9;

        Console.Write(sum);
    }
}
", TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle);

            CompileAndVerify(compilation, expectedOutput: "34361843576", verify: Verification.Skipped).VerifyIL("Test.Main", @"
{
  // Code size      720 (0x2d0)
  .maxstack  3
  .locals init (System.ReadOnlySpan<sbyte> V_0, //s1
                System.ReadOnlySpan<byte> V_1, //s2
                System.ReadOnlySpan<short> V_2, //s3
                System.ReadOnlySpan<ushort> V_3, //s4
                System.ReadOnlySpan<int> V_4, //s5
                System.ReadOnlySpan<uint> V_5, //s6
                System.ReadOnlySpan<long> V_6, //s7
                System.ReadOnlySpan<ulong> V_7, //s8
                System.ReadOnlySpan<char> V_8, //s9
                long V_9, //sum
                System.ReadOnlySpan<sbyte> V_10,
                int V_11,
                sbyte V_12, //v1
                System.ReadOnlySpan<byte> V_13,
                byte V_14, //v2
                System.ReadOnlySpan<short> V_15,
                short V_16, //v3
                System.ReadOnlySpan<ushort> V_17,
                ushort V_18, //v4
                System.ReadOnlySpan<int> V_19,
                int V_20, //v5
                System.ReadOnlySpan<uint> V_21,
                uint V_22, //v6
                System.ReadOnlySpan<long> V_23,
                long V_24, //v7
                System.ReadOnlySpan<ulong> V_25,
                ulong V_26, //v8
                System.ReadOnlySpan<char> V_27,
                char V_28) //v9
  IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_0005:  ldc.i4.s   32
  IL_0007:  newobj     ""System.ReadOnlySpan<sbyte>..ctor(void*, int)""
  IL_000c:  stloc.0
  IL_000d:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_0012:  ldc.i4.s   32
  IL_0014:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_0019:  stloc.1
  IL_001a:  ldsfld     ""short[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A5""
  IL_001f:  dup
  IL_0020:  brtrue.s   IL_003b
  IL_0022:  pop
  IL_0023:  ldc.i4.s   16
  IL_0025:  newarr     ""short""
  IL_002a:  dup
  IL_002b:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_0030:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0035:  dup
  IL_0036:  stsfld     ""short[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A5""
  IL_003b:  newobj     ""System.ReadOnlySpan<short>..ctor(short[])""
  IL_0040:  stloc.2
  IL_0041:  ldsfld     ""ushort[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A13""
  IL_0046:  dup
  IL_0047:  brtrue.s   IL_0062
  IL_0049:  pop
  IL_004a:  ldc.i4.s   16
  IL_004c:  newarr     ""ushort""
  IL_0051:  dup
  IL_0052:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_0057:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_005c:  dup
  IL_005d:  stsfld     ""ushort[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A13""
  IL_0062:  newobj     ""System.ReadOnlySpan<ushort>..ctor(ushort[])""
  IL_0067:  stloc.3
  IL_0068:  ldsfld     ""int[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A6""
  IL_006d:  dup
  IL_006e:  brtrue.s   IL_0088
  IL_0070:  pop
  IL_0071:  ldc.i4.8
  IL_0072:  newarr     ""int""
  IL_0077:  dup
  IL_0078:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_007d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0082:  dup
  IL_0083:  stsfld     ""int[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A6""
  IL_0088:  newobj     ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_008d:  stloc.s    V_4
  IL_008f:  ldsfld     ""uint[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A14""
  IL_0094:  dup
  IL_0095:  brtrue.s   IL_00af
  IL_0097:  pop
  IL_0098:  ldc.i4.8
  IL_0099:  newarr     ""uint""
  IL_009e:  dup
  IL_009f:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_00a4:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00a9:  dup
  IL_00aa:  stsfld     ""uint[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A14""
  IL_00af:  newobj     ""System.ReadOnlySpan<uint>..ctor(uint[])""
  IL_00b4:  stloc.s    V_5
  IL_00b6:  ldsfld     ""long[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A7""
  IL_00bb:  dup
  IL_00bc:  brtrue.s   IL_00d6
  IL_00be:  pop
  IL_00bf:  ldc.i4.4
  IL_00c0:  newarr     ""long""
  IL_00c5:  dup
  IL_00c6:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_00cb:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00d0:  dup
  IL_00d1:  stsfld     ""long[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A7""
  IL_00d6:  newobj     ""System.ReadOnlySpan<long>..ctor(long[])""
  IL_00db:  stloc.s    V_6
  IL_00dd:  ldsfld     ""ulong[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A15""
  IL_00e2:  dup
  IL_00e3:  brtrue.s   IL_00fd
  IL_00e5:  pop
  IL_00e6:  ldc.i4.4
  IL_00e7:  newarr     ""ulong""
  IL_00ec:  dup
  IL_00ed:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_00f2:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00f7:  dup
  IL_00f8:  stsfld     ""ulong[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A15""
  IL_00fd:  newobj     ""System.ReadOnlySpan<ulong>..ctor(ulong[])""
  IL_0102:  stloc.s    V_7
  IL_0104:  ldsfld     ""char[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A1""
  IL_0109:  dup
  IL_010a:  brtrue.s   IL_0125
  IL_010c:  pop
  IL_010d:  ldc.i4.s   16
  IL_010f:  newarr     ""char""
  IL_0114:  dup
  IL_0115:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051""
  IL_011a:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_011f:  dup
  IL_0120:  stsfld     ""char[] <PrivateImplementationDetails>.AF9613760F72635FBDB44A5A0A63C39F12AF30F950A6EE5C971BE188E89C4051_A1""
  IL_0125:  newobj     ""System.ReadOnlySpan<char>..ctor(char[])""
  IL_012a:  stloc.s    V_8
  IL_012c:  ldc.i4.0
  IL_012d:  conv.i8
  IL_012e:  stloc.s    V_9
  IL_0130:  ldloc.0
  IL_0131:  stloc.s    V_10
  IL_0133:  ldc.i4.0
  IL_0134:  stloc.s    V_11
  IL_0136:  br.s       IL_0152
  IL_0138:  ldloca.s   V_10
  IL_013a:  ldloc.s    V_11
  IL_013c:  call       ""ref readonly sbyte System.ReadOnlySpan<sbyte>.this[int].get""
  IL_0141:  ldind.i1
  IL_0142:  stloc.s    V_12
  IL_0144:  ldloc.s    V_9
  IL_0146:  ldloc.s    V_12
  IL_0148:  conv.i8
  IL_0149:  add
  IL_014a:  stloc.s    V_9
  IL_014c:  ldloc.s    V_11
  IL_014e:  ldc.i4.1
  IL_014f:  add
  IL_0150:  stloc.s    V_11
  IL_0152:  ldloc.s    V_11
  IL_0154:  ldloca.s   V_10
  IL_0156:  call       ""int System.ReadOnlySpan<sbyte>.Length.get""
  IL_015b:  blt.s      IL_0138
  IL_015d:  ldloc.1
  IL_015e:  stloc.s    V_13
  IL_0160:  ldc.i4.0
  IL_0161:  stloc.s    V_11
  IL_0163:  br.s       IL_017f
  IL_0165:  ldloca.s   V_13
  IL_0167:  ldloc.s    V_11
  IL_0169:  call       ""ref readonly byte System.ReadOnlySpan<byte>.this[int].get""
  IL_016e:  ldind.u1
  IL_016f:  stloc.s    V_14
  IL_0171:  ldloc.s    V_9
  IL_0173:  ldloc.s    V_14
  IL_0175:  conv.u8
  IL_0176:  add
  IL_0177:  stloc.s    V_9
  IL_0179:  ldloc.s    V_11
  IL_017b:  ldc.i4.1
  IL_017c:  add
  IL_017d:  stloc.s    V_11
  IL_017f:  ldloc.s    V_11
  IL_0181:  ldloca.s   V_13
  IL_0183:  call       ""int System.ReadOnlySpan<byte>.Length.get""
  IL_0188:  blt.s      IL_0165
  IL_018a:  ldloc.2
  IL_018b:  stloc.s    V_15
  IL_018d:  ldc.i4.0
  IL_018e:  stloc.s    V_11
  IL_0190:  br.s       IL_01ac
  IL_0192:  ldloca.s   V_15
  IL_0194:  ldloc.s    V_11
  IL_0196:  call       ""ref readonly short System.ReadOnlySpan<short>.this[int].get""
  IL_019b:  ldind.i2
  IL_019c:  stloc.s    V_16
  IL_019e:  ldloc.s    V_9
  IL_01a0:  ldloc.s    V_16
  IL_01a2:  conv.i8
  IL_01a3:  add
  IL_01a4:  stloc.s    V_9
  IL_01a6:  ldloc.s    V_11
  IL_01a8:  ldc.i4.1
  IL_01a9:  add
  IL_01aa:  stloc.s    V_11
  IL_01ac:  ldloc.s    V_11
  IL_01ae:  ldloca.s   V_15
  IL_01b0:  call       ""int System.ReadOnlySpan<short>.Length.get""
  IL_01b5:  blt.s      IL_0192
  IL_01b7:  ldloc.3
  IL_01b8:  stloc.s    V_17
  IL_01ba:  ldc.i4.0
  IL_01bb:  stloc.s    V_11
  IL_01bd:  br.s       IL_01d9
  IL_01bf:  ldloca.s   V_17
  IL_01c1:  ldloc.s    V_11
  IL_01c3:  call       ""ref readonly ushort System.ReadOnlySpan<ushort>.this[int].get""
  IL_01c8:  ldind.u2
  IL_01c9:  stloc.s    V_18
  IL_01cb:  ldloc.s    V_9
  IL_01cd:  ldloc.s    V_18
  IL_01cf:  conv.u8
  IL_01d0:  add
  IL_01d1:  stloc.s    V_9
  IL_01d3:  ldloc.s    V_11
  IL_01d5:  ldc.i4.1
  IL_01d6:  add
  IL_01d7:  stloc.s    V_11
  IL_01d9:  ldloc.s    V_11
  IL_01db:  ldloca.s   V_17
  IL_01dd:  call       ""int System.ReadOnlySpan<ushort>.Length.get""
  IL_01e2:  blt.s      IL_01bf
  IL_01e4:  ldloc.s    V_4
  IL_01e6:  stloc.s    V_19
  IL_01e8:  ldc.i4.0
  IL_01e9:  stloc.s    V_11
  IL_01eb:  br.s       IL_0207
  IL_01ed:  ldloca.s   V_19
  IL_01ef:  ldloc.s    V_11
  IL_01f1:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_01f6:  ldind.i4
  IL_01f7:  stloc.s    V_20
  IL_01f9:  ldloc.s    V_9
  IL_01fb:  ldloc.s    V_20
  IL_01fd:  conv.i8
  IL_01fe:  add
  IL_01ff:  stloc.s    V_9
  IL_0201:  ldloc.s    V_11
  IL_0203:  ldc.i4.1
  IL_0204:  add
  IL_0205:  stloc.s    V_11
  IL_0207:  ldloc.s    V_11
  IL_0209:  ldloca.s   V_19
  IL_020b:  call       ""int System.ReadOnlySpan<int>.Length.get""
  IL_0210:  blt.s      IL_01ed
  IL_0212:  ldloc.s    V_5
  IL_0214:  stloc.s    V_21
  IL_0216:  ldc.i4.0
  IL_0217:  stloc.s    V_11
  IL_0219:  br.s       IL_0235
  IL_021b:  ldloca.s   V_21
  IL_021d:  ldloc.s    V_11
  IL_021f:  call       ""ref readonly uint System.ReadOnlySpan<uint>.this[int].get""
  IL_0224:  ldind.u4
  IL_0225:  stloc.s    V_22
  IL_0227:  ldloc.s    V_9
  IL_0229:  ldloc.s    V_22
  IL_022b:  conv.u8
  IL_022c:  add
  IL_022d:  stloc.s    V_9
  IL_022f:  ldloc.s    V_11
  IL_0231:  ldc.i4.1
  IL_0232:  add
  IL_0233:  stloc.s    V_11
  IL_0235:  ldloc.s    V_11
  IL_0237:  ldloca.s   V_21
  IL_0239:  call       ""int System.ReadOnlySpan<uint>.Length.get""
  IL_023e:  blt.s      IL_021b
  IL_0240:  ldloc.s    V_6
  IL_0242:  stloc.s    V_23
  IL_0244:  ldc.i4.0
  IL_0245:  stloc.s    V_11
  IL_0247:  br.s       IL_0262
  IL_0249:  ldloca.s   V_23
  IL_024b:  ldloc.s    V_11
  IL_024d:  call       ""ref readonly long System.ReadOnlySpan<long>.this[int].get""
  IL_0252:  ldind.i8
  IL_0253:  stloc.s    V_24
  IL_0255:  ldloc.s    V_9
  IL_0257:  ldloc.s    V_24
  IL_0259:  add
  IL_025a:  stloc.s    V_9
  IL_025c:  ldloc.s    V_11
  IL_025e:  ldc.i4.1
  IL_025f:  add
  IL_0260:  stloc.s    V_11
  IL_0262:  ldloc.s    V_11
  IL_0264:  ldloca.s   V_23
  IL_0266:  call       ""int System.ReadOnlySpan<long>.Length.get""
  IL_026b:  blt.s      IL_0249
  IL_026d:  ldloc.s    V_7
  IL_026f:  stloc.s    V_25
  IL_0271:  ldc.i4.0
  IL_0272:  stloc.s    V_11
  IL_0274:  br.s       IL_028f
  IL_0276:  ldloca.s   V_25
  IL_0278:  ldloc.s    V_11
  IL_027a:  call       ""ref readonly ulong System.ReadOnlySpan<ulong>.this[int].get""
  IL_027f:  ldind.i8
  IL_0280:  stloc.s    V_26
  IL_0282:  ldloc.s    V_9
  IL_0284:  ldloc.s    V_26
  IL_0286:  add
  IL_0287:  stloc.s    V_9
  IL_0289:  ldloc.s    V_11
  IL_028b:  ldc.i4.1
  IL_028c:  add
  IL_028d:  stloc.s    V_11
  IL_028f:  ldloc.s    V_11
  IL_0291:  ldloca.s   V_25
  IL_0293:  call       ""int System.ReadOnlySpan<ulong>.Length.get""
  IL_0298:  blt.s      IL_0276
  IL_029a:  ldloc.s    V_8
  IL_029c:  stloc.s    V_27
  IL_029e:  ldc.i4.0
  IL_029f:  stloc.s    V_11
  IL_02a1:  br.s       IL_02bd
  IL_02a3:  ldloca.s   V_27
  IL_02a5:  ldloc.s    V_11
  IL_02a7:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_02ac:  ldind.u2
  IL_02ad:  stloc.s    V_28
  IL_02af:  ldloc.s    V_9
  IL_02b1:  ldloc.s    V_28
  IL_02b3:  conv.u8
  IL_02b4:  add
  IL_02b5:  stloc.s    V_9
  IL_02b7:  ldloc.s    V_11
  IL_02b9:  ldc.i4.1
  IL_02ba:  add
  IL_02bb:  stloc.s    V_11
  IL_02bd:  ldloc.s    V_11
  IL_02bf:  ldloca.s   V_27
  IL_02c1:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_02c6:  blt.s      IL_02a3
  IL_02c8:  ldloc.s    V_9
  IL_02ca:  call       ""void System.Console.Write(long)""
  IL_02cf:  ret
}");
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(0, 4)]
        public void NonSize1Types_NonFullLength_NotOptimized(int start, int length)
        {
            var csharp = RuntimeHelpersCreateSpan + @$"
public class Test
{{
    public static System.ReadOnlySpan<char> StaticData => new System.ReadOnlySpan<char>(new char[] {{ 'a', 'b', 'c' }}, {start}, {length});
}}";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.StaticData.get", @$"{{
  // Code size       29 (0x1d)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""char""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   97
  IL_000a:  stelem.i2
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.s   98
  IL_000f:  stelem.i2
  IL_0010:  dup
  IL_0011:  ldc.i4.2
  IL_0012:  ldc.i4.s   99
  IL_0014:  stelem.i2
  IL_0015:  ldc.i4.{start}
  IL_0016:  ldc.i4.{length}
  IL_0017:  newobj     ""System.ReadOnlySpan<char>..ctor(char[], int, int)""
  IL_001c:  ret
}}");
        }

        [Fact]
        public void NonSize1Types_NoCreateSpan_BadArrayCtor_NotOptimized()
        {
            // This IL applies CompilerFeatureRequiredAttribute to WellKnownMember.System_ReadOnlySpan_T__ctor_Array.
            // That should prevent its usage during code gen, as if the member doesn't exist.
            var ilSource = CompilerFeatureRequiredAttributeIL + @"
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
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            !T[] arr
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 04 54 65 73 74 00 00
        )

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
";

            var csharp = @"
using System;

public class Test
{
    public static ReadOnlySpan<int> StaticData => new int[] { 10, 20 };

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
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   10
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.s   20
  IL_000f:  stelem.i4
  IL_0010:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.op_Implicit(int[])""
  IL_0015:  ret
}
";
            // Verify emitted IL with "bad" WellKnownMember.System_ReadOnlySpan_T__ctor_Array
            verifier.VerifyIL("Test.StaticData.get", expected);

            // We should get the same IL with regular ReadOnlySpan implementation,
            // but with WellKnownMember.System_ReadOnlySpan_T__ctor_Array missing
            compilation = CreateCompilationWithMscorlibAndSpan(csharp);
            compilation.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);
            verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("Test.StaticData.get", expected);
        }

        [Theory]
        [InlineData("byte", 1)]
        [InlineData("sbyte", 1)]
        [InlineData("short", 2)]
        [InlineData("ushort", 2)]
        [InlineData("int", 4)]
        [InlineData("uint", 4)]
        [InlineData("float", 4)]
        [InlineData("long", 8)]
        [InlineData("ulong", 8)]
        [InlineData("double", 8)]
        [InlineData("System.DayOfWeek", 4)]
        public void Alignment_FieldsAreAlignedAndPackedAccordingToType(string typeName, int expectedAlignment)
        {
            string csharp = RuntimeHelpersCreateSpan + $@"
public class Test
{{
    public static System.ReadOnlySpan<{typeName}> Data => new[] {{ ({typeName})1, ({typeName})2, ({typeName})3 }};
}}";

            var compilation = CreateCompilation(csharp, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.UnsafeReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyTypeIL("<PrivateImplementationDetails>", il =>
            {
                Assert.Contains($".pack {expectedAlignment}", il);

                Match m = Regex.Match(il, @"\.data cil I_([0-9A-F]*) = bytearray");
                Assert.True(m.Success, $"Expected regex to match in {il}");
                Assert.True(long.TryParse(m.Groups[1].Value, NumberStyles.HexNumber, null, out long rva), $"Expected {m.Value} to parse as hex long.");
                Assert.True(rva % expectedAlignment == 0, $"Expected RVA {rva:X8} to be {expectedAlignment}-byte aligned.");
            });
        }

        [Theory]
        [InlineData("byte", 1, false)]
        [InlineData("byte", 2, false)]
        [InlineData("byte", 3, true)]
        [InlineData("byte", 4, false)]
        [InlineData("byte", 8, false)]
        [InlineData("byte", 9, true)]
        [InlineData("sbyte", 1, false)]
        [InlineData("sbyte", 2, false)]
        [InlineData("sbyte", 4, false)]
        [InlineData("short", 1, true)]
        [InlineData("short", 2, true)]
        [InlineData("short", 3, true)]
        [InlineData("short", 4, true)]
        [InlineData("ushort", 1, true)]
        [InlineData("ushort", 4, true)]
        [InlineData("int", 1, true)]
        [InlineData("int", 2, true)]
        [InlineData("int", 3, true)]
        [InlineData("int", 4, true)]
        [InlineData("uint", 1, true)]
        [InlineData("uint", 2, true)]
        [InlineData("System.DayOfWeek", 1, true)]
        [InlineData("System.DayOfWeek", 2, true)]
        [InlineData("long", 1, true)]
        [InlineData("long", 2, true)]
        public void AlignmentImpactsStaticArrayTypeCreation_BuiltInTypesOnlyUsedForSize1Types(string typeName, int numValues, bool shouldGenerateType)
        {
            string values = string.Join(", ", Enumerable.Range(1, numValues).Select(i => $"({typeName}){i}"));
            string csharp = RuntimeHelpersCreateSpan + $@"
public class Test
{{
    public static System.ReadOnlySpan<{typeName}> Data => new[] {{ {values} }};
}}";

            var compilation = CreateCompilation(csharp, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.UnsafeReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyTypeIL("<PrivateImplementationDetails>", il =>
            {
                if (shouldGenerateType)
                {
                    Assert.Contains("__StaticArrayInitTypeSize=", il);
                }
                else
                {
                    Assert.DoesNotContain("__StaticArrayInitTypeSize=", il);
                }
            });
        }

        [Fact]
        public void PrivateImplementationDetails_TypesOrderedBySizeThenAlignment()
        {
            string csharp = RuntimeHelpersCreateSpan + @"
class Test
{
    public static int M()
    {       
        var s1 = (System.ReadOnlySpan<ushort>)new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };
        var s2 = (System.ReadOnlySpan<byte>)new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
        var s3 = (System.ReadOnlySpan<ulong>)new ulong[] { 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF };
        var s4 = (System.ReadOnlySpan<uint>)new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };

        var s5 = (System.ReadOnlySpan<byte>)new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
        var s6 = (System.ReadOnlySpan<ushort>)new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };
        var s7 = (System.ReadOnlySpan<uint>)new uint[] { 0xFFFFFFFF, 0xFFFFFFFF };
        var s8 = (System.ReadOnlySpan<ulong>)new ulong[] { 0xFFFFFFFFFFFFFFFF };

        var s9 = (System.ReadOnlySpan<uint>)new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        var s10 = (System.ReadOnlySpan<ulong>)new ulong[] { 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF };
        var s11 = (System.ReadOnlySpan<byte>)new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
        var s12 = (System.ReadOnlySpan<ushort>)new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };

        var s13 = (System.ReadOnlySpan<ulong>)new ulong[] { 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF };
        var s14 = (System.ReadOnlySpan<uint>)new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        var s15 = (System.ReadOnlySpan<ushort>)new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };
        var s16 = (System.ReadOnlySpan<byte>)new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };

        return
            s1.Length + s2.Length + s3.Length + s4.Length +
            s5.Length + s6.Length + s7.Length + s8.Length +
            s9.Length + s10.Length + s11.Length + s12.Length +
            s13.Length + s14.Length + s15.Length + s16.Length;
    }
}
";
            var compilation = CreateCompilation(csharp, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.UnsafeReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyTypeIL("<PrivateImplementationDetails>", il =>
            {
                string[] expected = new[]
                {
                    "__StaticArrayInitTypeSize=8_Align=2",
                    "__StaticArrayInitTypeSize=8_Align=4",
                    "__StaticArrayInitTypeSize=8_Align=8",
                    "__StaticArrayInitTypeSize=16",
                    "__StaticArrayInitTypeSize=16_Align=2",
                    "__StaticArrayInitTypeSize=16_Align=4",
                    "__StaticArrayInitTypeSize=16_Align=8",
                    "__StaticArrayInitTypeSize=24",
                    "__StaticArrayInitTypeSize=24_Align=2",
                    "__StaticArrayInitTypeSize=24_Align=4",
                    "__StaticArrayInitTypeSize=24_Align=8",
                    "__StaticArrayInitTypeSize=32",
                    "__StaticArrayInitTypeSize=32_Align=2",
                    "__StaticArrayInitTypeSize=32_Align=4",
                    "__StaticArrayInitTypeSize=32_Align=8",
                };

                // .class nested assembly explicit ansi sealed 'TYPENAME'
                string[] actual = Regex.Matches(il, @"\.class nested assembly explicit ansi sealed '([^']*?)'").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();

                Assert.Equal(expected, actual);
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_Null()
        {
            var src = $$"""
Assert(C.MString());
Assert(C.MObject());
Assert(C.MC());

System.Console.Write("ran");

static void Assert<T>(System.ReadOnlySpan<T> values)
{
    if (values.Length != 3) throw null;
    if (values[0] is not null) throw null;
    if (values[1] is not null) throw null;
    if (values[2] is not null) throw null;

    ref readonly T t1 = ref values[0];
    if (t1 is not null) throw null;

    ref readonly T t2 = ref values[1];
    if (t2 is not null) throw null;

    ref readonly T t3 = ref values[2];
    if (t3 is not null) throw null;
}

public class C
{
    public static System.ReadOnlySpan<string> MString() => new string[] { null, null, null };

    public static System.ReadOnlySpan<object> MObject() => new object[] { null, null, null };

    public static System.ReadOnlySpan<C> MC() => new C[] { null, null, null };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var ilVerifyMessage = """
                [MString]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1a }
                [MObject]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xb }
                [MC]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xb }
                """;
            var verifier = CompileAndVerify(compilation, expectedOutput: "ran",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.MString", """
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldsfld     "string[] <PrivateImplementationDetails>.709E80C88487A2411E1EE4DFB9F22A861492D20C4765150C0C794ABD70F8147C_B11"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0015
  IL_0008:  pop
  IL_0009:  ldc.i4.3
  IL_000a:  newarr     "string"
  IL_000f:  dup
  IL_0010:  stsfld     "string[] <PrivateImplementationDetails>.709E80C88487A2411E1EE4DFB9F22A861492D20C4765150C0C794ABD70F8147C_B11"
  IL_0015:  newobj     "System.ReadOnlySpan<string>..ctor(string[])"
  IL_001a:  ret
}
""");

            verifier.VerifyIL("C.MObject", """
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "object"
  IL_0006:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])"
  IL_000b:  ret
}
""");

            verifier.VerifyIL("C.MC", """
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "C"
  IL_0006:  call       "System.ReadOnlySpan<C> System.ReadOnlySpan<C>.op_Implicit(C[])"
  IL_000b:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_OtherStrings()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]} {values[1]}");

public class C
{
    public static System.ReadOnlySpan<string> M()
        => new string[] { "hello", "world" };

    public static System.ReadOnlySpan<string> M2()
        => new string[] { "hello", "world" };

    public static System.ReadOnlySpan<string> M3()
        => new string[] { "hello world" };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var ilVerifyMessage = """
                [M]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x2a }
                [M2]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x2a }
                [M3]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x22 }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "2 hello world",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            var expectedIL = """
{
  // Code size       43 (0x2b)
  .maxstack  4
  IL_0000:  ldsfld     "string[] <PrivateImplementationDetails>.13B33575336780080BB71DEC2A7434043608FF4569C0E44AD6FCFE007B5E6E06_B11"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0025
  IL_0008:  pop
  IL_0009:  ldc.i4.2
  IL_000a:  newarr     "string"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldstr      "hello"
  IL_0016:  stelem.ref
  IL_0017:  dup
  IL_0018:  ldc.i4.1
  IL_0019:  ldstr      "world"
  IL_001e:  stelem.ref
  IL_001f:  dup
  IL_0020:  stsfld     "string[] <PrivateImplementationDetails>.13B33575336780080BB71DEC2A7434043608FF4569C0E44AD6FCFE007B5E6E06_B11"
  IL_0025:  newobj     "System.ReadOnlySpan<string>..ctor(string[])"
  IL_002a:  ret
}
""";
            verifier.VerifyIL("C.M", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);

            verifier.VerifyIL("C.M3", """
{
  // Code size       35 (0x23)
  .maxstack  4
  IL_0000:  ldsfld     "string[] <PrivateImplementationDetails>.36F71D944B9FE83E99A7DA0CA583032588C05C1016F4FA965FD724A1E7D5E69C_B11"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001d
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "string"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldstr      "hello world"
  IL_0016:  stelem.ref
  IL_0017:  dup
  IL_0018:  stsfld     "string[] <PrivateImplementationDetails>.36F71D944B9FE83E99A7DA0CA583032588C05C1016F4FA965FD724A1E7D5E69C_B11"
  IL_001d:  newobj     "System.ReadOnlySpan<string>..ctor(string[])"
  IL_0022:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_OtherStrings_MissingReadOnlySpanCtor()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]} {values[1]}");

public class C
{
    public static System.ReadOnlySpan<string> M()
        => new string[] { "hello", "world" };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            compilation.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);

            var ilVerifyMessage = """
                [M]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1b }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "2 hello world",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            verifier.VerifyIL("C.M", """
{
  // Code size       28 (0x1c)
  .maxstack  4
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     "string"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      "hello"
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      "world"
  IL_0015:  stelem.ref
  IL_0016:  call       "System.ReadOnlySpan<string> System.ReadOnlySpan<string>.op_Implicit(string[])"
  IL_001b:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_VariableStrings()
        {
            var src = """
var x = C.M();
System.Console.Write($"{x[0]} {x[1]}");

public class C
{
    public static System.ReadOnlySpan<string> M()
    {
        var hello = "hello";
        var world = "world";
        return new string[] { hello, world };
    }
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var ilVerifyMessage = """
                [M]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1f }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "hello world",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            verifier.VerifyIL("C.M", """
{
  // Code size       32 (0x20)
  .maxstack  4
  .locals init (string V_0, //hello
                string V_1) //world
  IL_0000:  ldstr      "hello"
  IL_0005:  stloc.0
  IL_0006:  ldstr      "world"
  IL_000b:  stloc.1
  IL_000c:  ldc.i4.2
  IL_000d:  newarr     "string"
  IL_0012:  dup
  IL_0013:  ldc.i4.0
  IL_0014:  ldloc.0
  IL_0015:  stelem.ref
  IL_0016:  dup
  IL_0017:  ldc.i4.1
  IL_0018:  ldloc.1
  IL_0019:  stelem.ref
  IL_001a:  call       "System.ReadOnlySpan<string> System.ReadOnlySpan<string>.op_Implicit(string[])"
  IL_001f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_NoInitializer()
        {
            var src = """
public class C
{
    public static System.ReadOnlySpan<object> M() => new object[];
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            compilation.VerifyDiagnostics(
                // (3,64): error CS1586: Array creation must have array size or array initializer
                //     public static System.ReadOnlySpan<object> M() => new object[];
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(3, 64)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_NativeInts()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<nint> M()
        => new nint[] { 1 };

    public static System.ReadOnlySpan<nint> M2()
        => new nint[] { 1 };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var ilVerifyMessage = """
                [M]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1f }
                [M2]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1f }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "1 1",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            var expectedIL = """
{
  // Code size       32 (0x20)
  .maxstack  4
  IL_0000:  ldsfld     "nint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B8"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001a
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "System.IntPtr"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.1
  IL_0012:  conv.i
  IL_0013:  stelem.i
  IL_0014:  dup
  IL_0015:  stsfld     "nint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B8"
  IL_001a:  newobj     "System.ReadOnlySpan<nint>..ctor(nint[])"
  IL_001f:  ret
}
""";
            verifier.VerifyIL("C.M", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_NativeInts_Max()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<nint> M()
        => new nint[] { System.Int32.MaxValue };

    public static System.ReadOnlySpan<nint> M2()
        => new nint[] { System.Int32.MaxValue };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var verifier = CompileAndVerify(compilation, expectedOutput: "1 2147483647", verify: Verification.FailsILVerify);
            var expectedIL = """
{
  // Code size       36 (0x24)
  .maxstack  4
  IL_0000:  ldsfld     "nint[] <PrivateImplementationDetails>.A2C70538651A7E9296B097E8C3DFC1B195A945802FFE45AA471868FBA6F1042E_B8"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001e
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "System.IntPtr"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4     0x7fffffff
  IL_0016:  conv.i
  IL_0017:  stelem.i
  IL_0018:  dup
  IL_0019:  stsfld     "nint[] <PrivateImplementationDetails>.A2C70538651A7E9296B097E8C3DFC1B195A945802FFE45AA471868FBA6F1042E_B8"
  IL_001e:  newobj     "System.ReadOnlySpan<nint>..ctor(nint[])"
  IL_0023:  ret
}
""";
            verifier.VerifyIL("C.M", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_NativeUnsignedInts()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<nuint> M()
        => new nuint[] { 1 };

    public static System.ReadOnlySpan<nuint> M2()
        => new nuint[] { 1 };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var ilVerifyMessage = """
                [M]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1f }
                [M2]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1f }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "1 1",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            var expectedIL = """
{
  // Code size       32 (0x20)
  .maxstack  4
  IL_0000:  ldsfld     "nuint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B16"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001a
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "System.UIntPtr"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.1
  IL_0012:  conv.i
  IL_0013:  stelem.i
  IL_0014:  dup
  IL_0015:  stsfld     "nuint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B16"
  IL_001a:  newobj     "System.ReadOnlySpan<nuint>..ctor(nuint[])"
  IL_001f:  ret
}
""";
            verifier.VerifyIL("C.M", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_NativeUnsignedInts_Max()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<nuint> M()
        => new nuint[] { System.UInt32.MaxValue };

   public static System.ReadOnlySpan<nuint> M2()
        => new nuint[] { System.UInt32.MaxValue };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var verifier = CompileAndVerify(compilation, expectedOutput: "1 4294967295", verify: Verification.Skipped);
            var expectedIL = """
{
  // Code size       32 (0x20)
  .maxstack  4
  IL_0000:  ldsfld     "nuint[] <PrivateImplementationDetails>.AD95131BC0B799C0B1AF477FB14FCF26A6A9F76079E48BF090ACB7E8367BFD0E_B16"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001a
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "System.UIntPtr"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.m1
  IL_0012:  conv.u
  IL_0013:  stelem.i
  IL_0014:  dup
  IL_0015:  stsfld     "nuint[] <PrivateImplementationDetails>.AD95131BC0B799C0B1AF477FB14FCF26A6A9F76079E48BF090ACB7E8367BFD0E_B16"
  IL_001a:  newobj     "System.ReadOnlySpan<nuint>..ctor(nuint[])"
  IL_001f:  ret
}
""";
            verifier.VerifyIL("C.M", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_SameValuesDifferentTypes()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<nuint> M() => new nuint[] { 1 };
    public static System.ReadOnlySpan<nint> M2() => new nint[] { 1 };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var ilVerifyMessage = """
                [M]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1f }
                [M2]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x1f }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "1 1",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            verifier.VerifyIL("C.M", """
{
  // Code size       32 (0x20)
  .maxstack  4
  IL_0000:  ldsfld     "nuint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B16"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001a
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "System.UIntPtr"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.1
  IL_0012:  conv.i
  IL_0013:  stelem.i
  IL_0014:  dup
  IL_0015:  stsfld     "nuint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B16"
  IL_001a:  newobj     "System.ReadOnlySpan<nuint>..ctor(nuint[])"
  IL_001f:  ret
}
""");
            verifier.VerifyIL("C.M2", """
{
  // Code size       32 (0x20)
  .maxstack  4
  IL_0000:  ldsfld     "nint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B8"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001a
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "System.IntPtr"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.1
  IL_0012:  conv.i
  IL_0013:  stelem.i
  IL_0014:  dup
  IL_0015:  stsfld     "nint[] <PrivateImplementationDetails>.67ABDD721024F0FF4E0B3F4C2FC13BC5BAD42D0B7851D456D88D203D15AAA450_B8"
  IL_001a:  newobj     "System.ReadOnlySpan<nint>..ctor(nint[])"
  IL_001f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_Decimals()
        {
            var src = """
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<decimal> M()
        => new decimal[] { 1 };

    public static System.ReadOnlySpan<decimal> M2()
        => new decimal[] { 1 };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var ilVerifyMessage = """
                [M]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x26 }
                [M2]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x26 }
                """;

            var verifier = CompileAndVerify(compilation, expectedOutput: "1 1",
                verify: Verification.FailsILVerify with { ILVerifyMessage = ilVerifyMessage });

            var expectedIL = """
{
  // Code size       39 (0x27)
  .maxstack  4
  IL_0000:  ldsfld     "decimal[] <PrivateImplementationDetails>.4CBBD8CA5215B8D161AEC181A74B694F4E24B001D5B081DC0030ED797A8973E0_B18"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0021
  IL_0008:  pop
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "decimal"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldsfld     "decimal decimal.One"
  IL_0016:  stelem     "decimal"
  IL_001b:  dup
  IL_001c:  stsfld     "decimal[] <PrivateImplementationDetails>.4CBBD8CA5215B8D161AEC181A74B694F4E24B001D5B081DC0030ED797A8973E0_B18"
  IL_0021:  newobj     "System.ReadOnlySpan<decimal>..ctor(decimal[])"
  IL_0026:  ret
}
""";
            verifier.VerifyIL("C.M", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_DateTime_NotConstant()
        {
            var src = """
using System;
using System.Runtime.CompilerServices;

public class C
{
    [DateTimeConstant(-1)] static DateTime dateTime = default;

    public static System.ReadOnlySpan<DateTime> M()
        => new DateTime[] { dateTime };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            var expectedIL = """
{
  // Code size       24 (0x18)
  .maxstack  4
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "System.DateTime"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldsfld     "System.DateTime C.dateTime"
  IL_000d:  stelem     "System.DateTime"
  IL_0012:  call       "System.ReadOnlySpan<System.DateTime> System.ReadOnlySpan<System.DateTime>.op_Implicit(System.DateTime[])"
  IL_0017:  ret
}
""";
            verifier.VerifyIL("C.M", expectedIL);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_DateTime_WithConst()
        {
            var src = """
using System;
using System.Runtime.CompilerServices;

public class C
{
    [DateTimeConstant(-1)] const DateTime dateTime = default;
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            compilation.VerifyDiagnostics(
                // (6,28): error CS0283: The type 'DateTime' cannot be declared const
                //     [DateTimeConstant(-1)] const DateTime dateTime = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("System.DateTime").WithLocation(6, 28),
                // (6,54): error CS0133: The expression being assigned to 'C.dateTime' must be constant
                //     [DateTimeConstant(-1)] const DateTime dateTime = default;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default").WithArguments("C.dateTime").WithLocation(6, 54)
                );
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        [InlineData("sbyte")]
        [InlineData("byte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("float")]
        [InlineData("double")]
        public void ReadOnlySpanFromArrayOfConstants_IntegerTypesUsingBlob(string type)
        {
            var src = $$"""
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<{{type}}> M()
        => new {{type}}[] { 42 };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            CompileAndVerify(compilation, expectedOutput: "1 42", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_Char()
        {
            var src = $$"""
var values = C.M();
System.Console.Write($"{values.Length} {values[0]}");

public class C
{
    public static System.ReadOnlySpan<char> M()
        => new char[] { '!' };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            CompileAndVerify(compilation, expectedOutput: "1 !", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_Bool()
        {
            var src = $$"""
var values = C.M();
System.Console.Write($"{values.Length} {values[0]} {values[1]}");

public class C
{
    public static System.ReadOnlySpan<bool> M()
        => new bool[] { true, false };
}
""";
            var compilation = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(compilation, expectedOutput: "2 True False", verify: Verification.Skipped).VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    "short <PrivateImplementationDetails>.47DC540C94CEB704A23875C11273E16BB0B8A87AED84DE911F2133568115F254"
  IL_0005:  ldc.i4.2
  IL_0006:  newobj     "System.ReadOnlySpan<bool>..ctor(void*, int)"
  IL_000b:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_WithoutConst()
        {
            var src = """
public struct S { public int i; }
public class C
{
    public static System.ReadOnlySpan<S> M()
        => new S[] { default };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("C.M", """
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "S"
  IL_0006:  call       "System.ReadOnlySpan<S> System.ReadOnlySpan<S>.op_Implicit(S[])"
  IL_000b:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69472")]
        public void ReadOnlySpanFromArrayOfConstants_NullableValueType()
        {
            var src = """
public class C
{
    public static System.ReadOnlySpan<int?> M() => new int?[] { null };
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(src);
            var verifier = CompileAndVerify(compilation, verify: Verification.Skipped);
            verifier.VerifyIL("C.M", """
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "int?"
  IL_0006:  call       "System.ReadOnlySpan<int?> System.ReadOnlySpan<int?>.op_Implicit(int?[])"
  IL_000b:  ret
}
""");
        }

        [Theory]
        [InlineData("string")]
        [InlineData("object")]
        [InlineData("decimal")]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("System.DateTime")]
        [InlineData("Test")]
        public void EmptyArrayCtor_OtherTypes(string type)
        {
            var comp = CreateCompilationWithMscorlibAndSpan($$"""
using System;

class Test
{
    public static void Main()
    {       
        // inplace inits
        var s1 = new ReadOnlySpan<{{type}}>(new {{type}}[] { });
        var s2 = new ReadOnlySpan<{{type}}>(new {{type}}[] { });

        Console.Write(s1.Length == s2.Length);

        // make an instance
        Console.Write(s1.Length == new ReadOnlySpan<{{type}}>(new {{type}}[] { }).Length);
    }
}
""", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "TrueTrue").VerifyIL("Test.Main", $$"""
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (System.ReadOnlySpan<{{type}}> V_0, //s1
                System.ReadOnlySpan<{{type}}> V_1, //s2
                System.ReadOnlySpan<{{type}}> V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.ReadOnlySpan<{{type}}>"
  IL_0008:  ldloca.s   V_1
  IL_000a:  initobj    "System.ReadOnlySpan<{{type}}>"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "int System.ReadOnlySpan<{{type}}>.Length.get"
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "int System.ReadOnlySpan<{{type}}>.Length.get"
  IL_001e:  ceq
  IL_0020:  call       "void System.Console.Write(bool)"
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       "int System.ReadOnlySpan<{{type}}>.Length.get"
  IL_002c:  ldloca.s   V_2
  IL_002e:  initobj    "System.ReadOnlySpan<{{type}}>"
  IL_0034:  ldloc.2
  IL_0035:  stloc.2
  IL_0036:  ldloca.s   V_2
  IL_0038:  call       "int System.ReadOnlySpan<{{type}}>.Length.get"
  IL_003d:  ceq
  IL_003f:  call       "void System.Console.Write(bool)"
  IL_0044:  ret
}
""");
        }

        [Theory]
        [InlineData("string")]
        [InlineData("object")]
        [InlineData("decimal")]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("System.DateTime")]
        public void UnusedSpan_NothingEmitted(string type)
        {
            string csharp = $$"""
public class Test
{
    public static int M()
    {
        System.ReadOnlySpan<{{type}}> s = new {{type}}[] { default };
        return 42;
    }
}
""";
            var compilation = CreateCompilationWithMscorlibAndSpan(csharp, TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(compilation, verify: Verification.Passes);
            verifier.VerifyIL("Test.M", """
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  ret
}
""");
        }
    }
}
