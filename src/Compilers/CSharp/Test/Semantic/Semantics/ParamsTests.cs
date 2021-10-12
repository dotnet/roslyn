// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ParamsTests : CSharpTestBase
    {
        private const string StackAllocDefinition =
@"namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static unsafe Span<T> StackAlloc<T>(int length)
        {
            return new Span<T>(new T[length]);
        }
    }
}";

        [Fact]
        public void ParamsSpan()
        {
            var source =
@"using System;
class Program
{
    static void F1(params Span<object> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    static void F2(params ReadOnlySpan<object> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    static void Main()
    {
        F1();
        F1(1, 2, ""hello"");
        F2();
        F2(""span"", 3);
    }
}";
            var verifier = CompileAndVerify(new[] { source, SpanSource, StackAllocDefinition }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
2
hello
span
3
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      122 (0x7a)
  .maxstack  2
  .locals init (System.Span<object> V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_0006:  call       ""void Program.F1(params System.Span<object>)""
  IL_000b:  ldc.i4.3
  IL_000c:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  ldc.i4.0
  IL_0015:  call       ""ref object System.Span<object>.this[int].get""
  IL_001a:  ldc.i4.1
  IL_001b:  box        ""int""
  IL_0020:  stind.ref
  IL_0021:  ldloca.s   V_0
  IL_0023:  ldc.i4.1
  IL_0024:  call       ""ref object System.Span<object>.this[int].get""
  IL_0029:  ldc.i4.2
  IL_002a:  box        ""int""
  IL_002f:  stind.ref
  IL_0030:  ldloca.s   V_0
  IL_0032:  ldc.i4.2
  IL_0033:  call       ""ref object System.Span<object>.this[int].get""
  IL_0038:  ldstr      ""hello""
  IL_003d:  stind.ref
  IL_003e:  ldloc.0
  IL_003f:  call       ""void Program.F1(params System.Span<object>)""
  IL_0044:  ldc.i4.0
  IL_0045:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_004a:  call       ""void Program.F2(params System.ReadOnlySpan<object>)""
  IL_004f:  ldc.i4.2
  IL_0050:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_0055:  stloc.0
  IL_0056:  ldloca.s   V_0
  IL_0058:  ldc.i4.0
  IL_0059:  call       ""ref object System.Span<object>.this[int].get""
  IL_005e:  ldstr      ""span""
  IL_0063:  stind.ref
  IL_0064:  ldloca.s   V_0
  IL_0066:  ldc.i4.1
  IL_0067:  call       ""ref object System.Span<object>.this[int].get""
  IL_006c:  ldc.i4.3
  IL_006d:  box        ""int""
  IL_0072:  stind.ref
  IL_0073:  ldloc.0
  IL_0074:  call       ""void Program.F2(params System.ReadOnlySpan<object>)""
  IL_0079:  ret
}");
        }

        /// <summary>
        /// Prefer params Span or ReadOnlySpan over params T[].
        /// </summary>
        [Fact]
        public void OverloadResolution()
        {
            var source =
@"using System;
class Program
{
    static void F1(params object[] args) { throw new Exception(); }
    static void F2(params object[] args) { throw new Exception(); }
    static void F1(params Span<object> args) { foreach (var arg in args) Console.WriteLine(arg); }
    static void F2(params ReadOnlySpan<object> args) { foreach (var arg in args) Console.WriteLine(arg); }
    static void Main()
    {
        F1(1, 2, 3);
        F2(""hello"", ""world"");
    }
}";
            CompileAndVerify(new[] { source, SpanSource, StackAllocDefinition }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
2
3
hello
world
");
        }

        /// <summary>
        /// Re-use buffers within a method.
        /// </summary>
        [Fact]
        public void RepeatedCalls_01()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static unsafe Span<T> StackAlloc<T>(int length)
        {
            Console.WriteLine(""StackAlloc<{0}>({1})"", typeof(T), length);
            return new Span<T>(new T[length]);
        }
    }
}";
            var source2 =
@"using System;
class Program
{
    static void F<T>(params Span<T> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    static void Main()
    {
        F(1, 2);
        int offset = 2;
        while (offset < 15)
        {
            F(offset + 1, offset + 2);
            F(offset + 3, offset + 4, offset + 5);
            offset += 5;
        }
        F(offset + 1, offset + 2, offset + 3);
    }
}";
            // PROTOTYPE: Should re-use buffers.
            var verifier = CompileAndVerify(new[] { source1, source2, SpanSource }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"StackAlloc<System.Int32>(2)
1
2
StackAlloc<System.Int32>(2)
3
4
StackAlloc<System.Int32>(3)
5
6
7
StackAlloc<System.Int32>(2)
8
9
StackAlloc<System.Int32>(3)
10
11
12
StackAlloc<System.Int32>(2)
13
14
StackAlloc<System.Int32>(3)
15
16
17
StackAlloc<System.Int32>(3)
18
19
20
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      182 (0xb6)
  .maxstack  3
  .locals init (int V_0, //offset
                System.Span<int> V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  ldc.i4.0
  IL_000a:  call       ""ref int System.Span<int>.this[int].get""
  IL_000f:  ldc.i4.1
  IL_0010:  stind.i4
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldc.i4.1
  IL_0014:  call       ""ref int System.Span<int>.this[int].get""
  IL_0019:  ldc.i4.2
  IL_001a:  stind.i4
  IL_001b:  ldloc.1
  IL_001c:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0021:  ldc.i4.2
  IL_0022:  stloc.0
  IL_0023:  br.s       IL_007f
  IL_0025:  ldc.i4.2
  IL_0026:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_002b:  stloc.1
  IL_002c:  ldloca.s   V_1
  IL_002e:  ldc.i4.0
  IL_002f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.1
  IL_0036:  add
  IL_0037:  stind.i4
  IL_0038:  ldloca.s   V_1
  IL_003a:  ldc.i4.1
  IL_003b:  call       ""ref int System.Span<int>.this[int].get""
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.2
  IL_0042:  add
  IL_0043:  stind.i4
  IL_0044:  ldloc.1
  IL_0045:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_004a:  ldc.i4.3
  IL_004b:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_0050:  stloc.1
  IL_0051:  ldloca.s   V_1
  IL_0053:  ldc.i4.0
  IL_0054:  call       ""ref int System.Span<int>.this[int].get""
  IL_0059:  ldloc.0
  IL_005a:  ldc.i4.3
  IL_005b:  add
  IL_005c:  stind.i4
  IL_005d:  ldloca.s   V_1
  IL_005f:  ldc.i4.1
  IL_0060:  call       ""ref int System.Span<int>.this[int].get""
  IL_0065:  ldloc.0
  IL_0066:  ldc.i4.4
  IL_0067:  add
  IL_0068:  stind.i4
  IL_0069:  ldloca.s   V_1
  IL_006b:  ldc.i4.2
  IL_006c:  call       ""ref int System.Span<int>.this[int].get""
  IL_0071:  ldloc.0
  IL_0072:  ldc.i4.5
  IL_0073:  add
  IL_0074:  stind.i4
  IL_0075:  ldloc.1
  IL_0076:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_007b:  ldloc.0
  IL_007c:  ldc.i4.5
  IL_007d:  add
  IL_007e:  stloc.0
  IL_007f:  ldloc.0
  IL_0080:  ldc.i4.s   15
  IL_0082:  blt.s      IL_0025
  IL_0084:  ldc.i4.3
  IL_0085:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_008a:  stloc.1
  IL_008b:  ldloca.s   V_1
  IL_008d:  ldc.i4.0
  IL_008e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0093:  ldloc.0
  IL_0094:  ldc.i4.1
  IL_0095:  add
  IL_0096:  stind.i4
  IL_0097:  ldloca.s   V_1
  IL_0099:  ldc.i4.1
  IL_009a:  call       ""ref int System.Span<int>.this[int].get""
  IL_009f:  ldloc.0
  IL_00a0:  ldc.i4.2
  IL_00a1:  add
  IL_00a2:  stind.i4
  IL_00a3:  ldloca.s   V_1
  IL_00a5:  ldc.i4.2
  IL_00a6:  call       ""ref int System.Span<int>.this[int].get""
  IL_00ab:  ldloc.0
  IL_00ac:  ldc.i4.3
  IL_00ad:  add
  IL_00ae:  stind.i4
  IL_00af:  ldloc.1
  IL_00b0:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_00b5:  ret
}");
        }

        [Fact]
        public void RepeatedCalls_02()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static unsafe Span<T> StackAlloc<T>(int length)
        {
            Console.WriteLine(""StackAlloc<{0}>({1})"", typeof(T), length);
            return new Span<T>(new T[length]);
        }
    }
}";
            var source2 =
@"using System;
class Program
{
    static T ElementAt<T>(int index, params Span<T> args) => args[index];
    static void Main()
    {
        var value = ElementAt(
            0,
            ElementAt(1, 'a', 'b', 'c'),
            ElementAt(2, 'e', 'f', 'g'));
        Console.WriteLine(value);
    }
}";
            // PROTOTYPE: Should re-use buffers.
            var verifier = CompileAndVerify(new[] { source1, source2, SpanSource }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"StackAlloc<System.Char>(2)
StackAlloc<System.Char>(3)
StackAlloc<System.Char>(3)
b
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      132 (0x84)
  .maxstack  5
  .locals init (System.Span<char> V_0,
                System.Span<char> V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""System.Span<char> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<char>(int)""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""ref char System.Span<char>.this[int].get""
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.3
  IL_0012:  call       ""System.Span<char> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<char>(int)""
  IL_0017:  stloc.1
  IL_0018:  ldloca.s   V_1
  IL_001a:  ldc.i4.0
  IL_001b:  call       ""ref char System.Span<char>.this[int].get""
  IL_0020:  ldc.i4.s   97
  IL_0022:  stind.i2
  IL_0023:  ldloca.s   V_1
  IL_0025:  ldc.i4.1
  IL_0026:  call       ""ref char System.Span<char>.this[int].get""
  IL_002b:  ldc.i4.s   98
  IL_002d:  stind.i2
  IL_002e:  ldloca.s   V_1
  IL_0030:  ldc.i4.2
  IL_0031:  call       ""ref char System.Span<char>.this[int].get""
  IL_0036:  ldc.i4.s   99
  IL_0038:  stind.i2
  IL_0039:  ldloc.1
  IL_003a:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_003f:  stind.i2
  IL_0040:  ldloca.s   V_0
  IL_0042:  ldc.i4.1
  IL_0043:  call       ""ref char System.Span<char>.this[int].get""
  IL_0048:  ldc.i4.2
  IL_0049:  ldc.i4.3
  IL_004a:  call       ""System.Span<char> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<char>(int)""
  IL_004f:  stloc.1
  IL_0050:  ldloca.s   V_1
  IL_0052:  ldc.i4.0
  IL_0053:  call       ""ref char System.Span<char>.this[int].get""
  IL_0058:  ldc.i4.s   101
  IL_005a:  stind.i2
  IL_005b:  ldloca.s   V_1
  IL_005d:  ldc.i4.1
  IL_005e:  call       ""ref char System.Span<char>.this[int].get""
  IL_0063:  ldc.i4.s   102
  IL_0065:  stind.i2
  IL_0066:  ldloca.s   V_1
  IL_0068:  ldc.i4.2
  IL_0069:  call       ""ref char System.Span<char>.this[int].get""
  IL_006e:  ldc.i4.s   103
  IL_0070:  stind.i2
  IL_0071:  ldloc.1
  IL_0072:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_0077:  stind.i2
  IL_0078:  ldloc.0
  IL_0079:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_007e:  call       ""void System.Console.WriteLine(char)""
  IL_0083:  ret
}");
        }
    }
}
