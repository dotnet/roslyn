// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class Utf8StringsLiteralsTests : CompilingTestBase
    {
        private static string HelpersSource => @"
class Helpers
{
    public static void Print(ReadOnlySpan<byte> span)
    {
        System.Console.Write(""{"");
        foreach (var item in span)
        {
            System.Console.Write("" 0x{0:X}"", item);
        }
        System.Console.WriteLine("" }"");
    }
}
";

        [Fact]
        public void ImplicitConversions_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
    }

    static byte[] Test1() => ""hello"";
    static Span<byte> Test2() => ""dog"";
    static ReadOnlySpan<byte> Test3() => ""cat"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (9,30): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //     static byte[] Test1() => "hello";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "byte[]").WithLocation(9, 30),
                // (10,34): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //     static Span<byte> Test2() => "dog";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "System.Span<byte>").WithLocation(10, 34),
                // (11,42): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //     static ReadOnlySpan<byte> Test3() => "cat";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(11, 42)
                );
        }

        [Fact]
        public void ImplicitConversions_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
    }

    static byte[] Test1() => ""hello""U8;
    static Span<byte> Test2() => ""dog""U8;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (9,30): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'byte[]'
                //     static byte[] Test1() => "hello"U8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""U8").WithArguments("System.ReadOnlySpan<byte>", "byte[]").WithLocation(9, 30),
                // (10,34): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'System.Span<byte>'
                //     static Span<byte> Test2() => "dog"U8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""U8").WithArguments("System.ReadOnlySpan<byte>", "System.Span<byte>").WithLocation(10, 34)
                );
        }

        [Fact]
        public void ImplicitConversions_03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
    }

    const string nullConstant = null; 
    static byte[] Test1() => nullConstant;
    static Span<byte> Test2() => nullConstant;
    static ReadOnlySpan<byte> Test3() => nullConstant;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,30): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //     static byte[] Test1() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nullConstant").WithArguments("string", "byte[]").WithLocation(10, 30),
                // (11,34): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //     static Span<byte> Test2() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nullConstant").WithArguments("string", "System.Span<byte>").WithLocation(11, 34),
                // (12,42): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //     static ReadOnlySpan<byte> Test3() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nullConstant").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(12, 42)
                );
        }

        [Fact]
        public void ImplicitConversions_04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
    }

    const object nullConstant = null; 
    static byte[] Test1() => nullConstant;
    static Span<byte> Test2() => nullConstant;
    static ReadOnlySpan<byte> Test3() => nullConstant;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,30): error CS0266: Cannot implicitly convert type 'object' to 'byte[]'. An explicit conversion exists (are you missing a cast?)
                //     static byte[] Test1() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullConstant").WithArguments("object", "byte[]").WithLocation(10, 30),
                // (11,34): error CS0266: Cannot implicitly convert type 'object' to 'System.Span<byte>'. An explicit conversion exists (are you missing a cast?)
                //     static Span<byte> Test2() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullConstant").WithArguments("object", "System.Span<byte>").WithLocation(11, 34),
                // (11,34): error CS0266: Cannot implicitly convert type 'object' to 'System.Span<byte>'. An explicit conversion exists (are you missing a cast?)
                //     static Span<byte> Test2() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullConstant").WithArguments("object", "System.Span<byte>").WithLocation(11, 34),
                // (12,42): error CS0266: Cannot implicitly convert type 'object' to 'System.ReadOnlySpan<byte>'. An explicit conversion exists (are you missing a cast?)
                //     static ReadOnlySpan<byte> Test3() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullConstant").WithArguments("object", "System.ReadOnlySpan<byte>").WithLocation(12, 42),
                // (12,42): error CS0266: Cannot implicitly convert type 'object' to 'System.ReadOnlySpan<byte>'. An explicit conversion exists (are you missing a cast?)
                //     static ReadOnlySpan<byte> Test3() => nullConstant;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullConstant").WithArguments("object", "System.ReadOnlySpan<byte>").WithLocation(12, 42)
                );
        }

        [Fact]
        public void ImplicitConversions_TupleLiteral_01()
        {
            var source = @"

class C
{
    static void Main()
    {
        (byte[] b, (byte[] d, string e) c) a = (""hello"", (""dog"", ""cat""));
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (7,49): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         (byte[] b, (byte[] d, string e) c) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "byte[]").WithLocation(7, 49),
                // (7,59): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         (byte[] b, (byte[] d, string e) c) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "byte[]").WithLocation(7, 59)
                );
        }

        [Fact]
        public void ImplicitConversions_Deconstruction_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (byte[] a, (byte[] b, string c)) = (""hello"", (""dog"", ""cat""));
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (6,45): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         (byte[] a, (byte[] b, string c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "byte[]").WithLocation(6, 45),
                // (6,55): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         (byte[] a, (byte[] b, string c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "byte[]").WithLocation(6, 55)
                );
        }

        [Fact]
        public void ExplicitConversions_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var array = (byte[])""hello"";
        var span = (Span<byte>)""dog"";
        var readonlySpan = (ReadOnlySpan<byte>)""cat"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (7,21): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         var array = (byte[])"hello";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(byte[])""hello""").WithArguments("string", "byte[]").WithLocation(7, 21),
                // (8,20): error CS0030: Cannot convert type 'string' to 'System.Span<byte>'
                //         var span = (Span<byte>)"dog";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(Span<byte>)""dog""").WithArguments("string", "System.Span<byte>").WithLocation(8, 20),
                // (9,28): error CS0030: Cannot convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         var readonlySpan = (ReadOnlySpan<byte>)"cat";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(ReadOnlySpan<byte>)""cat""").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(9, 28)
                );
        }

        [Fact]
        public void ExplicitConversions_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var array = (byte[])""hello""u8;
        var span = (Span<byte>)""dog""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (7,21): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'byte[]'
                //         var array = (byte[])"hello"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(byte[])""hello""u8").WithArguments("System.ReadOnlySpan<byte>", "byte[]").WithLocation(7, 21),
                // (8,20): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'System.Span<byte>'
                //         var span = (Span<byte>)"dog"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(Span<byte>)""dog""u8").WithArguments("System.ReadOnlySpan<byte>", "System.Span<byte>").WithLocation(8, 20)
                );
        }

        [Fact]
        public void ExplicitConversions_03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        const string nullConstant = null;
        var array = (byte[])nullConstant;
        var span = (Span<byte>)nullConstant;
        var readonlySpan = (ReadOnlySpan<byte>)nullConstant;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,21): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         var array = (byte[])nullConstant;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(byte[])nullConstant").WithArguments("string", "byte[]").WithLocation(8, 21),
                // (9,20): error CS0030: Cannot convert type 'string' to 'System.Span<byte>'
                //         var span = (Span<byte>)nullConstant;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<byte>)nullConstant").WithArguments("string", "System.Span<byte>").WithLocation(9, 20),
                // (10,28): error CS0030: Cannot convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         var readonlySpan = (ReadOnlySpan<byte>)nullConstant;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<byte>)nullConstant").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(10, 28)
                );
        }

        [Fact]
        public void ExplicitConversions_04()
        {
            var source = @"#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        const object nullConstant = null;
        var array = (byte[])nullConstant;
        var span = (Span<byte>)nullConstant;
        var readonlySpan = (ReadOnlySpan<byte>)nullConstant;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (9,20): error CS0457: Ambiguous user defined conversions 'Span<byte>.implicit operator Span<byte>(ArraySegment<byte>)' and 'Span<byte>.implicit operator Span<byte>(byte[]?)' when converting from 'object' to 'Span<byte>'
                //         var span = (Span<byte>)nullConstant;
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(Span<byte>)nullConstant").WithArguments("System.Span<byte>.implicit operator System.Span<byte>(System.ArraySegment<byte>)", "System.Span<byte>.implicit operator System.Span<byte>(byte[]?)", "object", "System.Span<byte>").WithLocation(9, 20),
                // (10,28): error CS0457: Ambiguous user defined conversions 'ReadOnlySpan<byte>.implicit operator ReadOnlySpan<byte>(ArraySegment<byte>)' and 'ReadOnlySpan<byte>.implicit operator ReadOnlySpan<byte>(byte[]?)' when converting from 'object' to 'ReadOnlySpan<byte>'
                //         var readonlySpan = (ReadOnlySpan<byte>)nullConstant;
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(ReadOnlySpan<byte>)nullConstant").WithArguments("System.ReadOnlySpan<byte>.implicit operator System.ReadOnlySpan<byte>(System.ArraySegment<byte>)", "System.ReadOnlySpan<byte>.implicit operator System.ReadOnlySpan<byte>(byte[]?)", "object", "System.ReadOnlySpan<byte>").WithLocation(10, 28)
                );
        }

        [Fact]
        public void ExplicitConversions_TupleLiteral_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var a = ((byte[] b, (byte[] d, string e) c))(""hello"", (""dog"", ""cat""));
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (6,54): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         var a = ((byte[] b, (byte[] d, string e) c))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""hello""").WithArguments("string", "byte[]").WithLocation(6, 54),
                // (6,64): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         var a = ((byte[] b, (byte[] d, string e) c))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""dog""").WithArguments("string", "byte[]").WithLocation(6, 64)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InvalidContent_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        _ = ""hello \uD801\uD802""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS9026: The input string cannot be converted into the equivalent UTF-8 byte representation. Unable to translate Unicode character \\uD801 at index 6 to specified code page.
                //         _ = "hello \uD801\uD802"u8;
                Diagnostic(ErrorCode.ERR_CannotBeConvertedToUtf8, @"""hello \uD801\uD802""u8").WithArguments(@"Unable to translate Unicode character \\uD801 at index 6 to specified code page.").WithLocation(6, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InvalidContent_03()
        {
            var source = @"
class C
{
    static void Main()
    {
        _ = ""\uD83D\uDE00""u8;
        _ = ""\uD83D""u8 + ""\uDE00""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS9026: The input string cannot be converted into the equivalent UTF-8 byte representation. Unable to translate Unicode character \\uD83D at index 0 to specified code page.
                //         _ = "\uD83D"u8 + "\uDE00"u8;
                Diagnostic(ErrorCode.ERR_CannotBeConvertedToUtf8, @"""\uD83D""u8").WithArguments(@"Unable to translate Unicode character \\uD83D at index 0 to specified code page.").WithLocation(7, 13),
                // (7,26): error CS9026: The input string cannot be converted into the equivalent UTF-8 byte representation. Unable to translate Unicode character \\uDE00 at index 0 to specified code page.
                //         _ = "\uD83D"u8 + "\uDE00"u8;
                Diagnostic(ErrorCode.ERR_CannotBeConvertedToUtf8, @"""\uDE00""u8").WithArguments(@"Unable to translate Unicode character \\uDE00 at index 0 to specified code page.").WithLocation(7, 26)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void NoBehaviorChangeForConversionFromNullLiteral_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        System.Console.WriteLine(Test1() is null ? -1 : 0);
        System.Console.WriteLine(Test2().Length);
        System.Console.WriteLine(Test3().Length);
    }

    static byte[] Test1() => null;
    static Span<byte> Test2() => null;
    static ReadOnlySpan<byte> Test3() => null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
-1
0
0
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test1()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}
");

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       ""System.Span<byte> System.Span<byte>.op_Implicit(byte[])""
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
  IL_0006:  ret
}
");

            comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);

            CompileAndVerify(comp, expectedOutput: @"
-1
0
0
", verify: Verification.Fails).VerifyDiagnostics();

            comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);

            CompileAndVerify(comp, expectedOutput: @"
-1
0
0
", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void NoConversionFromType_01()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        string value = ""s"";
        byte[] array = value;
        Span<byte> span = value;
        ReadOnlySpan<byte> readonlySpan = value;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (9,24): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         byte[] array = value;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "value").WithArguments("string", "byte[]").WithLocation(9, 24),
                // (10,27): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //         Span<byte> span = value;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "value").WithArguments("string", "System.Span<byte>").WithLocation(10, 27),
                // (11,43): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         ReadOnlySpan<byte> readonlySpan = value;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "value").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(11, 43)
                );
        }

        [Fact]
        public void NoConversionFromType_02()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        string value = ""s"";
        var array = (byte[])value;
        var span = (Span<byte>)value;
        var readonlySpan = (ReadOnlySpan<byte>)value;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (9,21): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         var array = (byte[])value;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(byte[])value").WithArguments("string", "byte[]").WithLocation(9, 21),
                // (10,20): error CS0030: Cannot convert type 'string' to 'System.Span<byte>'
                //         var span = (Span<byte>)value;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<byte>)value").WithArguments("string", "System.Span<byte>").WithLocation(10, 20),
                // (11,28): error CS0030: Cannot convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         var readonlySpan = (ReadOnlySpan<byte>)value;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<byte>)value").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(11, 28)
                );
        }

        [Fact]
        public void InvalidTargetType_01()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        const string s = ""s"";

        byte[,] array1 = s;
        Span<byte[]> span1 = s;
        ReadOnlySpan<byte[]> readonlySpan1 = s;

        int[] array2 = s;
        Span<int> span2 = s;
        ReadOnlySpan<int> readonlySpan2 = s;

        char[] array3 = s;
        Span<char> span3 = s;

        string[] array4 = s;
        Span<string> span4 = s;
        ReadOnlySpan<string> readonlySpan4 = s;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (10,26): error CS0029: Cannot implicitly convert type 'string' to 'byte[*,*]'
                //         byte[,] array1 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "byte[*,*]").WithLocation(10, 26),
                // (11,30): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte[]>'
                //         Span<byte[]> span1 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.Span<byte[]>").WithLocation(11, 30),
                // (12,46): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte[]>'
                //         ReadOnlySpan<byte[]> readonlySpan1 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.ReadOnlySpan<byte[]>").WithLocation(12, 46),
                // (14,24): error CS0029: Cannot implicitly convert type 'string' to 'int[]'
                //         int[] array2 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "int[]").WithLocation(14, 24),
                // (15,27): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<int>'
                //         Span<int> span2 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.Span<int>").WithLocation(15, 27),
                // (16,43): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<int>'
                //         ReadOnlySpan<int> readonlySpan2 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.ReadOnlySpan<int>").WithLocation(16, 43),
                // (18,25): error CS0029: Cannot implicitly convert type 'string' to 'char[]'
                //         char[] array3 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "char[]").WithLocation(18, 25),
                // (19,28): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<char>'
                //         Span<char> span3 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.Span<char>").WithLocation(19, 28),
                // (21,27): error CS0029: Cannot implicitly convert type 'string' to 'string[]'
                //         string[] array4 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "string[]").WithLocation(21, 27),
                // (22,30): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<string>'
                //         Span<string> span4 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.Span<string>").WithLocation(22, 30),
                // (23,46): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<string>'
                //         ReadOnlySpan<string> readonlySpan4 = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.ReadOnlySpan<string>").WithLocation(23, 46)
                );
        }

        [Fact]
        public void NotSZArray_01()
        {
            var il = @"
.class public auto ansi beforefieldinit Test
    extends System.Object
{
    .method public hidebysig specialname static 
        void set_P (
            uint8[1] 'value'
        ) cil managed 
    {
        .maxstack 8
        IL_0000: nop
        IL_0001: ret
    }

    .property uint8[1] P()
    {
        .set void Test::set_P(uint8[1])
    }
}
";

            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used

class C
{
    static void Main()
    {
        Test.P = ""s"";
    }
}
";

            var comp = CreateCompilationWithIL(source, il);
            var p = comp.GetMember<PropertySymbol>("Test.P");
            var type = (ArrayTypeSymbol)p.Type;
            Assert.Equal("System.Byte[*]", type.ToTestDisplayString());
            Assert.Equal(1, type.Rank);
            Assert.False(type.IsSZArray);
            comp.VerifyEmitDiagnostics(
                // (8,18): error CS0029: Cannot implicitly convert type 'string' to 'byte[*]'
                //         Test.P = "s";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""s""").WithArguments("string", "byte[*]").WithLocation(8, 18)
                );
        }

        [Fact]
        public void NotARefStruct_01()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        Span<byte> span = ""dog"";
    }
}

