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

        private static MetadataReference GetSpanLibrary()
        {
            var comp = CreateCompilation(SpanSource, options: TestOptions.UnsafeReleaseDll);
            return comp.EmitToImageReference();
        }

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
  // Code size      124 (0x7c)
  .maxstack  2
  .locals init (System.Span<object> V_0,
                System.Span<object> V_1,
                System.Span<object> V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_0006:  ldc.i4.3
  IL_0007:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_0013:  stloc.1
  IL_0014:  ldc.i4.2
  IL_0015:  call       ""System.Span<object> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<object>(int)""
  IL_001a:  stloc.2
  IL_001b:  call       ""void Program.F1(params System.Span<object>)""
  IL_0020:  ldloca.s   V_0
  IL_0022:  ldc.i4.0
  IL_0023:  call       ""ref object System.Span<object>.this[int].get""
  IL_0028:  ldc.i4.1
  IL_0029:  box        ""int""
  IL_002e:  stind.ref
  IL_002f:  ldloca.s   V_0
  IL_0031:  ldc.i4.1
  IL_0032:  call       ""ref object System.Span<object>.this[int].get""
  IL_0037:  ldc.i4.2
  IL_0038:  box        ""int""
  IL_003d:  stind.ref
  IL_003e:  ldloca.s   V_0
  IL_0040:  ldc.i4.2
  IL_0041:  call       ""ref object System.Span<object>.this[int].get""
  IL_0046:  ldstr      ""hello""
  IL_004b:  stind.ref
  IL_004c:  ldloc.0
  IL_004d:  call       ""void Program.F1(params System.Span<object>)""
  IL_0052:  ldloc.1
  IL_0053:  call       ""void Program.F2(params System.ReadOnlySpan<object>)""
  IL_0058:  ldloca.s   V_2
  IL_005a:  ldc.i4.0
  IL_005b:  call       ""ref object System.Span<object>.this[int].get""
  IL_0060:  ldstr      ""span""
  IL_0065:  stind.ref
  IL_0066:  ldloca.s   V_2
  IL_0068:  ldc.i4.1
  IL_0069:  call       ""ref object System.Span<object>.this[int].get""
  IL_006e:  ldc.i4.3
  IL_006f:  box        ""int""
  IL_0074:  stind.ref
  IL_0075:  ldloc.2
  IL_0076:  call       ""void Program.F2(params System.ReadOnlySpan<object>)""
  IL_007b:  ret
}");
        }

        /// <summary>
        /// params value cannot be returned from the method since that
        /// would prevent sharing repeated allocations at the call-site.
        /// </summary>
        [Fact]
        public void CannotReturnSpan_01()
        {
            var source =
@"using System;
class Program
{
    static T[] F0<T>(params T[] x0)
    {
        return x0;
    }
    static Span<T> F1<T>(params Span<T> x1)
    {
        return x1;
    }
    static Span<T> F2<T>(Span<T> x2, params Span<T> y2)
    {
        return x2;
    }
}";
            var comp = CreateCompilation(source, references: new[] { GetSpanLibrary() });
            comp.VerifyDiagnostics(
                // (10,16): error CS8980: Cannot use params 'x1' in this context because it may prevent reuse at the call-site
                //         return x1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "x1").WithArguments("x1").WithLocation(10, 16));
        }

        [Fact]
        public void CannotReturnSpan_02()
        {
            var source =
@"using System;
class Program
{
    static void F0<T>(out T[] x0, params T[] y0)
    {
        x0 = y0;
    }
    static void F1<T>(out ReadOnlySpan<T> x1, params ReadOnlySpan<T> y1)
    {
        x1 = y1;
    }
    static void F2<T>(out ReadOnlySpan<T> x2, ReadOnlySpan<T> y2, params ReadOnlySpan<T> z2)
    {
        x2 = y2;
    }
}";
            var comp = CreateCompilation(source, references: new[] { GetSpanLibrary() });
            comp.VerifyDiagnostics(
                // (10,14): error CS8980: Cannot use params 'y1' in this context because it may prevent reuse at the call-site
                //         x1 = y1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y1").WithArguments("y1").WithLocation(10, 14));
        }

        [Fact]
        public void CannotReturnSpan_03()
        {
            var source =
@"using System;
class Program
{
    static Span<T> F1<T>(params Span<T> x1)
    {
        x1 = default;
        return x1;
    }
    static void F2<T>(out ReadOnlySpan<T> x2, params ReadOnlySpan<T> y2)
    {
        y2 = default;
        x2 = y2;
    }
}";
            var comp = CreateCompilation(source, references: new[] { GetSpanLibrary() });
            comp.VerifyDiagnostics(
                // (7,16): error CS8980: Cannot use params 'x1' in this context because it may prevent reuse at the call-site
                //         return x1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "x1").WithArguments("x1").WithLocation(7, 16),
                // (12,14): error CS8980: Cannot use params 'y2' in this context because it may prevent reuse at the call-site
                //         x2 = y2;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y2").WithArguments("y2").WithLocation(12, 14));
        }

        [Fact]
        public void CannotReturnSpan_04()
        {
            var source =
@"using System;
class Program
{
    static Span<T> F1<T>(Span<T> x1, params Span<T> y1)
    {
        x1 = y1;
        return x1;
    }
    static Span<T> F2<T>(Span<T> x2, params Span<T> y2)
    {
        x2 = y2;
        x2 = default;
        return x2;
    }
    static void F3<T>(out ReadOnlySpan<T> x3, ReadOnlySpan<T> y3, params ReadOnlySpan<T> z3)
    {
        y3 = z3;
        x3 = y3;
    }
    static void F4<T>(out ReadOnlySpan<T> x4, ReadOnlySpan<T> y4, params ReadOnlySpan<T> z4)
    {
        y4 = z4;
        y4 = default;
        x4 = y4;
    }
}";
            var comp = CreateCompilation(source, references: new[] { GetSpanLibrary() });
            comp.VerifyDiagnostics(
                // (6,14): error CS8980: Cannot use params 'y1' in this context because it may prevent reuse at the call-site
                //         x1 = y1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y1").WithArguments("y1").WithLocation(6, 14),
                // (11,14): error CS8980: Cannot use params 'y2' in this context because it may prevent reuse at the call-site
                //         x2 = y2;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y2").WithArguments("y2").WithLocation(11, 14),
                // (17,14): error CS8980: Cannot use params 'z3' in this context because it may prevent reuse at the call-site
                //         y3 = z3;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "z3").WithArguments("z3").WithLocation(17, 14),
                // (22,14): error CS8980: Cannot use params 'z4' in this context because it may prevent reuse at the call-site
                //         y4 = z4;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "z4").WithArguments("z4").WithLocation(22, 14));
        }

        /// <summary>
        /// Prefer params Span or ReadOnlySpan over params T[].
        /// </summary>
        [Fact]
        public void OverloadResolution_01()
        {
            var source =
@"using System;
class Program
{
    static void F1(params object[] args) { throw new Exception(); }
    static void F1(params Span<object> args) { foreach (var arg in args) Console.WriteLine(arg); }
    static void F2(params object[] args) { throw new Exception(); }
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

        [Fact]
        public void OverloadResolution_02()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(params T[] args) { Console.WriteLine(""F1<T>(params T[] args)""); }
    static void F1(params Span<object> args) { Console.WriteLine(""F1(params Span<object> args)""); }
    static void F2(params object[] args) { Console.WriteLine(""F2(params object[] args)""); }
    static void F2<T>(params Span<T> args) { Console.WriteLine(""F2<T>(params Span<T> args)""); }
    static void Main()
    {
        F1(1, 2, 3);
        F2(4, 5);
    }
}";
            CompileAndVerify(new[] { source, SpanSource, StackAllocDefinition }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"F1<T>(params T[] args)
F2<T>(params Span<T> args)
");
        }

        [Fact]
        public void OverloadResolution_03()
        {
            var source =
@"using System;
class Program
{
    static void F(params ReadOnlySpan<object> args) { }
    static void F(params Span<object> args) { }
    static void Main()
    {
        F(1, 2, 3);
    }
}";
            var comp = CreateCompilation(new[] { source, SpanSource, StackAllocDefinition }, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(params ReadOnlySpan<object>)' and 'Program.F(params Span<object>)'
                //         F(1, 2, 3);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(params System.ReadOnlySpan<object>)", "Program.F(params System.Span<object>)").WithLocation(8, 9));
        }

        [Fact]
        public void OverloadResolution_04()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(params ReadOnlySpan<T> args) { Console.WriteLine(""F1<T>(params ReadOnlySpan<T> args)""); }
    static void F1(params Span<object> args) { Console.WriteLine(""F1(params Span<object> args)""); }
    static void F2(params ReadOnlySpan<object> args) { Console.WriteLine(""F2(params ReadOnlySpan<object> args)""); }
    static void F2<T>(params Span<T> args) { Console.WriteLine(""F2<T>(params Span<T> args)""); }
    static void Main()
    {
        F1(1, 2, 3);
        F2(""hello"", ""world"");
    }
}";
            CompileAndVerify(new[] { source, SpanSource, StackAllocDefinition }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"F1<T>(params ReadOnlySpan<T> args)
F2<T>(params Span<T> args)
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
            var verifier = CompileAndVerify(new[] { source1, source2, SpanSource }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"StackAlloc<System.Int32>(2)
StackAlloc<System.Int32>(2)
StackAlloc<System.Int32>(3)
StackAlloc<System.Int32>(3)
1
2
3
4
5
6
7
8
9
10
11
12
13
14
15
16
17
18
19
20
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      194 (0xc2)
  .maxstack  3
  .locals init (System.Span<int> V_0,
                System.Span<int> V_1,
                System.Span<int> V_2,
                System.Span<int> V_3,
                int V_4) //offset
  IL_0000:  ldc.i4.2
  IL_0001:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.2
  IL_0008:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.3
  IL_000f:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_0014:  stloc.2
  IL_0015:  ldc.i4.3
  IL_0016:  call       ""System.Span<int> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<int>(int)""
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldc.i4.0
  IL_001f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0024:  ldc.i4.1
  IL_0025:  stind.i4
  IL_0026:  ldloca.s   V_0
  IL_0028:  ldc.i4.1
  IL_0029:  call       ""ref int System.Span<int>.this[int].get""
  IL_002e:  ldc.i4.2
  IL_002f:  stind.i4
  IL_0030:  ldloc.0
  IL_0031:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0036:  ldc.i4.2
  IL_0037:  stloc.s    V_4
  IL_0039:  br.s       IL_008e
  IL_003b:  ldloca.s   V_1
  IL_003d:  ldc.i4.0
  IL_003e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0043:  ldloc.s    V_4
  IL_0045:  ldc.i4.1
  IL_0046:  add
  IL_0047:  stind.i4
  IL_0048:  ldloca.s   V_1
  IL_004a:  ldc.i4.1
  IL_004b:  call       ""ref int System.Span<int>.this[int].get""
  IL_0050:  ldloc.s    V_4
  IL_0052:  ldc.i4.2
  IL_0053:  add
  IL_0054:  stind.i4
  IL_0055:  ldloc.1
  IL_0056:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_005b:  ldloca.s   V_2
  IL_005d:  ldc.i4.0
  IL_005e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0063:  ldloc.s    V_4
  IL_0065:  ldc.i4.3
  IL_0066:  add
  IL_0067:  stind.i4
  IL_0068:  ldloca.s   V_2
  IL_006a:  ldc.i4.1
  IL_006b:  call       ""ref int System.Span<int>.this[int].get""
  IL_0070:  ldloc.s    V_4
  IL_0072:  ldc.i4.4
  IL_0073:  add
  IL_0074:  stind.i4
  IL_0075:  ldloca.s   V_2
  IL_0077:  ldc.i4.2
  IL_0078:  call       ""ref int System.Span<int>.this[int].get""
  IL_007d:  ldloc.s    V_4
  IL_007f:  ldc.i4.5
  IL_0080:  add
  IL_0081:  stind.i4
  IL_0082:  ldloc.2
  IL_0083:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0088:  ldloc.s    V_4
  IL_008a:  ldc.i4.5
  IL_008b:  add
  IL_008c:  stloc.s    V_4
  IL_008e:  ldloc.s    V_4
  IL_0090:  ldc.i4.s   15
  IL_0092:  blt.s      IL_003b
  IL_0094:  ldloca.s   V_3
  IL_0096:  ldc.i4.0
  IL_0097:  call       ""ref int System.Span<int>.this[int].get""
  IL_009c:  ldloc.s    V_4
  IL_009e:  ldc.i4.1
  IL_009f:  add
  IL_00a0:  stind.i4
  IL_00a1:  ldloca.s   V_3
  IL_00a3:  ldc.i4.1
  IL_00a4:  call       ""ref int System.Span<int>.this[int].get""
  IL_00a9:  ldloc.s    V_4
  IL_00ab:  ldc.i4.2
  IL_00ac:  add
  IL_00ad:  stind.i4
  IL_00ae:  ldloca.s   V_3
  IL_00b0:  ldc.i4.2
  IL_00b1:  call       ""ref int System.Span<int>.this[int].get""
  IL_00b6:  ldloc.s    V_4
  IL_00b8:  ldc.i4.3
  IL_00b9:  add
  IL_00ba:  stind.i4
  IL_00bb:  ldloc.3
  IL_00bc:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_00c1:  ret
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
    static T ElementAt<T>(int index, params Span<T> args)
    {
        var value = args[index];
        Console.WriteLine(""ElementAt<{0}>({1}): {2}"", typeof(T), index, value);
        return value;
    }
    static void Main()
    {
        var value = ElementAt(
            0,
            ElementAt(1, 'a', 'b', 'c'),
            ElementAt(2, 'e', 'f', 'g'),
            'h');
        Console.WriteLine(value);
    }
}";
            // No buffer re-use.
            var verifier = CompileAndVerify(new[] { source1, source2, SpanSource }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"StackAlloc<System.Char>(3)
StackAlloc<System.Char>(3)
StackAlloc<System.Char>(3)
ElementAt<System.Char>(1): b
ElementAt<System.Char>(2): g
ElementAt<System.Char>(0): b
b
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      143 (0x8f)
  .maxstack  5
  .locals init (System.Span<char> V_0,
                System.Span<char> V_1,
                System.Span<char> V_2)
  IL_0000:  ldc.i4.3
  IL_0001:  call       ""System.Span<char> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<char>(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.3
  IL_0008:  call       ""System.Span<char> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<char>(int)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.3
  IL_000f:  call       ""System.Span<char> System.Runtime.CompilerServices.RuntimeHelpers.StackAlloc<char>(int)""
  IL_0014:  stloc.2
  IL_0015:  ldc.i4.0
  IL_0016:  ldloca.s   V_2
  IL_0018:  ldc.i4.0
  IL_0019:  call       ""ref char System.Span<char>.this[int].get""
  IL_001e:  ldc.i4.1
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldc.i4.0
  IL_0022:  call       ""ref char System.Span<char>.this[int].get""
  IL_0027:  ldc.i4.s   97
  IL_0029:  stind.i2
  IL_002a:  ldloca.s   V_0
  IL_002c:  ldc.i4.1
  IL_002d:  call       ""ref char System.Span<char>.this[int].get""
  IL_0032:  ldc.i4.s   98
  IL_0034:  stind.i2
  IL_0035:  ldloca.s   V_0
  IL_0037:  ldc.i4.2
  IL_0038:  call       ""ref char System.Span<char>.this[int].get""
  IL_003d:  ldc.i4.s   99
  IL_003f:  stind.i2
  IL_0040:  ldloc.0
  IL_0041:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_0046:  stind.i2
  IL_0047:  ldloca.s   V_2
  IL_0049:  ldc.i4.1
  IL_004a:  call       ""ref char System.Span<char>.this[int].get""
  IL_004f:  ldc.i4.2
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
  IL_0078:  ldloca.s   V_2
  IL_007a:  ldc.i4.2
  IL_007b:  call       ""ref char System.Span<char>.this[int].get""
  IL_0080:  ldc.i4.s   104
  IL_0082:  stind.i2
  IL_0083:  ldloc.2
  IL_0084:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_0089:  call       ""void System.Console.WriteLine(char)""
  IL_008e:  ret
}");
        }
    }
}
