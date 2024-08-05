// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class ForeachTest : EmitMetadataTestBase
    {
        // The loop object must be an array or an object collection
        [Fact]
        public void SimpleLoop()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        string[] arr = new string[4]; // Initialize
        arr[0] = ""one"";               // Element 1
        arr[1] = ""two"";               // Element 2
        arr[2] = ""three"";             // Element 3
        arr[3] = ""four"";              // Element 4
        foreach (string s in arr)
        {
            System.Console.WriteLine(s);
        }
    }
}
";
            string expectedOutput = @"one
two
three
four";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestIteration()
        {
            CompileAndVerify(@"
using System;
public class Test
{
    public static void Main(string[] args)
    {
        unsafe
        {
            int* y = null;
            foreach (var x in new int*[] { y }) { }
        }
    }
}", options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  4
  .locals init (int* V_0, //y
  int*[] V_1,
  int V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  newarr     ""int*""
  IL_0009:  dup
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  stelem.i
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.2
  IL_0010:  br.s       IL_001a
  IL_0012:  ldloc.1
  IL_0013:  ldloc.2
  IL_0014:  ldelem.i
  IL_0015:  pop
  IL_0016:  ldloc.2
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  stloc.2
  IL_001a:  ldloc.2
  IL_001b:  ldloc.1
  IL_001c:  ldlen
  IL_001d:  conv.i4
  IL_001e:  blt.s      IL_0012
  IL_0020:  ret
}");
        }

        // Using the Linq as iteration variable
        [Fact]
        public void TestLinqInForeach()
        {
            var text =
@"using System;
using System.Linq;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int x in from char c in ""abc"" select c)
        {
            Console.WriteLine(x);
        }
    }
}";
            string expectedOutput = @"97
98
99";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Empty foreach statement
        [Fact]
        public void TestEmptyStatementForeach()
        {
            var text =
@"class C
{
    static void Main()
    {
        foreach (char C in ""abc"");
    }
}";
            string expectedIL = @"{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (string V_0,
  int V_1)
  IL_0000:  ldstr      ""abc""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_0016
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  callvirt   ""char string.this[int].get""
  IL_0011:  pop
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4.1
  IL_0014:  add
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  ldloc.0
  IL_0018:  callvirt   ""int string.Length.get""
  IL_001d:  blt.s      IL_000a
  IL_001f:  ret
}";
            CompileAndVerify(text).VerifyIL("C.Main", expectedIL);
        }

        // Foreach value can't be deleted in a loop
        [Fact]
        public void TestRemoveValueInForeach()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;