namespace System
{
    public readonly struct Span<T>
    {
        public Span(T[] arr)
        {
        }
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,27): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //         Span<byte> span = "dog";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "System.Span<byte>").WithLocation(8, 27)
                );
        }

        [Fact]
        public void NotARefStruct_02()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        ReadOnlySpan<byte> readonlySpan = ""cat"";
    }
}


namespace System
{
    public readonly struct ReadOnlySpan<T>
    {
        public ReadOnlySpan(T[] arr)
        {
        }
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,43): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         ReadOnlySpan<byte> readonlySpan = "cat";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(8, 43)
                );
        }

        [Fact]
        public void NotARefStruct_03()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        Span<byte> span = ""dog"";
    }
}

namespace System
{
    public class Span<T>
    {
        public Span(T[] arr)
        {
        }
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,27): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //         Span<byte> span = "dog";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "System.Span<byte>").WithLocation(8, 27)
                );
        }

        [Fact]
        public void NotARefStruct_04()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        ReadOnlySpan<byte> readonlySpan = ""cat"";
    }
}


namespace System
{
    public class ReadOnlySpan<T>
    {
        public ReadOnlySpan(T[] arr)
        {
        }
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,43): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         ReadOnlySpan<byte> readonlySpan = "cat";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(8, 43)
                );
        }

        [Fact]
        public void UserDefinedImplicitConversions_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        C1 x = ""hello"";
        C2 y = ""dog"";
        C3 z = ""cat"";
    }
}

class C1
{
    public static implicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,16): error CS0029: Cannot implicitly convert type 'string' to 'C1'
                //         C1 x = "hello";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(7, 16),
                // (8,16): error CS0029: Cannot implicitly convert type 'string' to 'C2'
                //         C2 y = "dog";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(8, 16),
                // (9,16): error CS0029: Cannot implicitly convert type 'string' to 'C3'
                //         C3 z = "cat";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(9, 16)
                );
        }

        [Fact]
        public void UserDefinedImplicitConversions_TupleLiteral_01()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        (C1, (C2, C3)) a = (""hello"", (""dog"", ""cat""));
    }
}

class C1
{
    public static implicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,29): error CS0029: Cannot implicitly convert type 'string' to 'C1'
                //         (C1, (C2, C3)) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(8, 29),
                // (8,39): error CS0029: Cannot implicitly convert type 'string' to 'C2'
                //         (C1, (C2, C3)) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(8, 39),
                // (8,46): error CS0029: Cannot implicitly convert type 'string' to 'C3'
                //         (C1, (C2, C3)) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(8, 46)
                );
        }

        [Fact]
        public void UserDefinedImplicitConversions_Deconstruction_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        (C1 a, (C2 b, C3 c)) = (""hello"", (""dog"", ""cat""));
    }
}

class C1
{
    public static implicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,33): error CS0029: Cannot implicitly convert type 'string' to 'C1'
                //         (C1 a, (C2 b, C3 c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(7, 33),
                // (7,43): error CS0029: Cannot implicitly convert type 'string' to 'C2'
                //         (C1 a, (C2 b, C3 c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(7, 43),
                // (7,50): error CS0029: Cannot implicitly convert type 'string' to 'C3'
                //         (C1 a, (C2 b, C3 c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(7, 50)
                );
        }

        [Fact]
        public void UserDefinedImplicitConversions_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        var x = (C1)""hello"";
        var y = (C2)""dog"";
        var z = (C3)""cat"";
    }
}

class C1
{
    public static implicit operator C1(byte[] x)
    {
        Helpers.Print(x);
        return new C1();
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        Helpers.Print(x);
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        Helpers.Print(x);
        return new C3();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,17): error CS0030: Cannot convert type 'string' to 'C1'
                //         var x = (C1)"hello";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C1)""hello""").WithArguments("string", "C1").WithLocation(8, 17),
                // (9,17): error CS0030: Cannot convert type 'string' to 'C2'
                //         var y = (C2)"dog";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C2)""dog""").WithArguments("string", "C2").WithLocation(9, 17),
                // (10,17): error CS0030: Cannot convert type 'string' to 'C3'
                //         var z = (C3)"cat";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C3)""cat""").WithArguments("string", "C3").WithLocation(10, 17)
                );
        }

        [Fact]
        public void UserDefinedImplicitConversions_TupleLiteral_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        _ = ((C1, (C2, C3)))(""hello"", (""dog"", ""cat""));
    }
}

class C1
{
    public static implicit operator C1(byte[] x)
    {
        Helpers.Print(x);
        return new C1();
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        Helpers.Print(x);
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        Helpers.Print(x);
        return new C3();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,30): error CS0030: Cannot convert type 'string' to 'C1'
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(8, 30),
                // (8,40): error CS0030: Cannot convert type 'string' to 'C2'
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(8, 40),
                // (8,47): error CS0030: Cannot convert type 'string' to 'C3'
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(8, 47)
                );
        }

        [Fact]
        public void UserDefinedExplicitConversions_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        var x = (C1)""hello"";
        var y = (C2)""dog"";
        var z = (C3)""cat"";
    }
}

class C1
{
    public static explicit operator C1(byte[] x)
    {
        Helpers.Print(x);
        return new C1();
    }
}

class C2
{
    public static explicit operator C2(Span<byte> x)
    {
        Helpers.Print(x);
        return new C2();
    }
}

class C3
{
    public static explicit operator C3(ReadOnlySpan<byte> x)
    {
        Helpers.Print(x);
        return new C3();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,17): error CS0030: Cannot convert type 'string' to 'C1'
                //         var x = (C1)"hello";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C1)""hello""").WithArguments("string", "C1").WithLocation(8, 17),
                // (9,17): error CS0030: Cannot convert type 'string' to 'C2'
                //         var y = (C2)"dog";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C2)""dog""").WithArguments("string", "C2").WithLocation(9, 17),
                // (10,17): error CS0030: Cannot convert type 'string' to 'C3'
                //         var z = (C3)"cat";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C3)""cat""").WithArguments("string", "C3").WithLocation(10, 17)
                );
        }

        [Fact]
        public void UserDefinedExplicitConversions_TupleLiteral_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        _ = ((C1, (C2, C3)))(""hello"", (""dog"", ""cat""));
    }
}

class C1
{
    public static explicit operator C1(byte[] x)
    {
        Helpers.Print(x);
        return new C1();
    }
}

class C2
{
    public static explicit operator C2(Span<byte> x)
    {
        Helpers.Print(x);
        return new C2();
    }
}

class C3
{
    public static explicit operator C3(ReadOnlySpan<byte> x)
    {
        Helpers.Print(x);
        return new C3();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,30): error CS0030: Cannot convert type 'string' to 'C1'
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(8, 30),
                // (8,40): error CS0030: Cannot convert type 'string' to 'C2'
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(8, 40),
                // (8,47): error CS0030: Cannot convert type 'string' to 'C3'
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(8, 47)
                );
        }

        [Fact]
        public void UserDefinedExplicitConversions_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        C1 x = ""hello"";
        C2 y = ""dog"";
        C3 z = ""cat"";
    }
}

class C1
{
    public static explicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static explicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static explicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,16): error CS0029: Cannot implicitly convert type 'string' to 'C1'
                //         C1 x = "hello";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(7, 16),
                // (8,16): error CS0029: Cannot implicitly convert type 'string' to 'C2'
                //         C2 y = "dog";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(8, 16),
                // (9,16): error CS0029: Cannot implicitly convert type 'string' to 'C3'
                //         C3 z = "cat";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(9, 16)
                );
        }

        [Fact]
        public void UserDefinedExplicitConversions_TupleLiteral_02()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        (C1, (C2, C3)) a = (""hello"", (""dog"", ""cat""));
    }
}

class C1
{
    public static explicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static explicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static explicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,29): error CS0029: Cannot implicitly convert type 'string' to 'C1'
                //         (C1, (C2, C3)) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(8, 29),
                // (8,39): error CS0029: Cannot implicitly convert type 'string' to 'C2'
                //         (C1, (C2, C3)) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(8, 39),
                // (8,46): error CS0029: Cannot implicitly convert type 'string' to 'C3'
                //         (C1, (C2, C3)) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(8, 46)
                );
        }

        [Fact]
        public void UserDefinedExplicitConversions_Deconstruction_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        (C1 a, (C2 b, C3 c)) = (""hello"", (""dog"", ""cat""));
    }
}

class C1
{
    public static explicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static explicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static explicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,33): error CS0029: Cannot implicitly convert type 'string' to 'C1'
                //         (C1 a, (C2 b, C3 c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "C1").WithLocation(7, 33),
                // (7,43): error CS0029: Cannot implicitly convert type 'string' to 'C2'
                //         (C1 a, (C2 b, C3 c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""").WithArguments("string", "C2").WithLocation(7, 43),
                // (7,50): error CS0029: Cannot implicitly convert type 'string' to 'C3'
                //         (C1 a, (C2 b, C3 c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""").WithArguments("string", "C3").WithLocation(7, 50)
                );
        }

        [Fact]
        public void ExpressionTree_02()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class C
{
    static void Main()
    {
        Expression<Func<C1>> x = () => (C1)""hello"";
        Expression<Func<C2>> y = () => (C2)""dog"";
        Expression<Func<C3>> z = () => (C3)""cat"";

        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
        System.Console.WriteLine(z);
    }
}

class C1
{
    public static implicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,40): error CS0030: Cannot convert type 'string' to 'C1'
                //         Expression<Func<C1>> x = () => (C1)"hello";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C1)""hello""").WithArguments("string", "C1").WithLocation(8, 40),
                // (9,40): error CS0030: Cannot convert type 'string' to 'C2'
                //         Expression<Func<C2>> y = () => (C2)"dog";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C2)""dog""").WithArguments("string", "C2").WithLocation(9, 40),
                // (10,40): error CS0030: Cannot convert type 'string' to 'C3'
                //         Expression<Func<C3>> z = () => (C3)"cat";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C3)""cat""").WithArguments("string", "C3").WithLocation(10, 40)
                );
        }

        [Fact]
        public void ExpressionTree_03()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class C
{
    static void Main()
    {
        Expression<Func<byte[]>> x = () => ""hello""u8.ToArray();
        System.Console.WriteLine(x);
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,44): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
                //         Expression<Func<byte[]>> x = () => "hello"u8.ToArray();
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, @"""hello""u8").WithArguments("ReadOnlySpan").WithLocation(8, 44)
                );
        }

        [Fact]
        public void ExpressionTree_04()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class C
{
    static void Main()
    {
        Expression<Func<byte[]>> x = () => (""h""u8 + ""ello""u8).ToArray();
        System.Console.WriteLine(x);
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,45): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
                //         Expression<Func<byte[]>> x = () => ("h"u8 + "ello"u8).ToArray();
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, @"""h""u8 + ""ello""u8").WithArguments("ReadOnlySpan").WithLocation(8, 45),
                // (8,45): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
                //         Expression<Func<byte[]>> x = () => ("h"u8 + "ello"u8).ToArray();
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, @"""h""u8").WithArguments("ReadOnlySpan").WithLocation(8, 45),
                // (8,53): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
                //         Expression<Func<byte[]>> x = () => ("h"u8 + "ello"u8).ToArray();
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, @"""ello""u8").WithArguments("ReadOnlySpan").WithLocation(8, 53)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.Write(Test(""s""));
        System.Console.Write(Test(""s""u8));
    }

    static string Test(ReadOnlySpan<char> a) => ""ReadOnlySpan<char>"";
    static string Test(byte[] a) => ""array"";
    static string Test(ReadOnlySpan<byte> a) => ""ReadOnlySpan<byte>"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpan<char>ReadOnlySpan<byte>", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void OverloadResolution_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.Write(Test(""s""));
        System.Console.Write(Test(""s""u8));
    }

    static string Test(byte[] a) => ""array"";
    static string Test(ReadOnlySpan<char> a) => ""ReadOnlySpan"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (8,35): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<byte>' to 'byte[]'
                //         System.Console.Write(Test("s"u8));
                Diagnostic(ErrorCode.ERR_BadArgType, @"""s""u8").WithArguments("1", "System.ReadOnlySpan<byte>", "byte[]").WithLocation(8, 35)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_03()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""));
    }

    static string Test(C1 a) => ""string"";
    static string Test(C2 a) => ""array"";
}

class C1
{
    public static implicit operator C1(string x)
    {
        return new C1();
    }
}

class C2
{
    public static implicit operator C2(byte[] x)
    {
        return new C2();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"string").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s"", (int)1));
    }

    static string Test(ReadOnlySpan<char> a, long x) => ""ReadOnlySpan"";
    static string Test(byte[] a, int x) => ""array"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpan").VerifyDiagnostics();

            comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpan").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_05()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Test(""s"");
    }

    static void Test(C1 a) {}
}

class C1
{
    public static implicit operator C1(string x)
    {
        System.Console.WriteLine(""string"");
        return new C1();
    }

    public static implicit operator C1(ReadOnlySpan<byte> x)
    {
        System.Console.WriteLine(""ReadOnlySpan<byte>"");
        return new C1();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"string").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_10()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        var p = new Program();
        Console.WriteLine(p.M(""""));
    }

    public string M(byte[] b) => ""byte[]"";
}