class C
{
    static public void Main()
    {
        List<int> arrInt = new List<int>();
        arrInt.Add(1);
        foreach (int i in arrInt)
        {
            arrInt.Remove(i);//It will generate error in run-time
        }
    }
}
";
            string expectedIL = @"{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (System.Collections.Generic.List<int> V_0, //arrInt
           System.Collections.Generic.List<int>.Enumerator V_1,
           int V_2) //i
  IL_0000:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0005:  stloc.0   
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4.1  
  IL_0008:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_000d:  ldloc.0   
  IL_000e:  callvirt   ""System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()""
  IL_0013:  stloc.1   
  .try
  {
    IL_0014:  br.s       IL_0026
    IL_0016:  ldloca.s   V_1
    IL_0018:  call       ""int System.Collections.Generic.List<int>.Enumerator.Current.get""
    IL_001d:  stloc.2   
    IL_001e:  ldloc.0   
    IL_001f:  ldloc.2   
    IL_0020:  callvirt   ""bool System.Collections.Generic.List<int>.Remove(int)""
    IL_0025:  pop       
    IL_0026:  ldloca.s   V_1
    IL_0028:  call       ""bool System.Collections.Generic.List<int>.Enumerator.MoveNext()""
    IL_002d:  brtrue.s   IL_0016
    IL_002f:  leave.s    IL_003f
  }
  finally
  {
    IL_0031:  ldloca.s   V_1
    IL_0033:  constrained. ""System.Collections.Generic.List<int>.Enumerator""
    IL_0039:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003e:  endfinally
  }
  IL_003f:  ret       
}";
            CompileAndVerify(text).VerifyIL("C.Main", expectedIL);
        }

        // With multidimensional arrays, you can use one loop to iterate through the elements
        [Fact]
        public void TestMultiDimensionalArray()
        {
            var text =
@"class T
{
    static public void Main()
    {
        int[,] numbers2D = new int[3, 2] { { 9, 99 }, { 3, 33 }, { 5, 55 } };
        foreach (int i in numbers2D)
        {
            System.Console.WriteLine(i);
        }
    }
}
";
            string expectedOutput = @"9
99
3
33
5
55";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [WorkItem(540917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540917")]
        [Fact]
        public void TestArray()
        {
            var text =
@"using System;
public class Test
{
    static void Main(string[] args)
    {
        string[] arr = new string[4]; // Initialize
        arr[0] = ""one"";               // Element 1
        arr[1] = ""two"";               // Element 2
        foreach (string s in arr)
        {
            System.Console.WriteLine(s);
        }
    }
}
";
            string expectedIL = @"
{
  // Code size       46 (0x2e)
  .maxstack  4
  .locals init (string[] V_0,
  int V_1)
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""one""
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      ""two""
  IL_0015:  stelem.ref
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_0027
  IL_001b:  ldloc.0
  IL_001c:  ldloc.1
  IL_001d:  ldelem.ref
  IL_001e:  call       ""void System.Console.WriteLine(string)""
  IL_0023:  ldloc.1
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.1
  IL_0027:  ldloc.1
  IL_0028:  ldloc.0
  IL_0029:  ldlen
  IL_002a:  conv.i4
  IL_002b:  blt.s      IL_001b
  IL_002d:  ret
}";
            CompileAndVerify(text).VerifyIL("Test.Main", expectedIL);
        }

        [Fact]
        public void TestSpan()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = new Span<int>(new[] {1, 2, 3});
        foreach(var i in sp)
        {
            Console.Write(i);
        }
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "123").VerifyIL("Test.Main", @"
{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (System.Span<int> V_0,
                int V_1)
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_002d
  IL_001b:  ldloca.s   V_0
  IL_001d:  ldloc.1
  IL_001e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0023:  ldind.i4
  IL_0024:  call       ""void System.Console.Write(int)""
  IL_0029:  ldloc.1
  IL_002a:  ldc.i4.1
  IL_002b:  add
  IL_002c:  stloc.1
  IL_002d:  ldloc.1
  IL_002e:  ldloca.s   V_0
  IL_0030:  call       ""int System.Span<int>.Length.get""
  IL_0035:  blt.s      IL_001b
  IL_0037:  ret
}");
        }

        [Fact]
        public void TestSpanSideeffectingLoopBody()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = new Span<int>(new[] {1, 2, 3});
        foreach(var i in sp)
        {
            Console.Write(i);
            sp = default;
        }

        Console.Write(sp.Length);
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "1230").VerifyIL("Test.Main", @"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (System.Span<int> V_0, //sp
                System.Span<int> V_1,
                int V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  newarr     ""int""
  IL_0008:  dup
  IL_0009:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000e:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0013:  call       ""System.Span<int>..ctor(int[])""
  IL_0018:  ldloc.0
  IL_0019:  stloc.1
  IL_001a:  ldc.i4.0
  IL_001b:  stloc.2
  IL_001c:  br.s       IL_0038
  IL_001e:  ldloca.s   V_1
  IL_0020:  ldloc.2
  IL_0021:  call       ""ref int System.Span<int>.this[int].get""
  IL_0026:  ldind.i4
  IL_0027:  call       ""void System.Console.Write(int)""
  IL_002c:  ldloca.s   V_0
  IL_002e:  initobj    ""System.Span<int>""
  IL_0034:  ldloc.2
  IL_0035:  ldc.i4.1
  IL_0036:  add
  IL_0037:  stloc.2
  IL_0038:  ldloc.2
  IL_0039:  ldloca.s   V_1
  IL_003b:  call       ""int System.Span<int>.Length.get""
  IL_0040:  blt.s      IL_001e
  IL_0042:  ldloca.s   V_0
  IL_0044:  call       ""int System.Span<int>.Length.get""
  IL_0049:  call       ""void System.Console.Write(int)""
  IL_004e:  ret
}");
        }

        [Fact]
        public void TestReadOnlySpan()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = new ReadOnlySpan<Color>(new [] {Color.Red, Color.Green, Color.Blue});
        foreach(var i in sp)
        {
            Console.Write(i);
        }
    }
}

", TestOptions.ReleaseExe);

            //NOTE: the verification error is expected. Wrapping of literals into readonly spans uses unsafe Span.ctor.
            CompileAndVerify(comp, expectedOutput: "RedGreenBlue", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (System.ReadOnlySpan<System.Color> V_0,
                int V_1)
  IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.AE4B3280E56E2FAF83F414A6E3DABE9D5FBE18976544C05FED121ACCB85B53FC""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<System.Color>..ctor(void*, int)""
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  IL_000e:  br.s       IL_0027
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldloc.1
  IL_0013:  call       ""ref readonly System.Color System.ReadOnlySpan<System.Color>.this[int].get""
  IL_0018:  ldind.i1
  IL_0019:  box        ""System.Color""
  IL_001e:  call       ""void System.Console.Write(object)""
  IL_0023:  ldloc.1
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.1
  IL_0027:  ldloc.1
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""int System.ReadOnlySpan<System.Color>.Length.get""
  IL_002f:  blt.s      IL_0010
  IL_0031:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestReadOnlySpanString()
        {
            var comp = CreateCompilation(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = (ReadOnlySpan<char>)""hello"";
        foreach(var i in sp)
        {
            Console.Write(i);
        }
    }
}

", targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "hello", verify: Verification.Passes).VerifyIL("Test.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (System.ReadOnlySpan<char> V_0,
                int V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_0021
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldloc.1
  IL_0012:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0017:  ldind.u2
  IL_0018:  call       ""void System.Console.Write(char)""
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4.1
  IL_001f:  add
  IL_0020:  stloc.1
  IL_0021:  ldloc.1
  IL_0022:  ldloca.s   V_0
  IL_0024:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0029:  blt.s      IL_000f
  IL_002b:  ret
}");
        }

        [Fact]
        public void TestReadOnlySpan2()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        foreach(var i in (ReadOnlySpan<byte>)new byte[] {1, 2, 3})
        {
            Console.Write(i);
        }
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (System.ReadOnlySpan<byte> V_0,
                int V_1)
  IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  IL_000e:  br.s       IL_0022
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldloc.1
  IL_0013:  call       ""ref readonly byte System.ReadOnlySpan<byte>.this[int].get""
  IL_0018:  ldind.u1
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ldloc.1
  IL_001f:  ldc.i4.1
  IL_0020:  add
  IL_0021:  stloc.1
  IL_0022:  ldloc.1
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       ""int System.ReadOnlySpan<byte>.Length.get""
  IL_002a:  blt.s      IL_0010
  IL_002c:  ret
}");
        }

        [Fact]
        public void TestSpanNoIndexer()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = new Span<int>(new[] {1, 2, 3});
        foreach(var i in sp)
        {
            Console.Write(i);
        }
    }
}
", TestOptions.ReleaseExe);

            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);

            CompileAndVerify(comp, expectedOutput: "123").VerifyIL("Test.Main", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (System.Span<int> V_0, //sp
                System.Span<int>.Enumerator V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  newarr     ""int""
  IL_0008:  dup
  IL_0009:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000e:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0013:  call       ""System.Span<int>..ctor(int[])""
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""System.Span<int>.Enumerator System.Span<int>.GetEnumerator()""
  IL_001f:  stloc.1
  IL_0020:  br.s       IL_002f
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""ref int System.Span<int>.Enumerator.Current.get""
  IL_0029:  ldind.i4
  IL_002a:  call       ""void System.Console.Write(int)""
  IL_002f:  ldloca.s   V_1
  IL_0031:  call       ""bool System.Span<int>.Enumerator.MoveNext()""
  IL_0036:  brtrue.s   IL_0022
  IL_0038:  ret
}");
        }

        [Fact]
        public void TestSpanValIndexer()
        {
            var comp = CreateEmptyCompilation(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = new ReadOnlySpan<int>(new[] {1, 2, 3});
        foreach(var i in sp)
        {
            Console.Write(i);
        }
    }
}


namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        private readonly T[] arr;

        public T this[int i] => arr[i];
        public int Length { get; }

        public ReadOnlySpan(T[] arr)
        {
            this.arr = arr;
            this.Length = arr.Length;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<T> _span;
            private int _index;

            internal Enumerator(ReadOnlySpan<T> span)
            {
                _span = span;
                _index = -1;
            }

            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            public T Current
            {
                get => _span[_index];
            }
        }
    }
}

", references: new[] { MscorlibRef_v4_0_30316_17626 }, TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       56 (0x38)
  .maxstack  4
  .locals init (System.ReadOnlySpan<int> V_0, //sp
                System.ReadOnlySpan<int>.Enumerator V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  newarr     ""int""
  IL_0008:  dup
  IL_0009:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000e:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0013:  call       ""System.ReadOnlySpan<int>..ctor(int[])""
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""System.ReadOnlySpan<int>.Enumerator System.ReadOnlySpan<int>.GetEnumerator()""
  IL_001f:  stloc.1
  IL_0020:  br.s       IL_002e
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""int System.ReadOnlySpan<int>.Enumerator.Current.get""
  IL_0029:  call       ""void System.Console.Write(int)""
  IL_002e:  ldloca.s   V_1
  IL_0030:  call       ""bool System.ReadOnlySpan<int>.Enumerator.MoveNext()""
  IL_0035:  brtrue.s   IL_0022
  IL_0037:  ret
}");
        }

        [Fact]
        public void TestSpanConvert()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = new Span<int>(new[] {1, 2, 3});
        foreach(byte i in sp)
        {
            Console.Write(i);
        }
    }
}

", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "123").VerifyIL("Test.Main", @"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (System.Span<int> V_0,
                int V_1)
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_002e
  IL_001b:  ldloca.s   V_0
  IL_001d:  ldloc.1
  IL_001e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0023:  ldind.i4
  IL_0024:  conv.u1
  IL_0025:  call       ""void System.Console.Write(int)""
  IL_002a:  ldloc.1
  IL_002b:  ldc.i4.1
  IL_002c:  add
  IL_002d:  stloc.1
  IL_002e:  ldloc.1
  IL_002f:  ldloca.s   V_0
  IL_0031:  call       ""int System.Span<int>.Length.get""
  IL_0036:  blt.s      IL_001b
  IL_0038:  ret
}");
        }

        [Fact]
        public void TestSpanDeconstruct()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
        static void Main(string[] args)
        {
            var sp = new Span<(int, int)>(new[] {(1, 2), (3, 4)});
            foreach(var (i, j) in sp)
            {
                Console.Write(i);
                Console.Write(j);
            }
        }
}

", TestOptions.ReleaseExe);

            comp = comp.WithReferences(comp.References.Concat(new[] { SystemRuntimeFacadeRef, ValueTupleRef }));

            CompileAndVerify(comp, expectedOutput: "1234").VerifyIL("Test.Main", @"
{
  // Code size       95 (0x5f)
  .maxstack  5
  .locals init (System.Span<System.ValueTuple<int, int>> V_0,
                int V_1,
                int V_2) //i
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""System.ValueTuple<int, int>""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  ldc.i4.2
  IL_000a:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000f:  stelem     ""System.ValueTuple<int, int>""
  IL_0014:  dup
  IL_0015:  ldc.i4.1
  IL_0016:  ldc.i4.3
  IL_0017:  ldc.i4.4
  IL_0018:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_001d:  stelem     ""System.ValueTuple<int, int>""
  IL_0022:  newobj     ""System.Span<System.ValueTuple<int, int>>..ctor(System.ValueTuple<int, int>[])""
  IL_0027:  stloc.0
  IL_0028:  ldc.i4.0
  IL_0029:  stloc.1
  IL_002a:  br.s       IL_0054
  IL_002c:  ldloca.s   V_0
  IL_002e:  ldloc.1
  IL_002f:  call       ""ref System.ValueTuple<int, int> System.Span<System.ValueTuple<int, int>>.this[int].get""
  IL_0034:  ldobj      ""System.ValueTuple<int, int>""
  IL_0039:  dup
  IL_003a:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_003f:  stloc.2
  IL_0040:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0045:  ldloc.2
  IL_0046:  call       ""void System.Console.Write(int)""
  IL_004b:  call       ""void System.Console.Write(int)""
  IL_0050:  ldloc.1
  IL_0051:  ldc.i4.1
  IL_0052:  add
  IL_0053:  stloc.1
  IL_0054:  ldloc.1
  IL_0055:  ldloca.s   V_0
  IL_0057:  call       ""int System.Span<System.ValueTuple<int, int>>.Length.get""
  IL_005c:  blt.s      IL_002c
  IL_005e:  ret
}");
        }

        [Fact]
        public void TestSpanConvertDebug()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class Test
{
    public static void Main()
    {       
        var sp = new Span<int>(new[] {1, 2, 3});
        foreach(byte i in sp)
        {
            Console.Write(i);
        }
    }
}

", TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "123").VerifyIL("Test.Main", @"
{
  // Code size       67 (0x43)
  .maxstack  4
  .locals init (System.Span<int> V_0, //sp
                System.Span<int> V_1,
                int V_2,
                byte V_3) //i
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.3
  IL_0004:  newarr     ""int""
  IL_0009:  dup
  IL_000a:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000f:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0014:  call       ""System.Span<int>..ctor(int[])""
  IL_0019:  nop
  IL_001a:  ldloc.0
  IL_001b:  stloc.1
  IL_001c:  ldc.i4.0
  IL_001d:  stloc.2
  IL_001e:  br.s       IL_0038
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldloc.2
  IL_0023:  call       ""ref int System.Span<int>.this[int].get""
  IL_0028:  ldind.i4
  IL_0029:  conv.u1
  IL_002a:  stloc.3
  IL_002b:  nop
  IL_002c:  ldloc.3
  IL_002d:  call       ""void System.Console.Write(int)""
  IL_0032:  nop
  IL_0033:  nop
  IL_0034:  ldloc.2
  IL_0035:  ldc.i4.1
  IL_0036:  add
  IL_0037:  stloc.2
  IL_0038:  ldloc.2
  IL_0039:  ldloca.s   V_1
  IL_003b:  call       ""int System.Span<int>.Length.get""
  IL_0040:  blt.s      IL_0020
  IL_0042:  ret
}");
        }

        // Traveled Multi-dimensional jagged arrays 
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestJaggedArray()
        {
            var text =
@"using System;
public class Test
{
    static void Main(string[] args)
    {
        int[][] arr = new int[][] { new int[] { 1, 2 }, new int[] { 4, 5, 6 } };
        foreach (int[] outer in arr)
        {
            foreach (int i in outer)
            {
                Console.WriteLine(i);
            }
        }
    }
}
";
            string expectedOutput = @"1
2
4
5
6";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Optimization to foreach (char c in String) by treating String as a char array 
        [Fact]
        public void TestString01()
        {
            var text =
@"using System;
public class Test
{
    static void Main(string[] args)
    {
        System.String Str = new System.String('\0', 1024);
        foreach (char C in Str) { }
    }
}
";
            string expectedOutput = @"";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestString02()
        {
            var text =
@"using System;
public class Test
{
    static public int Main(string[] args)
    {
        foreach (var var in ""goo"")
        {
            if (!var.GetType().Equals(typeof(char)))
            {
                System.Console.WriteLine(-1);
                return -1;
            }
        }
        return 0;
    }
}
";
            string expectedOutput = @"";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestString03()
        {
            var text =
@"using System;
public class Test
{
    static public void Main(string[] args)
    {
        String Str = null;
        foreach (char C in Str) { }
    }
}
";
            string expectedIL = @"{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0,
  int V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  br.s       IL_0012
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  callvirt   ""char string.this[int].get""
  IL_000d:  pop
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.1
  IL_0010:  add
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""int string.Length.get""
  IL_0019:  blt.s      IL_0006
  IL_001b:  ret
}";
            CompileAndVerify(text).VerifyIL("Test.Main", expectedIL);
        }

        // Traversing items in 'Dictionary'
        [Fact]
        public void TestDictionary()
        {
            var text =
@"using System;
using System.Collections.Generic;
public class Test
{
    static public void Main(string[] args)
    {
        Dictionary<int, int> s = new Dictionary<int, int>();
        s.Add(1, 2);
        s.Add(2, 3);
        s.Add(3, 4);
        foreach (var pair in s) { Console.WriteLine( pair.Key );}
        foreach (KeyValuePair<int, int> pair in s) {Console.WriteLine( pair.Value ); }
    }
}
";
            string expectedOutput = @"1
2
3
2
3
4";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Inner foreach loop referencing the outer foreach loop iteration variable
        [Fact]
        public void TestNestedLoop()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                System.Console.WriteLine(y);
            }
        }
    }
}
";
            string expectedOutput = @"A