static class E
{
    public static string M(this object o, string s) => ""string"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"string").VerifyDiagnostics();

            comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, expectedOutput: @"string").VerifyDiagnostics();
        }

        [Fact]
        public void NullableAnalysis_03()
        {
            var source = @"
#nullable enable

class C
{
    static void Main()
    {
        _ = ""hello""u8.Length;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Utf8StringLiteral_01(string suffix)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }





    static ReadOnlySpan<byte> Test3() => ""cat""" + suffix + @";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (15,42): error CS8936: Feature 'UTF-8 string literals' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static ReadOnlySpan<byte> Test3() => "cat"u8;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""cat""" + suffix).WithArguments("UTF-8 string literals", "11.0").WithLocation(15, 42)
                );
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Utf8StringLiteral_02(string suffix)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();


        Helpers.Print(Test3());
    }



    static ReadOnlySpan<byte> Test3() => @""cat""" + suffix + @";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (15,42): error CS8936: Feature 'UTF-8 string literals' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static ReadOnlySpan<byte> Test3() => @"cat"u8;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"@""cat""" + suffix).WithArguments("UTF-8 string literals", "11.0").WithLocation(15, 42)
                );
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Utf8StringLiteral_03(string suffix)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();


        Helpers.Print(Test3());
    }



    static ReadOnlySpan<byte> Test3() => """"""cat""""""" + suffix + @";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (15,42): error CS8936: Feature 'raw string literals' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static ReadOnlySpan<byte> Test3() => """cat"""U8;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""""cat""""""" + suffix).WithArguments("raw string literals", "11.0").WithLocation(15, 42),
                // (15,42): error CS8936: Feature 'UTF-8 string literals' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static ReadOnlySpan<byte> Test3() => """cat"""U8;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""""cat""""""" + suffix).WithArguments("UTF-8 string literals", "11.0").WithLocation(15, 42)
                );
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Utf8StringLiteral_04(string suffix)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();


        Helpers.Print(Test3());
    }







    static ReadOnlySpan<byte> Test3() => """"""
  cat
  """"""" + suffix + @";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (19,42): error CS8936: Feature 'raw string literals' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static ReadOnlySpan<byte> Test3() => """
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""""
  cat
  """"""" + suffix).WithArguments("raw string literals", "11.0").WithLocation(19, 42),
                // (19,42): error CS8936: Feature 'UTF-8 string literals' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static ReadOnlySpan<byte> Test3() => """
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""""
  cat
  """"""" + suffix).WithArguments("UTF-8 string literals", "11.0").WithLocation(19, 42)
                );
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Utf8StringLiteral_01_InPlaceCtorCall(string suffix)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3()
    {
        var x = ""cat""" + suffix + @";
        return x;
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       20 (0x14)
  .maxstack  3
  .locals init (System.ReadOnlySpan<byte> V_0, //x
                System.ReadOnlySpan<byte> V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldsflda    ""int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57""
  IL_0008:  ldc.i4.3
  IL_0009:  call       ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000e:  ldloc.0
  IL_000f:  stloc.1
  IL_0010:  br.s       IL_0012
  IL_0012:  ldloc.1
  IL_0013:  ret
}
");
        }

        [Fact]
        public void MissingType_01()
        {
            var source = @"

class C
{
    static void Main()
    {
        _ = ""hello""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeTypeMissing(SpecialType.System_Byte);
            comp.VerifyDiagnostics(
                // (7,13): error CS0518: Predefined type 'System.Byte' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.Byte").WithLocation(7, 13)
                );
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS0518: Predefined type 'System.Byte' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.Byte").WithLocation(7, 13)
                );
        }

        [Fact]
        public void MissingType_02()
        {
            var source = @"

class C
{
    static void Main()
    {
        _ = ""hello""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe);
            comp.MakeTypeMissing(SpecialType.System_Int32);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.Int32").WithLocation(7, 13),
                // (7,13): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.Int32").WithLocation(7, 13),
                // (7,13): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.Int32").WithLocation(7, 13)
                );
        }

        [Fact]
        public void MissingType_03()
        {
            var source = @"

class C
{
    static void Main()
    {
        _ = ""hello""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeTypeMissing(WellKnownType.System_ReadOnlySpan_T);
            comp.VerifyDiagnostics(
                // (7,13): error CS0518: Predefined type 'System.ReadOnlySpan`1' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.ReadOnlySpan`1").WithLocation(7, 13)
                );
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS0518: Predefined type 'System.ReadOnlySpan`1' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.ReadOnlySpan`1").WithLocation(7, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_07()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3() => ""cat""u8;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_09()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3() => ""cat""u8;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer);
            var verifier = CompileAndVerify(comp, expectedOutput: "{ 0x63 0x61 0x74 }", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.3
  IL_0013:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[], int, int)""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void MissingHelpers_10()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3() => ""cat""u8;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array_Start_Length);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (11,42): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1..ctor'
                //     static ReadOnlySpan<byte> Test3() => "cat"u8;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""cat""u8").WithArguments("System.ReadOnlySpan`1", ".ctor").WithLocation(11, 42)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_11()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""u8));
    }

    static string Test(ReadOnlySpan<byte> a) => ""ReadOnlySpan"";
    static string Test(byte[] a) => ""array"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpan", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_13()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""u8));
    }

    static string Test(ReadOnlySpan<byte> a) => ""ReadOnlySpan"";
    static string Test(Span<byte> a) => ""Span"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpan", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_14()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""u8));
    }

    static string Test(ReadOnlySpan<byte> a) => ""ReadOnlySpan"";
    static string Test(byte[] a) => ""array"";
    static string Test(Span<byte> a) => ""Span"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpan", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedImplicitConversions_03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        C1 x = ""hello""u8;
    }
}

class C1
{
    public static implicit operator C1(ReadOnlySpan<byte> x)
    {
        Helpers.Print(x);
        return new C1();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedImplicitConversions_04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (C1)""hello""u8;
    }
}

class C1
{
    public static implicit operator C1(ReadOnlySpan<byte> x)
    {
        Helpers.Print(x);
        return new C1();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void UserDefinedImplicitConversions_05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        C2 y = ""dog""u8;
        C3 z = ""cat""u8;
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(byte[] x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,16): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'C2'
                //         C2 y = "dog"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""u8").WithArguments("System.ReadOnlySpan<byte>", "C2").WithLocation(7, 16),
                // (8,16): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'C3'
                //         C3 z = "cat"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""u8").WithArguments("System.ReadOnlySpan<byte>", "C3").WithLocation(8, 16)
                );
        }

        [Fact]
        public void UserDefinedImplicitConversions_06()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var y = (C2)""dog""u8;
        var z = (C3)""cat""u8;
    }
}

class C2
{
    public static implicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static implicit operator C3(byte[] x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,17): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'C2'
                //         var y = (C2)"dog"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C2)""dog""u8").WithArguments("System.ReadOnlySpan<byte>", "C2").WithLocation(7, 17),
                // (8,17): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'C3'
                //         var z = (C3)"cat"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C3)""cat""u8").WithArguments("System.ReadOnlySpan<byte>", "C3").WithLocation(8, 17)
                );
        }

        [Fact]
        public void UserDefinedExplicitConversions_03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        C1 x = ""hello""u8;
        C2 y = ""dog""u8;
        C3 z = ""cat""u8;
    }
}

class C1
{
    public static explicit operator C1(byte[] x)
    {
        return new C1();
    }
}

class C2
{
    public static explicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static explicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (7,16): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'C1'
                //         C1 x = "hello"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""u8").WithArguments("System.ReadOnlySpan<byte>", "C1").WithLocation(7, 16),
                // (8,16): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'C2'
                //         C2 y = "dog"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""u8").WithArguments("System.ReadOnlySpan<byte>", "C2").WithLocation(8, 16),
                // (9,16): error CS0266: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'C3'. An explicit conversion exists (are you missing a cast?)
                //         C3 z = "cat"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""cat""u8").WithArguments("System.ReadOnlySpan<byte>", "C3").WithLocation(9, 16)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedExplicitConversions_04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (C1)""hello""u8;
    }
}

class C1
{
    public static explicit operator C1(ReadOnlySpan<byte> x)
    {
        Helpers.Print(x);
        return new C1();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void UserDefinedExplicitConversions_05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var y = (C2)""dog""u8;
        var z = (C3)""cat""u8;
    }
}