B
C
X
Y
Z";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Breaking from nested Loops
        [Fact]
        public void TestBreakInNestedLoop()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                if (y == 'A')
                    break;
                else 
                    System.Console.WriteLine(y);
            }
            System.Console.WriteLine(x);
        }
    }
}
";
            string expectedOutput = @"ABC
X
Y
Z
XYZ";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Continuing  from nested Loops
        [Fact]
        public void TestContinueInNestedLoop()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                if (y == 'C')
                    continue;
                System.Console.WriteLine(y);
            }
        }
    }
}
";
            string expectedOutput = @"A
B
X
Y
Z";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Goto in foreach loops
        [Fact]
        public void TestGoto01()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                System.Console.WriteLine(y);
                goto stop;
            }
        }
    stop:
        return;
    }
}
";
            string expectedOutput = @"A";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestGoto02()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                System.Console.WriteLine(y);
                goto outerLoop;
            }
        outerLoop:
            return;
        }
    }
}
";
            string expectedOutput = @"A";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // 'Return' in foreach
        [Fact]
        public void TestReturn()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            return;
        }
    }
}
";
            string expectedIL = @"{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (string[] V_0,
  int V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""ABC""
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      ""XYZ""
  IL_0015:  stelem.ref
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_0020
  IL_001b:  ldloc.0
  IL_001c:  ldloc.1
  IL_001d:  ldelem.ref
  IL_001e:  pop
  IL_001f:  ret
  IL_0020:  ldloc.1
  IL_0021:  ldloc.0
  IL_0022:  ldlen
  IL_0023:  conv.i4
  IL_0024:  blt.s      IL_001b
  IL_0026:  ret
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", expectedIL);
        }

        // Dynamic works in foreach 
        [Fact]
        public void TestDynamic()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (dynamic x in S)
        {
            System.Console.WriteLine(x.ToLower());
        }
    }
}
";
            string expectedOutput = @"abc
xyz";
            CompileAndVerify(text, new[] { CSharpRef }, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVar01()
        {
            var text =
@"using System.Collections.Generic;
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var var in new List<double> { 1.0, 2.0, 3.0 })
        {
            if (var.GetType().Equals(typeof(double)))
            {
                System.Console.WriteLine(true);
            }
        }
    }
}
";
            string expectedOutput = @"True
True
True";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVar02()
        {
            var text =
@"
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var var in new string[] { ""one"", ""two"", ""three"" })
        {
            if (!var.GetType().Equals(typeof(double)))
            {
                System.Console.WriteLine(false);
            }
        }
    }
}
";
            string expectedOutput = @"False
False
False";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVar03()
        {
            var text =
@"
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var var in new MyClass())
        {
            if (var.GetType().Equals(typeof(int)))
            {
                System.Console.WriteLine(true);
            }
        }
    }
}
class MyClass
{
    public MyEnumerator GetEnumerator()
    {
        return new MyEnumerator();
    }
}
class MyEnumerator
{
    int count = 4;
    public int Current
    {
        get
        {
            return count;
        }
    }
    public bool MoveNext()
    {
        count--;
        return count != 0;
    }
}
";
            string expectedOutput = @"True
True
True";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestQuery()
        {
            var text =
@"
using System.Linq;
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var x in from x in new[] { 'A', 'B', 'C' }
                          let z = x.ToString()
                          select z into w
                          select w)
        {
            System.Console.WriteLine(x.ToLower());
        }
    }
}
";
            string expectedOutput = @"a