class C2
{
    public static explicit operator C2(Span<byte> x)
    {
        return new C2();
    }
}

class C3
{
    public static explicit operator C3(byte[] x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,17): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'C2'
                //         var y = (C2)"dog"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C2)""dog""u8").WithArguments("System.ReadOnlySpan<byte>", "C2").WithLocation(7, 17),
                // (8,17): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'C3'
                //         var z = (C3)"cat"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C3)""cat""u8").WithArguments("System.ReadOnlySpan<byte>", "C3").WithLocation(8, 17)
                );
        }

        [Theory]
        [InlineData(@"""hello""")]
        [InlineData(@"@""hello""")]
        [InlineData(@"""""""hello""""""")]
        [InlineData(@"""""""
  hello
  """"""")]
        public void NaturalType_01(string literal)
        {
            var source = @"
class C
{
    static void Main()
    {
        PrintType(" + literal + @"u8);
    }

    static void PrintType<T>(T value)
    {
        System.Console.WriteLine(typeof(T));
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (6,9): error CS9244: The type 'ReadOnlySpan<byte>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'C.PrintType<T>(T)'
                //         PrintType(@"hello"u8);
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "PrintType").WithArguments("C.PrintType<T>(T)", "T", "System.ReadOnlySpan<byte>").WithLocation(6, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_15()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test((""s"", 1)));
    }

    static string Test((object, int) a) => ""object"";
    static string Test((byte[], int) a) => ""array"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"object").VerifyDiagnostics();

            comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, expectedOutput: @"object").VerifyDiagnostics();
        }

        [Fact]
        public void NoConversionFromNonStringNull_01()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        const object nullValue = null;
        byte[] array = nullValue;
        Span<byte> span = nullValue;
        ReadOnlySpan<byte> readonlySpan = nullValue;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (9,24): error CS0266: Cannot implicitly convert type 'object' to 'byte[]'. An explicit conversion exists (are you missing a cast?)
                //         byte[] array = nullValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullValue").WithArguments("object", "byte[]").WithLocation(9, 24),
                // (10,27): error CS0266: Cannot implicitly convert type 'object' to 'System.Span<byte>'. An explicit conversion exists (are you missing a cast?)
                //         Span<byte> span = nullValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullValue").WithArguments("object", "System.Span<byte>").WithLocation(10, 27),
                // (11,43): error CS0266: Cannot implicitly convert type 'object' to 'System.ReadOnlySpan<byte>'. An explicit conversion exists (are you missing a cast?)
                //         ReadOnlySpan<byte> readonlySpan = nullValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nullValue").WithArguments("object", "System.ReadOnlySpan<byte>").WithLocation(11, 43)
                );
        }

        [Fact]
        public void VariableIsNotUsed()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {


        var x1 = ""123""U8;
        ReadOnlySpan<byte> x3 = ""125""U8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (9,13): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         var x1 = "123"U8;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(9, 13),
                // (10,28): warning CS0219: The variable 'x3' is assigned but its value is never used
                //         ReadOnlySpan<byte> x3 = "125"U8;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x3").WithArguments("x3").WithLocation(10, 28)
                );
        }

        [Fact]
        public void NotConstant_01()
        {
            var source = @"#pragma warning disable CS0219 // The variable 'y' is assigned but its value is never used
using System;
class C
{
    const ReadOnlySpan<byte> x = ""07""U8;

    static void Main()
    {
        const ReadOnlySpan<byte> y = ""08""U8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (5,11): error CS8345: Field or auto-implemented property cannot be of type 'ReadOnlySpan<byte>' unless it is an instance member of a ref struct.
                //     const ReadOnlySpan<byte> x = "07"U8;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "ReadOnlySpan<byte>").WithArguments("System.ReadOnlySpan<byte>").WithLocation(5, 11),
                // (5,34): error CS0133: The expression being assigned to 'C.x' must be constant
                //     const ReadOnlySpan<byte> x = "07"U8;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, @"""07""U8").WithArguments("C.x").WithLocation(5, 34),
                // (9,15): error CS0283: The type 'ReadOnlySpan<byte>' cannot be declared const
                //         const ReadOnlySpan<byte> y = "08"U8;
                Diagnostic(ErrorCode.ERR_BadConstType, "ReadOnlySpan<byte>").WithArguments("System.ReadOnlySpan<byte>").WithLocation(9, 15)
                );
        }

        [Fact]
        public void DefaultParameterValues_01()
        {
            var source = @"
using System;
class C
{






    static void M05(byte[] x = ""05""u8){}
    static void M06(Span<byte> x = ""06""u8){}
    static void M07(ReadOnlySpan<byte> x = ""07""U8){}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (11,32): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     static void M05(byte[] x = "05"u8){}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"""05""u8").WithArguments("x").WithLocation(11, 32),
                // (12,36): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     static void M06(Span<byte> x = "06"u8){}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"""06""u8").WithArguments("x").WithLocation(12, 36),
                // (13,44): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     static void M07(ReadOnlySpan<byte> x = "07"U8){}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"""07""U8").WithArguments("x").WithLocation(13, 44)
                );
        }

        [Fact]
        public void DefaultParameterValues_02()
        {
            var source = @"
using System;
class C
{






    static void M05(byte[] x = (byte[])""05""u8){}
    static void M06(Span<byte> x = (Span<byte>)""06""u8){}
    static void M07(ReadOnlySpan<byte> x = (ReadOnlySpan<byte>)""07""U8){}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (11,32): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'byte[]'
                //     static void M05(byte[] x = (byte[])"05"u8){}
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(byte[])""05""u8").WithArguments("System.ReadOnlySpan<byte>", "byte[]").WithLocation(11, 32),
                // (12,36): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'System.Span<byte>'
                //     static void M06(Span<byte> x = (Span<byte>)"06"u8){}
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(Span<byte>)""06""u8").WithArguments("System.ReadOnlySpan<byte>", "System.Span<byte>").WithLocation(12, 36),
                // (13,44): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     static void M07(ReadOnlySpan<byte> x = (ReadOnlySpan<byte>)"07"U8){}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"(ReadOnlySpan<byte>)""07""U8").WithArguments("x").WithLocation(13, 44)
                );
        }

        [Fact]
        public void CallerMemberName_01()
        {
            var source = @"
using System;
class C
{
    void Test1([System.Runtime.CompilerServices.CallerMemberName] byte[] x = default)
    {
    }
    void Test2([System.Runtime.CompilerServices.CallerMemberName] Span<byte> x = default)
    {
    }
    void Test3([System.Runtime.CompilerServices.CallerMemberName] ReadOnlySpan<byte> x = default)
    {
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (5,17): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'byte[]'
                //     void Test1([System.Runtime.CompilerServices.CallerMemberName] byte[] x = default)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "System.Runtime.CompilerServices.CallerMemberName").WithArguments("string", "byte[]").WithLocation(5, 17),
                // (8,17): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'Span<byte>'
                //     void Test2([System.Runtime.CompilerServices.CallerMemberName] Span<byte> x = default)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "System.Runtime.CompilerServices.CallerMemberName").WithArguments("string", "System.Span<byte>").WithLocation(8, 17),
                // (11,17): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'ReadOnlySpan<byte>'
                //     void Test3([System.Runtime.CompilerServices.CallerMemberName] ReadOnlySpan<byte> x = default)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "System.Runtime.CompilerServices.CallerMemberName").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(11, 17)
                );
        }

        [Fact]
        public void AttributeArgument_01()
        {
            var source = @"

class C
{








    [Utf8(""3""U8)]
    static void M03(){}







    [Utf8((byte[])""3""U8)]
    static void M06(){}
}

public class Utf8Attribute : System.Attribute
{
    public Utf8Attribute(byte[] x) {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (13,11): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<byte>' to 'byte[]'
                //     [Utf8("3"U8)]
                Diagnostic(ErrorCode.ERR_BadArgType, @"""3""U8").WithArguments("1", "System.ReadOnlySpan<byte>", "byte[]").WithLocation(13, 11),
                // (22,11): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'byte[]'
                //     [Utf8((byte[])"3"U8)]
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(byte[])""3""U8").WithArguments("System.ReadOnlySpan<byte>", "byte[]").WithLocation(22, 11)
                );
        }

        [Fact]
        public void AttributeArgument_02()
        {
            var source = @"
using System;
class C
{








    [Utf8(""3""U8)]
    static void M03(){}







    [Utf8((Span<byte>)""3""U8)]
    static void M06(){}
}

public class Utf8Attribute : System.Attribute
{
    public Utf8Attribute(Span<byte> x) {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (13,11): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<byte>' to 'System.Span<byte>'
                //     [Utf8("3"U8)]
                Diagnostic(ErrorCode.ERR_BadArgType, @"""3""U8").WithArguments("1", "System.ReadOnlySpan<byte>", "System.Span<byte>").WithLocation(13, 11),
                // (22,6): error CS0181: Attribute constructor parameter 'x' has type 'Span<byte>', which is not a valid attribute parameter type
                //     [Utf8((Span<byte>)"3"U8)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Utf8").WithArguments("x", "System.Span<byte>").WithLocation(22, 6),
                // (22,11): error CS0030: Cannot convert type 'System.ReadOnlySpan<byte>' to 'System.Span<byte>'
                //     [Utf8((Span<byte>)"3"U8)]
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(Span<byte>)""3""U8").WithArguments("System.ReadOnlySpan<byte>", "System.Span<byte>").WithLocation(22, 11)
                );
        }

        [Fact]
        public void AttributeArgument_03()
        {
            var source = @"
using System;
class C
{








    [Utf8(""3""U8)]
    static void M03(){}







    [Utf8((ReadOnlySpan<byte>)""3""U8)]
    static void M06(){}
}

public class Utf8Attribute : System.Attribute
{
    public Utf8Attribute(ReadOnlySpan<byte> x) {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (13,6): error CS0181: Attribute constructor parameter 'x' has type 'ReadOnlySpan<byte>', which is not a valid attribute parameter type
                //     [Utf8("3"U8)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Utf8").WithArguments("x", "System.ReadOnlySpan<byte>").WithLocation(13, 6),
                // (22,6): error CS0181: Attribute constructor parameter 'x' has type 'ReadOnlySpan<byte>', which is not a valid attribute parameter type
                //     [Utf8((ReadOnlySpan<byte>)"3"U8)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Utf8").WithArguments("x", "System.ReadOnlySpan<byte>").WithLocation(22, 6)
                );
        }

        [Fact]
        public void NoConversionFromNonConstant_01()
        {
            var source = @"
using System;
class C
{
    void Test(string s)
    {
        byte[] x = s;
        Span<byte> y = s;
        ReadOnlySpan<byte> z = s;
        x = (byte[])s;
        y = (Span<byte>)s;
        z = (ReadOnlySpan<byte>)s;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (7,20): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         byte[] x = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "byte[]").WithLocation(7, 20),
                // (8,24): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //         Span<byte> y = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.Span<byte>").WithLocation(8, 24),
                // (9,32): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         ReadOnlySpan<byte> z = s;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(9, 32),
                // (10,13): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         x = (byte[])s;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(byte[])s").WithArguments("string", "byte[]").WithLocation(10, 13),
                // (11,13): error CS0030: Cannot convert type 'string' to 'System.Span<byte>'
                //         y = (Span<byte>)s;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<byte>)s").WithArguments("string", "System.Span<byte>").WithLocation(11, 13),
                // (12,13): error CS0030: Cannot convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         z = (ReadOnlySpan<byte>)s;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<byte>)s").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(12, 13)
                );
        }

        [Fact]
        public void NoConversionFromNonConstant_02()
        {
            var source = @"
using System;
class C
{
    void Test(string s)
    {
        byte[] x = $""1{s}"";
        Span<byte> y = $""2{s}"";
        ReadOnlySpan<byte> z = $""3{s}"";
        x = (byte[])$""4{s}"";
        y = (Span<byte>)$""5{s}"";
        z = (ReadOnlySpan<byte>)$""6{s}"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (7,20): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         byte[] x = $"1{s}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"$""1{s}""").WithArguments("string", "byte[]").WithLocation(7, 20),
                // (8,24): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //         Span<byte> y = $"2{s}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"$""2{s}""").WithArguments("string", "System.Span<byte>").WithLocation(8, 24),
                // (9,32): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         ReadOnlySpan<byte> z = $"3{s}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"$""3{s}""").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(9, 32),
                // (10,13): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         x = (byte[])$"4{s}";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(byte[])$""4{s}""").WithArguments("string", "byte[]").WithLocation(10, 13),
                // (11,13): error CS0030: Cannot convert type 'string' to 'System.Span<byte>'
                //         y = (Span<byte>)$"5{s}";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(Span<byte>)$""5{s}""").WithArguments("string", "System.Span<byte>").WithLocation(11, 13),
                // (12,13): error CS0030: Cannot convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         z = (ReadOnlySpan<byte>)$"6{s}";
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(ReadOnlySpan<byte>)$""6{s}""").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(12, 13)
                );
        }

        [Fact]
        public void PatternMatching_01()
        {
            var source = @"

class C
{
    void Test(byte[] s)
    {
        _ = s is ""1"";
        _ = s is ""2""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (7,18): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         _ = s is "1";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""1""").WithArguments("string", "byte[]").WithLocation(7, 18),
                // (8,18): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'byte[]'
                //         _ = s is "2"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""2""u8").WithArguments("System.ReadOnlySpan<byte>", "byte[]").WithLocation(8, 18)
                );
        }

        [Fact]
        public void PatternMatching_02()
        {
            var source = @"
using System;
class C
{
    void Test(Span<byte> s)
    {
        _ = s is ""1"";
        _ = s is ""2""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (7,18): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //         _ = s is "1";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""1""").WithArguments("string", "System.Span<byte>").WithLocation(7, 18),
                // (8,18): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'System.Span<byte>'
                //         _ = s is "2"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""2""u8").WithArguments("System.ReadOnlySpan<byte>", "System.Span<byte>").WithLocation(8, 18)
                );
        }

        [Fact]
        public void PatternMatching_03()
        {
            var source = @"
using System;
class C
{
    void Test(ReadOnlySpan<byte> s)
    {
        _ = s is ""1"";
        _ = s is ""2""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
                // (7,18): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         _ = s is "1";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""1""").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(7, 18),
                // (8,18): error CS9133: A constant value of type 'ReadOnlySpan<byte>' is expected
                //         _ = s is "2"u8;
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"""2""u8").WithArguments("System.ReadOnlySpan<byte>").WithLocation(8, 18)
                );
        }

        [Fact]
        public void SemanticModel_01()
        {
            var source = @"
using System;
class C
{
    byte[] Test()
    {
        return ""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Byte[]", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.False(model.GetConversion(node).Exists);
        }

        [Fact]
        public void SemanticModel_02()
        {
            var source = @"
using System;
class C
{
    byte[] Test()
    {
        return (byte[])""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.Byte[]", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Byte[]", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsIdentity);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_03()
        {
            var source = @"
using System;
class C
{
    Span<byte> Test()
    {
        return ""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Byte>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.False(model.GetConversion(node).Exists);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_04()
        {
            var source = @"
using System;
class C
{
    Span<byte> Test()
    {
        return (Span<byte>)""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.Span<System.Byte>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Byte>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsIdentity);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_05()
        {
            var source = @"
using System;
class C
{
    ReadOnlySpan<byte> Test()
    {
        return ""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.False(model.GetConversion(node).Exists);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_06()
        {
            var source = @"
using System;
class C
{
    ReadOnlySpan<byte> Test()
    {
        return (ReadOnlySpan<byte>)""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsIdentity);
        }

        [Fact]
        public void SemanticModel_07()
        {
            var source = @"
using System;
class C
{
    byte[] Test()
    {
        return ""1""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Byte[]", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.False(model.GetConversion(node).Exists);
        }

        [Fact]
        public void SemanticModel_08()
        {
            var source = @"
using System;
class C
{
    byte[] Test()
    {
        return (byte[])""1""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.Byte[]", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Byte[]", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsIdentity);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_09()
        {
            var source = @"
using System;
class C
{
    Span<byte> Test()
    {
        return ""1""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Byte>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.False(model.GetConversion(node).Exists);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_11()
        {
            var source = @"
using System;
class C
{
    ReadOnlySpan<byte> Test()
    {
        return ""1""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsIdentity);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_12()
        {
            var source = @"
using System;
class C
{
    ReadOnlySpan<byte> Test()
    {
        return (ReadOnlySpan<byte>)""1""u8;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Byte>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsIdentity);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_13()
        {
            var source = @"
using System;
class C
{
    ReadOnlySpan<char> Test()
    {
        return ""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Char> System.String.op_Implicit(System.String? value)", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Char>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsUserDefined);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel_14()
        {
            var source = @"
using System;
class C
{
    ReadOnlySpan<char> Test()
    {
        return (ReadOnlySpan<char>)""1"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Char> System.String.op_Implicit(System.String? value)", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.ReadOnlySpan<System.Char>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Char>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(node).IsIdentity);
        }

        [Theory]
        [InlineData(@"""cat""u8")]
        [InlineData(@"""c""u8 + ""at""u8")]
        [InlineData(@"""c""u8 + ""a""u8 + ""t""u8")]
        public void NullTerminate_01(string expression)
        {
            var source = @"
using System;
class C
{
    static ReadOnlySpan<byte> Test3() => " + expression + @";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: Verification.Fails with { ILVerifyMessage = """
[Test3]: Cannot change initonly field outside its .ctor. { Offset = 0x0 }
[Test3]: Unexpected type on the stack. { Offset = 0x6, Found = address of Int32, Expected = Native Int }
[Test3]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xb }
""" });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C.Test3", """
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    "int <PrivateImplementationDetails>.F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57"
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     "System.ReadOnlySpan<byte>..ctor(void*, int)"
  IL_000b:  ret
}
""");

            string blobId = ExecutionConditionUtil.IsWindows ?
                "I_000026F8" :
                "I_00002738";

            verifier.VerifyTypeIL("<PrivateImplementationDetails>", @"
.class private auto ansi sealed '<PrivateImplementationDetails>'
	extends [System.Runtime]System.Object
{
	.custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Fields
	.field assembly static initonly int32 F3D4280708A6C4BEA1BAEB5AD5A4B659E705A90BDD448840276EA20CB151BE57 at " + blobId + @"
	.data cil " + blobId + @" = bytearray (
		63 61 74 00
	)
} // end of class <PrivateImplementationDetails>
");
        }

        [Theory]
        [InlineData(@"""""u8")]
        [InlineData(@"""""u8 + """"u8")]
        [InlineData(@"""""u8 + """"u8 + """"u8")]
        public void NullTerminate_02(string expression)
        {
            var source = @"
using System;
class C
{
    static ReadOnlySpan<byte> Test3() => " + expression + @";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""byte <PrivateImplementationDetails>.6E340B9CFFB37A989CA544E6BB780A2C78901D3FB33738768511A30617AFA01D""
  IL_0005:  ldc.i4.0
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");
            string blobId = ExecutionConditionUtil.IsWindows ?
                "I_000026F8" :
                "I_00002738";

            verifier.VerifyTypeIL("<PrivateImplementationDetails>", @"
.class private auto ansi sealed '<PrivateImplementationDetails>'
	extends [System.Runtime]System.Object
{
	.custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Fields
	.field assembly static initonly uint8 '6E340B9CFFB37A989CA544E6BB780A2C78901D3FB33738768511A30617AFA01D' at " + blobId + @"
	.data cil " + blobId + @" = bytearray ( 00
	)
} // end of class <PrivateImplementationDetails>
");
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData(0, -1, "ldc.i4.0", "ldc.i4.m1")]
        [InlineData(0, 4, "ldc.i4.0", "ldc.i4.4")]
        [InlineData(-1, -1, "ldc.i4.m1", "ldc.i4.m1")]
        [InlineData(-1, 2, "ldc.i4.m1", "ldc.i4.2")]
        [InlineData(-1, 4, "ldc.i4.m1", "ldc.i4.4")]
        public void System_ReadOnlySpan_T__ctor_Array_Start_Length_ExplicitUsage_01(int start, int length, string startOpCode, string lengthOpCode)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        try
        {
            Test3();
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.Write(""ArgumentOutOfRangeException"");
        }
    }

    static ReadOnlySpan<byte> Test3()
    {
        return new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }, " + start + ", " + length + @");
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"ArgumentOutOfRangeException", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  .locals init (System.ReadOnlySpan<byte> V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""byte""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  " + startOpCode + @"
  IL_0013:  " + lengthOpCode + @"
  IL_0014:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[], int, int)""
  IL_0019:  stloc.0
  IL_001a:  br.s       IL_001c
  IL_001c:  ldloc.0
  IL_001d:  ret
}
");
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData(0, "{ }")]
        [InlineData(1, "{ 0x1 }")]
        [InlineData(2, "{ 0x1 0x2 }")]
        public void System_ReadOnlySpan_T__ctor_Array_Start_Length_ExplicitUsage_02(int length, string expected)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3()
    {
        return new ReadOnlySpan<byte>(new byte[] { 1, 2 }, 0, " + length + @");
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
" + expected + @"
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (System.ReadOnlySpan<byte> V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""short <PrivateImplementationDetails>.A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222""
  IL_0006:  ldc.i4." + length + @"
  IL_0007:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000c:  stloc.0
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.0
  IL_0010:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void System_ReadOnlySpan_T__ctor_Array_Start_Length_ExplicitUsage_03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3()
    {
        return new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }, 1, 1);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x2 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  .locals init (System.ReadOnlySpan<byte> V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""byte""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.1
  IL_0014:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[], int, int)""
  IL_0019:  stloc.0
  IL_001a:  br.s       IL_001c
  IL_001c:  ldloc.0
  IL_001d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void System_ReadOnlySpan_T__ctor_Array_Start_Length_ExplicitUsage_04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3()
    {
        return new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }, Start, 2);
    }

    static int Start = 0;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x1 0x2 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (System.ReadOnlySpan<byte> V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""byte""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  ldsfld     ""int C.Start""
  IL_0017:  ldc.i4.2
  IL_0018:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[], int, int)""
  IL_001d:  stloc.0
  IL_001e:  br.s       IL_0020
  IL_0020:  ldloc.0
  IL_0021:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void System_ReadOnlySpan_T__ctor_Array_Start_Length_ExplicitUsage_05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3()
    {
        return new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }, 0, Length);
    }

    static int Length = 3;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x1 0x2 0x3 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (System.ReadOnlySpan<byte> V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""byte""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  ldc.i4.0
  IL_0013:  ldsfld     ""int C.Length""
  IL_0018:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[], int, int)""
  IL_001d:  stloc.0
  IL_001e:  br.s       IL_0020
  IL_0020:  ldloc.0
  IL_0021:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void System_ReadOnlySpan_T__ctor_Array_Start_Length_ExplicitUsage_06()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3()
    {
        return new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }, Start, Length);
    }

    static int Start = 0;
    static int Length = 1;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x1 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (System.ReadOnlySpan<byte> V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""byte""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  ldsfld     ""int C.Start""
  IL_0017:  ldsfld     ""int C.Length""
  IL_001c:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[], int, int)""
  IL_0021:  stloc.0
  IL_0022:  br.s       IL_0024
  IL_0024:  ldloc.0
  IL_0025:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void System_ReadOnlySpan_T__ctor_Array_Start_Length_ExplicitUsage_07()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3()
    {
        return new ReadOnlySpan<byte>(new byte[] { }, 0, 0);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (System.ReadOnlySpan<byte> V_0,
                System.ReadOnlySpan<byte> V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""System.ReadOnlySpan<byte>""
  IL_0009:  ldloc.0
  IL_000a:  stloc.1
  IL_000b:  br.s       IL_000d
  IL_000d:  ldloc.1
  IL_000e:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void PassAround_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Helpers.Print(Test2());
    }

    static ReadOnlySpan<byte> Test2() => Test3(""cat""u8);

    static ReadOnlySpan<byte> Test3(ReadOnlySpan<byte> x) => x;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x63 0x61 0x74 }
", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void PassAround_02()
        {
            var source = @"using System;

class C
{
    static ref readonly ReadOnlySpan<byte> Test2()
    {
        return ref Test3(""cat""u8);
    }

    static ref readonly ReadOnlySpan<byte> Test3(in ReadOnlySpan<byte> x) => ref x;
}
";
            var comp = CreateCompilation(new[] { source + HelpersSource, UnscopedRefAttributeDefinition }, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            comp.VerifyDiagnostics(
                // (7,20): error CS8347: Cannot use a result of 'C.Test3(in ReadOnlySpan<byte>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref Test3("cat"u8);
                Diagnostic(ErrorCode.ERR_EscapeCall, @"Test3(""cat""u8)").WithArguments("C.Test3(in System.ReadOnlySpan<byte>)", "x").WithLocation(7, 20),
                // (7,26): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref Test3("cat"u8);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, @"""cat""u8").WithLocation(7, 26)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConcatenation_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        _ = new C() + ReadOnlySpan<byte>.Empty;
    }

    public static C operator +(C x, ReadOnlySpan<byte> y)
    {
        System.Console.WriteLine(""called"");
        return x;
    }

    public static implicit operator ReadOnlySpan<byte>(C x) => ReadOnlySpan<byte>.Empty;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"called", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConcatenation_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        _ = new C() + ""a""u8;
    }

    public static C operator +(C x, ReadOnlySpan<byte> y)
    {
        System.Console.WriteLine(""called"");
        return x;
    }

    public static implicit operator ReadOnlySpan<byte>(C x) => ReadOnlySpan<byte>.Empty;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"called", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void UserDefinedConcatenation_03()
        {
            var source = @"
using System;
public class C
{
    static void Main()
    {
        _ = new C() + ReadOnlySpan<byte>.Empty;
    }

    public static implicit operator ReadOnlySpan<byte>(C x) => ReadOnlySpan<byte>.Empty;
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        private readonly T[] arr;

        public ref readonly T this[int i] => ref arr[i];
        public override int GetHashCode() => 2;
        public int Length { get; }

        public ReadOnlySpan(T[] arr)
        {
            this.arr = arr;
            this.Length = arr.Length;
        }

        public static ReadOnlySpan<T> Empty => default;

        public static C operator +(C x, ReadOnlySpan<T> y)
        {
            System.Console.WriteLine(""called"");
            return x;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"called", verify: Verification.Fails).Diagnostics.Where(d => d.Code is not (int)ErrorCode.WRN_SameFullNameThisAggAgg).Verify();
        }

        [Fact]
        public void UserDefinedConcatenation_04()
        {
            var source = @"
using System;
public class C
{
    static void Main()
    {
        _ = new C() + ""a""u8;
    }

    public static implicit operator ReadOnlySpan<byte>(C x) => ReadOnlySpan<byte>.Empty;
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        private readonly T[] arr;

        public ref readonly T this[int i] => ref arr[i];
        public override int GetHashCode() => 2;
        public int Length { get; }

        public ReadOnlySpan(T[] arr, int start, int length)
        {
            this.arr = arr;
            this.Length = arr.Length;
        }

        public static ReadOnlySpan<T> Empty => default;

        public static C operator +(C x, ReadOnlySpan<T> y)
        {
            System.Console.WriteLine(""called"");
            return x;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"called", verify: Verification.Fails).Diagnostics.Where(d => d.Code is not (int)ErrorCode.WRN_SameFullNameThisAggAgg).Verify();
        }

        [Fact]
        public void UserDefinedConcatenation_05()
        {
            var source = @"
using System;
public class C
{
    static void Main()
    {
        _ = ReadOnlySpan<byte>.Empty + ReadOnlySpan<byte>.Empty;
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        private readonly T[] arr;

        public ref readonly T this[int i] => ref arr[i];
        public override int GetHashCode() => 2;
        public int Length { get; }

        public ReadOnlySpan(T[] arr)
        {
            this.arr = arr;
            this.Length = arr.Length;
        }

        public static ReadOnlySpan<T> Empty => default;

        public static ReadOnlySpan<T> operator +(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
        {
            System.Console.WriteLine(""called"");
            return x;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"called", verify: Verification.Fails).Diagnostics.Where(d => d.Code is not (int)ErrorCode.WRN_SameFullNameThisAggAgg).Verify();
        }

        [Fact]
        public void UserDefinedConcatenation_06()
        {
            var source = @"
public class C
{
    static void Main()
    {
        _ = ""a""u8 + ""b""u8;
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        private readonly T[] arr;

        public ref readonly T this[int i] => ref arr[i];
        public override int GetHashCode() => 2;
        public int Length { get; }

        public ReadOnlySpan(T[] arr, int start, int length)
        {
            this.arr = arr;
            this.Length = arr.Length;
        }

        public static ReadOnlySpan<T> Empty => default;

        public static ReadOnlySpan<T> operator +(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
        {
            System.Console.WriteLine(""called"");
            return x;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"called", verify: Verification.Fails).Diagnostics.Where(d => d.Code is not (int)ErrorCode.WRN_SameFullNameThisAggAgg).Verify();
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData(@"""12""u8 + ""34""u8")]
        [InlineData(@"""12""u8 + ""3""u8 + ""4""u8")]
        [InlineData(@"""12""u8 + (""3""u8 + ""4""u8)")]
        [InlineData(@"""1""u8 + ""2""u8 + ""3""u8 + ""4""u8")]
        [InlineData(@"""1""u8 + ""2""u8 + (""3""u8 + ""4""u8)")]
        [InlineData(@"""1""u8 + (""2""u8 + ""3""u8 + ""4""u8)")]
        [InlineData(@"""1""u8 + (""2""u8 + (""3""u8 + ""4""u8))")]
        [InlineData(@"""1""u8 + checked(""2""u8 + unchecked(""3""u8 + ""4""u8))")]
        public void Concatenation_01(string expression)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3() => " + expression + @";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x31 0x32 0x33 0x34 }
", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
    // Code size       12 (0xc)
    .maxstack  2
    IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.94E7DD2D7CA2487FF2B04D7CBB490139BB9A2B5CE798348F02CA0A29ABD4EFF1""
    IL_0005:  ldc.i4.4
    IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
    IL_000b:  ret
}
");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            foreach (var node in tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(b => b.IsKind(SyntaxKind.AddExpression)))
            {
                var method = (IMethodSymbol)model.GetSymbolInfo(node).Symbol;
                Assert.Equal("System.ReadOnlySpan<System.Byte> System.ReadOnlySpan<System.Byte>.op_Addition(System.ReadOnlySpan<System.Byte> left, System.ReadOnlySpan<System.Byte> right)", method.ToTestDisplayString());
                Assert.True(method.IsImplicitlyDeclared);
                Assert.Equal(MethodKind.BuiltinOperator, method.MethodKind);

                var synthesizedMethod = comp.CreateBuiltinOperator(
                    method.Name, method.ReturnType, method.Parameters[0].Type, method.Parameters[1].Type);
                Assert.Equal(synthesizedMethod, method);
            }
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData(@"(""1""u8 + ""2""u8 + (""3""u8 + ""4""u8)) + new C()")]
        [InlineData(@"(""1""u8 + ""2""u8 + (""3""u8 + ""4""u8)) + new C() + new C()")]
        [InlineData(@"new C() + (""1""u8 + ""2""u8 + (""3""u8 + ""4""u8))")]
        [InlineData(@"new C() + (""1""u8 + ""2""u8 + (""3""u8 + ""4""u8)) + new C()")]
        [InlineData(@"new C() + new C() + (""1""u8 + ""2""u8 + (""3""u8 + ""4""u8))")]
        [InlineData(@"new C() + ((""1""u8 + ""2""u8 + (""3""u8 + ""4""u8)) + new C())")]
        [InlineData(@"new C() + (new C() + (""1""u8 + ""2""u8 + (""3""u8 + ""4""u8)))")]
        public void Concatenation_02(string expression)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        _ = " + expression + @";
    }

    public static C operator +(ReadOnlySpan<byte> x, C y)
    {
        Helpers.Print(x);
        return y;
    }

    public static C operator +(C x, ReadOnlySpan<byte> y)
    {
        Helpers.Print(y);
        return x;
    }

    public static C operator +(C x, C y)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x31 0x32 0x33 0x34 }
", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Theory]
        [InlineData(@"ReadOnlySpan<byte>.Empty + ReadOnlySpan<byte>.Empty")]
        [InlineData(@"""a""u8 + ReadOnlySpan<byte>.Empty")]
        [InlineData(@"ReadOnlySpan<byte>.Empty + ""b""u8")]
        public void Concatenation_03(string expression)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        _ = " + expression + @";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,13): error CS9047: Operator '+' cannot be applied to operands of type 'ReadOnlySpan<byte>' and 'ReadOnlySpan<byte>' that are not UTF-8 byte representations
                //         _ = ReadOnlySpan<byte>.Empty + ReadOnlySpan<byte>.Empty;
                Diagnostic(ErrorCode.ERR_BadBinaryReadOnlySpanConcatenation, expression).WithArguments("+", "System.ReadOnlySpan<byte>", "System.ReadOnlySpan<byte>").WithLocation(7, 13)
                );
        }

        [Theory]
        [InlineData(@"ReadOnlySpan<byte>.Empty")]
        [InlineData(@"""b""u8")]
        public void Concatenation_04(string expression)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = ReadOnlySpan<byte>.Empty;
        var y = ""a""u8;

        x += " + expression + @";
        y += " + expression + @";
        ""c""u8 += " + expression + @";
        x++;
        y++;
        ""d""u8++;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,9): error CS0019: Operator '+=' cannot be applied to operands of type 'ReadOnlySpan<byte>' and 'ReadOnlySpan<byte>'
                //         x += "b"u8;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"x += " + expression).WithArguments("+=", "System.ReadOnlySpan<byte>", "System.ReadOnlySpan<byte>").WithLocation(10, 9),
                // (11,9): error CS0019: Operator '+=' cannot be applied to operands of type 'ReadOnlySpan<byte>' and 'ReadOnlySpan<byte>'
                //         y += "b"u8;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"y += " + expression).WithArguments("+=", "System.ReadOnlySpan<byte>", "System.ReadOnlySpan<byte>").WithLocation(11, 9),
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         "c"u8 += "b"u8;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"""c""u8").WithLocation(12, 9),
                // (13,9): error CS0023: Operator '++' cannot be applied to operand of type 'ReadOnlySpan<byte>'
                //         x++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "x++").WithArguments("++", "System.ReadOnlySpan<byte>").WithLocation(13, 9),
                // (14,9): error CS0023: Operator '++' cannot be applied to operand of type 'ReadOnlySpan<byte>'
                //         y++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "y++").WithArguments("++", "System.ReadOnlySpan<byte>").WithLocation(14, 9),
                // (15,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         "d"u8++;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, @"""d""u8").WithLocation(15, 9)
                );
        }

        [Theory]
        [InlineData(@"ReadOnlySpan<byte>.Empty - ReadOnlySpan<byte>.Empty")]
        [InlineData(@"""a""u8 - ReadOnlySpan<byte>.Empty")]
        [InlineData(@"ReadOnlySpan<byte>.Empty - ""b""u8")]
        public void Subtraction_01(string expression)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        _ = " + expression + @";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,13): error CS0019: Operator '-' cannot be applied to operands of type 'ReadOnlySpan<byte>' and 'ReadOnlySpan<byte>'
                //         _ = ReadOnlySpan<byte>.Empty - ReadOnlySpan<byte>.Empty;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, expression).WithArguments("-", "System.ReadOnlySpan<byte>", "System.ReadOnlySpan<byte>").WithLocation(7, 13)
                );
        }

        [Theory]
        [InlineData(@"new C() + ReadOnlySpan<byte>.Empty")]
        [InlineData(@"new C() + ""b""u8")]
        public void Concatenation_05(string expression)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        _ = " + expression + @";
    }

    public static implicit operator ReadOnlySpan<byte>(C x) => ReadOnlySpan<byte>.Empty;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'ReadOnlySpan<byte>'
                //         _ = new C() + ReadOnlySpan<byte>.Empty;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, expression).WithArguments("+", "C", "System.ReadOnlySpan<byte>").WithLocation(7, 13)
                );
        }

        [Theory]
        [InlineData(@"ReadOnlySpan<byte>.Empty + new C()")]
        [InlineData(@"""b""u8 + new C()")]
        public void Concatenation_06(string expression)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        _ = " + expression + @";
    }

    public static implicit operator ReadOnlySpan<byte>(C x) => ReadOnlySpan<byte>.Empty;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,13): error CS0019: Operator '+' cannot be applied to operands of type 'ReadOnlySpan<byte>' and 'C'
                //         _ = "b"u8 + new C();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, expression).WithArguments("+", "System.ReadOnlySpan<byte>", "C").WithLocation(7, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly), typeof(NoIOperationValidation)), WorkItem(62361, "https://github.com/dotnet/roslyn/issues/62361")]
        public void DeeplyNestedConcatenation()
        {
            var longConcat = new StringBuilder();
            for (int i = 0; i < 800; i++)
            {
                longConcat.Append(""" "a"u8 + """);
            }

            var source = $$"""
System.Console.Write(X.Y.Length);

class X
{
    public static System.ReadOnlySpan<byte> Y => {{longConcat}} "a"u8;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            CompileAndVerify(comp, expectedOutput: "801", verify: Verification.Fails).VerifyDiagnostics();
        }
    }
}