b
c
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield01()
        {
            var text =
@"
using System.Collections;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int i in myClass.Power(2, 8))
        {
            System.Console.WriteLine(""{0}"", i);
        }
    }
}

public class myClass
{
    public static IEnumerable Power(int number, int exponent)
    {
        int counter = 0;
        int result = 1;
        while (counter++ < exponent)
        {
            result = result * number;
            yield return result;
        }
    }
}
";
            string expectedOutput = @"2
4
8
16
32
64
128
256
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield02()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int i in FromTo(2,4))
        {
            System.Console.WriteLine(""{0}"", i);
        }
    }
    public static IEnumerable<int> FromTo(int from, int to)
    {
        for (int i = from; i <= to; i++) yield return i;
    }
}
";
            string expectedOutput = @"2
3
4
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield03()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (var i in EnumerateIt<string>(new List<string>() { ""abc"" }))
        {
            System.Console.WriteLine(i);
        }
    }
    public static IEnumerable<T> EnumerateIt<T>(IEnumerable<T> xs)
    {
        foreach (T x in xs) yield return x;
    }
}
";
            string expectedOutput = @"abc";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield04()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int p in EnumerateIt(FromTo(3, 5)))
        {
            System.Console.WriteLine(""{0}"", p);
        }
    }
    public static IEnumerable<int> FromTo(int from, int to)
    {
        for (int i = from; i <= to; i++) yield return i;
    }

    public static IEnumerable<T> EnumerateIt<T>(IEnumerable<T> xs)
    {
        foreach (T x in xs) yield return x;
    }
}
";
            string expectedOutput = @"3
4
5";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield05()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (var j in new Gen<double>()) { System.Console.WriteLine(j); }
    }
}
public class Gen<T> where T : new()
{
    public IEnumerator<T> GetEnumerator()
    {
        yield return new T();
        yield return new T();
        yield return new T();
    }
}
";
            string expectedOutput = @"0
0
0";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestValueTypeIterationVariableCanBeMutatedByInstanceMethods()
        {
            const string source = @"
struct A
{
    int field;

    void Set(A a)
    {
        this = a;
    }

    static void Main()
    {
        foreach (var a in new A[1])
        {
            a.Set(new A { field = 5 });
            System.Console.Write(a.field);
        }
    }  
}";

            CompileAndVerify(source, expectedOutput: "5");
        }

        [Fact, WorkItem(1077204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077204")]
        public void TestValueTypeIterationVariableFieldsAreReadonly()
        {
            const string source = @"
using System;

struct A
{
    public B B;

    static void Main()
    {
        A[] array = { default(A) };

        foreach (A a in array)
        {
            a.B.SetField(5);
            Console.Write(a.B.Field);
        }
    }
}

struct B
{
    public int Field;

    public void SetField(int value)
    {
        this.Field = value;
    }
}";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [Fact]
        public void TestValueTypeIterationVariableFieldsAreReadonly2()
        {
            const string source = @"
struct C
{
    public int field;

    public void SetField(int value)
    {
        field = value;
    }
}

struct B
{
    public C c;
}

struct A
{
    B b;

    static void Main()
    {
        foreach (var a in new A[1])
        {
            a.b.c.SetField(5);
            System.Console.Write(a.b.c.field);
        }
    }
}";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [Fact]
        public void Var_ExtensionGetEnumerator()
        {
            var source = """
                using System.Collections.Generic;
                class MyCollection<T>
                {
                    public readonly List<T> Items;
                    public MyCollection(params T[] items) { Items = new(items); }
                }
                static class Extensions
                {
                    public static IEnumerator<T> GetEnumerator<T>(this MyCollection<T> c) => c.Items.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = new(1, 2, 3);
                        int total = 0;
                        foreach (var y in x)
                            total += y;
                    }
                }
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularDefault.WithFeature("run-nullable-analysis", "never"));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decl = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var local = (SourceLocalSymbol)model.GetDeclaredSymbol(decl).GetSymbol<LocalSymbol>();
            Assert.True(local.IsVar);
            Assert.Equal("System.Int32", local.Type.ToTestDisplayString());

            comp.VerifyDiagnostics();
        }
    }
}
