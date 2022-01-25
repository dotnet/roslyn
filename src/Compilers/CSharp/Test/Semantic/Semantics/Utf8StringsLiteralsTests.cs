// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public static void Print(byte[] span)
    {
        System.Console.Write(""{"");
        foreach (var item in span)
        {
            System.Console.Write("" 0x{0:X}"", item);
        }
        System.Console.WriteLine("" }"");
    }

    public static void Print(Span<byte> span)
    {
        System.Console.Write(""{"");
        foreach (var item in span)
        {
            System.Console.Write("" 0x{0:X}"", item);
        }
        System.Console.WriteLine("" }"");
    }

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

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ImplicitConversions_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test1());
        Helpers.Print(Test2());
        Helpers.Print(Test3());
    }

    static byte[] Test1() => ""hello"";
    static Span<byte> Test2() => ""dog"";
    static ReadOnlySpan<byte> Test3() => ""cat"";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            verifier.VerifyIL("C.Test1()", @"
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.CD6357EFDD966DE8C0CB2F876CC89EC74CE35F0968E11743987084BD42FB8944""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<byte>..ctor(byte[])""
  IL_0016:  ret
}
");

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.77AF778B51ABD4A3C51C5DDD97204A9C3AE614EBCCB75A606C3B6865AED6744E""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (13,30): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static byte[] Test1() => "hello";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(13, 30),
                // (14,34): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static Span<byte> Test2() => "dog";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(14, 34),
                // (15,42): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static ReadOnlySpan<byte> Test3() => "cat";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""cat""").WithArguments("Utf8 String Literals").WithLocation(15, 42)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ImplicitConversions_TupleLiteral_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        (byte[] b, (byte[] d, string e) c) a = (""hello"", (""dog"", ""cat""));
        System.Console.WriteLine();
        Helpers.Print(a.b);
        Helpers.Print(a.c.d);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,49): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         (byte[] b, (byte[] d, string e) c) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(7, 49),
                // (7,59): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         (byte[] b, (byte[] d, string e) c) a = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(7, 59)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ImplicitConversions_Deconstruction_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        (byte[] a, (byte[] b, string c)) = (""hello"", (""dog"", ""cat""));
        System.Console.WriteLine();
        Helpers.Print(a);
        Helpers.Print(b);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,45): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         (byte[] a, (byte[] b, string c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(7, 45),
                // (7,55): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         (byte[] a, (byte[] b, string c)) = ("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(7, 55)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

        System.Console.WriteLine();
        Helpers.Print(array);
        Helpers.Print(span);
        Helpers.Print(readonlySpan);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,21): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var array = (byte[])"hello";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"(byte[])""hello""").WithArguments("Utf8 String Literals").WithLocation(7, 21),
                // (8,20): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var span = (Span<byte>)"dog";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"(Span<byte>)""dog""").WithArguments("Utf8 String Literals").WithLocation(8, 20),
                // (9,28): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var readonlySpan = (ReadOnlySpan<byte>)"cat";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"(ReadOnlySpan<byte>)""cat""").WithArguments("Utf8 String Literals").WithLocation(9, 28)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ExplicitConversions_TupleLiteral_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var a = ((byte[] b, (byte[] d, string e) c))(""hello"", (""dog"", ""cat""));
        System.Console.WriteLine();
        Helpers.Print(a.b);
        Helpers.Print(a.c.d);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,54): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var a = ((byte[] b, (byte[] d, string e) c))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(7, 54),
                // (7,64): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var a = ((byte[] b, (byte[] d, string e) c))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(7, 64)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InvalidContent_01()
        {
            var source = @"

class C
{
    static void Main()
    {
        byte[] array = ""hello \uD801\uD802"";
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (7,24): error CS8983: The input string cannot be converted into the equivalent UTF8 byte representation. Unable to translate Unicode character \\uD801 at index 6 to specified code page.
                //         byte[] array = "hello \uD801\uD802";
                Diagnostic(ErrorCode.ERR_CannotBeConvertedToUTF8, @"""hello \uD801\uD802""").WithArguments(@"Unable to translate Unicode character \\uD801 at index 6 to specified code page.").WithLocation(7, 24)
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
                // (6,13): error CS9100: The input string cannot be converted into the equivalent UTF8 byte representation. Unable to translate Unicode character \\uD801 at index 6 to specified code page.
                //         _ = "hello \uD801\uD802"u8;
                Diagnostic(ErrorCode.ERR_CannotBeConvertedToUTF8, @"""hello \uD801\uD802""u8").WithArguments(@"Unable to translate Unicode character \\uD801 at index 6 to specified code page.").WithLocation(6, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        byte[] array = ""hello"";
        ReadOnlySpan<byte> readonlySpan = ""cat"";

        System.Console.WriteLine();
        Helpers.Print(array);
        Helpers.Print(readonlySpan);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Array);
            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        byte[] array = ""hello"";
        Span<byte> span = ""dog"";

        System.Console.WriteLine();
        Helpers.Print(array);
        Helpers.Print(span);
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);
            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_03()
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
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Array);
            comp.VerifyEmitDiagnostics(
                // (8,27): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //         Span<byte> span = "dog";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""dog""").WithArguments("System.Span`1", ".ctor").WithLocation(8, 27)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_04()
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
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);
            comp.VerifyEmitDiagnostics(
                // (8,43): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1..ctor'
                //         ReadOnlySpan<byte> readonlySpan = "cat";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""cat""").WithArguments("System.ReadOnlySpan`1", ".ctor").WithLocation(8, 43)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Helpers.Print(Test2());
    }

    static Span<byte> Test2() => ""dog"";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Pointer);
            var verifier = CompileAndVerify(comp, expectedOutput: "{ 0x64 0x6F 0x67 }").VerifyDiagnostics();

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.CD6357EFDD966DE8C0CB2F876CC89EC74CE35F0968E11743987084BD42FB8944""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<byte>..ctor(byte[])""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_06()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Helpers.Print(Test3());
    }

    static ReadOnlySpan<byte> Test3() => ""cat"";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer);
            var verifier = CompileAndVerify(comp, expectedOutput: "{ 0x63 0x61 0x74 }").VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.77AF778B51ABD4A3C51C5DDD97204A9C3AE614EBCCB75A606C3B6865AED6744E""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.ReadOnlySpan<byte>..ctor(byte[])""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void NoConversionFromNull_01()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        const string nullValue = null;
        byte[] array = nullValue;
        Span<byte> span = nullValue;
        ReadOnlySpan<byte> readonlySpan = nullValue;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            // PROTOTYPE(UTF8StringLiterals) : More specific error message?
            comp.VerifyEmitDiagnostics(
                // (9,24): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         byte[] array = nullValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nullValue").WithArguments("string", "byte[]").WithLocation(9, 24),
                // (10,27): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<byte>'
                //         Span<byte> span = nullValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nullValue").WithArguments("string", "System.Span<byte>").WithLocation(10, 27),
                // (11,43): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         ReadOnlySpan<byte> readonlySpan = nullValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nullValue").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(11, 43)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void NoConversionFromNull_02()
        {
            var source = @"
#pragma warning disable CS0219 // The variable is assigned but its value is never used
using System;
class C
{
    static void Main()
    {
        const string nullValue = null;
        var array = (byte[])nullValue;
        var span = (Span<byte>)nullValue;
        var readonlySpan = (ReadOnlySpan<byte>)nullValue;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            // PROTOTYPE(UTF8StringLiterals) : More specific error message?
            comp.VerifyEmitDiagnostics(
                // (9,21): error CS0030: Cannot convert type 'string' to 'byte[]'
                //         var array = (byte[])nullValue;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(byte[])nullValue").WithArguments("string", "byte[]").WithLocation(9, 21),
                // (10,20): error CS0030: Cannot convert type 'string' to 'System.Span<byte>'
                //         var span = (Span<byte>)nullValue;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<byte>)nullValue").WithArguments("string", "System.Span<byte>").WithLocation(10, 20),
                // (11,28): error CS0030: Cannot convert type 'string' to 'System.ReadOnlySpan<byte>'
                //         var readonlySpan = (ReadOnlySpan<byte>)nullValue;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<byte>)nullValue").WithArguments("string", "System.ReadOnlySpan<byte>").WithLocation(11, 28)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

        [ConditionalFact(typeof(CoreClrOnly))]
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

        [ConditionalFact(typeof(CoreClrOnly))]
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

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ConstantExpressions_01()
        {
            var source = @"
using System;
class C
{
    const string first = ""\uD83D"";  // high surrogate
    const string second = ""\uDE00""; // low surrogate

    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test1());
        Helpers.Print(Test2());
        Helpers.Print(Test3());
    }

    static byte[] Test1() => first + second;
    static Span<byte> Test2() => first + second;
    static ReadOnlySpan<byte> Test3() => first + second;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0xF0 0x9F 0x98 0x80 }
{ 0xF0 0x9F 0x98 0x80 }
{ 0xF0 0x9F 0x98 0x80 }
").VerifyDiagnostics();

            verifier.VerifyIL("C.Test1()", @"
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""int <PrivateImplementationDetails>.F0443A342C5EF54783A111B51BA56C938E474C32324D90C3A60C9C8E3A37E2D9""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""int <PrivateImplementationDetails>.F0443A342C5EF54783A111B51BA56C938E474C32324D90C3A60C9C8E3A37E2D9""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<byte>..ctor(byte[])""
  IL_0016:  ret
}
");

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""int <PrivateImplementationDetails>.F0443A342C5EF54783A111B51BA56C938E474C32324D90C3A60C9C8E3A37E2D9""
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ConstantExpressions_02()
        {
            var source = @"
using System;
class C
{
    const string second = ""\uDE00""; // low surrogate

    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test1());
        Helpers.Print(Test2());
        Helpers.Print(Test3());
    }

    static byte[] Test1() => $""\uD83D{second}"";
    static Span<byte> Test2() => $""\uD83D{second}"";
    static ReadOnlySpan<byte> Test3() => $""\uD83D{second}"";
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0xF0 0x9F 0x98 0x80 }
{ 0xF0 0x9F 0x98 0x80 }
{ 0xF0 0x9F 0x98 0x80 }
").VerifyDiagnostics();

            verifier.VerifyIL("C.Test1()", @"
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""int <PrivateImplementationDetails>.F0443A342C5EF54783A111B51BA56C938E474C32324D90C3A60C9C8E3A37E2D9""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""int <PrivateImplementationDetails>.F0443A342C5EF54783A111B51BA56C938E474C32324D90C3A60C9C8E3A37E2D9""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<byte>..ctor(byte[])""
  IL_0016:  ret
}
");

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""int <PrivateImplementationDetails>.F0443A342C5EF54783A111B51BA56C938E474C32324D90C3A60C9C8E3A37E2D9""
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            // PROTOTYPE(UTF8StringLiterals) : Confirm this is the behavior we want. Cast is going to work.
            //                                However, it looks like this helps us to avoid breaking an existing code due to
            //                                an overload resolution ambiguity because implicit user defined conversions are not applicable.
            //                                See OverloadResolution_03 unit-test.
            comp.VerifyDiagnostics(
                // (7,16): error CS0266: Cannot implicitly convert type 'string' to 'C1'. An explicit conversion exists (are you missing a cast?)
                //         C1 x = "hello";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""hello""").WithArguments("string", "C1").WithLocation(7, 16),
                // (8,16): error CS0266: Cannot implicitly convert type 'string' to 'C2'. An explicit conversion exists (are you missing a cast?)
                //         C2 y = "dog";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""dog""").WithArguments("string", "C2").WithLocation(8, 16),
                // (9,16): error CS0266: Cannot implicitly convert type 'string' to 'C3'. An explicit conversion exists (are you missing a cast?)
                //         C3 z = "cat";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""cat""").WithArguments("string", "C3").WithLocation(9, 16)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

        [ConditionalFact(typeof(CoreClrOnly))]
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

        [ConditionalFact(typeof(CoreClrOnly))]
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

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,21): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var x = (C1)"hello";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(8, 21),
                // (9,21): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var y = (C2)"dog";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(9, 21),
                // (10,21): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var z = (C3)"cat";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""cat""").WithArguments("Utf8 String Literals").WithLocation(10, 21)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,30): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(8, 30),
                // (8,40): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(8, 40),
                // (8,47): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""cat""").WithArguments("Utf8 String Literals").WithLocation(8, 47)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,21): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var x = (C1)"hello";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(8, 21),
                // (9,21): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var y = (C2)"dog";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(9, 21),
                // (10,21): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var z = (C3)"cat";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""cat""").WithArguments("Utf8 String Literals").WithLocation(10, 21)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,30): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""").WithArguments("Utf8 String Literals").WithLocation(8, 30),
                // (8,40): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""").WithArguments("Utf8 String Literals").WithLocation(8, 40),
                // (8,47): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = ((C1, (C2, C3)))("hello", ("dog", "cat"));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""cat""").WithArguments("Utf8 String Literals").WithLocation(8, 47)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
                // (7,16): error CS0266: Cannot implicitly convert type 'string' to 'C1'. An explicit conversion exists (are you missing a cast?)
                //         C1 x = "hello";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""hello""").WithArguments("string", "C1").WithLocation(7, 16),
                // (8,16): error CS0266: Cannot implicitly convert type 'string' to 'C2'. An explicit conversion exists (are you missing a cast?)
                //         C2 y = "dog";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""dog""").WithArguments("string", "C2").WithLocation(8, 16),
                // (9,16): error CS0266: Cannot implicitly convert type 'string' to 'C3'. An explicit conversion exists (are you missing a cast?)
                //         C3 z = "cat";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""cat""").WithArguments("string", "C3").WithLocation(9, 16)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

        [ConditionalFact(typeof(CoreClrOnly))]
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

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ExpressionTree_01()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class C
{
    static void Main()
    {
        Expression<Func<byte[]>> x = () => ""hello"";
        Expression<FuncSpanOfByte> y = () => ""dog"";
        Expression<FuncReadOnlySpanOfByte> z = () => ""cat"";

        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
        System.Console.WriteLine(z);
    }

    delegate Span<byte> FuncSpanOfByte();
    delegate ReadOnlySpan<byte> FuncReadOnlySpanOfByte();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            // PROTOTYPE(UTF8StringLiterals) : Confirm this is the behavior we want. Or should we disallow this conversion in expression tree?
            CompileAndVerify(comp, expectedOutput: @"
() => new [] {104, 101, 108, 108, 111}
() => new Span`1(new [] {100, 111, 103})
() => new ReadOnlySpan`1(new [] {99, 97, 116})
").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            // PROTOTYPE(UTF8StringLiterals) : Confirm this is the behavior we want. Or should we disallow this conversion in expression tree?
            CompileAndVerify(comp, expectedOutput: @"
() => Convert(new [] {104, 101, 108, 108, 111}, C1)
() => Convert(new Span`1(new [] {100, 111, 103}), C2)
() => Convert(new ReadOnlySpan`1(new [] {99, 97, 116}), C3)
").VerifyDiagnostics();
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

    static string Test(ReadOnlySpan<char> a) => ""ReadOnlySpan"";
    static string Test(byte[] a) => ""array"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpanarray").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            CompileAndVerify(comp, expectedOutput: @"ReadOnlySpanarray").VerifyDiagnostics();
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
        public void OverloadResolution_06()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""));
    }

    static string Test(ReadOnlySpan<byte> a) => ""ReadOnlySpan"";
    static string Test(byte[] a) => ""array"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"array").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_07()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""));
    }

    static string Test(Span<byte> a) => ""Span"";
    static string Test(byte[] a) => ""array"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"array").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_08()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""));
    }

    static string Test(ReadOnlySpan<byte> a) => ""ReadOnlySpan"";
    static string Test(Span<byte> a) => ""Span"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"Span").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_09()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""));
    }

    static string Test(ReadOnlySpan<byte> a) => ""ReadOnlySpan"";
    static string Test(byte[] a) => ""array"";
    static string Test(Span<byte> a) => ""Span"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"array").VerifyDiagnostics();
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
            // The behavior has changed
            // PROTOTYPE(UTF8StringLiterals) : Add an entry in "docs/compilers/CSharp/Compiler Breaking Changes - DotNet 7.md"?
            CompileAndVerify(comp, expectedOutput: @"byte[]").
                VerifyDiagnostics();

            comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            // PROTOTYPE(UTF8StringLiterals) : Confirm we are comfortable with not changing semantics based on language version and keeping an error for this scenario.
            // PROTOTYPE(UTF8StringLiterals) : Add an entry in "docs/compilers/CSharp/Compiler Breaking Changes - DotNet 7.md"?
            comp.VerifyDiagnostics(
                // (9,31): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Console.WriteLine(p.M(""));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""""").WithArguments("Utf8 String Literals").WithLocation(9, 31)
                );
        }

        [Fact]
        public void NullableAnalysis_01()
        {
            var source = @"
#nullable enable

class C
{
    static void Main()
    {
        byte[]? x = ""hello"";
        _ = x.Length;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullableAnalysis_02()
        {
            var source = @"
#nullable enable

class C
{
    static void Main()
    {
        byte[]? x = (string)null;
        _ = x.Length;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,21): error CS0029: Cannot implicitly convert type 'string' to 'byte[]'
                //         byte[]? x = (string)null;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(string)null").WithArguments("string", "byte[]").WithLocation(8, 21),
                // (8,21): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         byte[]? x = (string)null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "(string)null").WithLocation(8, 21),
                // (9,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = x.Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(9, 13)
                );
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

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UTF8StringLiteral_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine();
        Helpers.Print(Test1());
        Helpers.Print(Test2());
        Helpers.Print(Test3());
    }

    static byte[] Test1() => ""hello""u8;
    static Span<byte> Test2() => ""dog""u8;
    static ReadOnlySpan<byte> Test3() => ""cat""u8;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            verifier.VerifyIL("C.Test1()", @"
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.CD6357EFDD966DE8C0CB2F876CC89EC74CE35F0968E11743987084BD42FB8944""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.Span<byte> System.Span<byte>.op_Implicit(byte[])""
  IL_0016:  ret
}
");

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.77AF778B51ABD4A3C51C5DDD97204A9C3AE614EBCCB75A606C3B6865AED6744E""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (13,30): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static byte[] Test1() => "hello"u8;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""hello""u8").WithArguments("Utf8 String Literals").WithLocation(13, 30),
                // (14,34): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static Span<byte> Test2() => "dog"u8;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""dog""u8").WithArguments("Utf8 String Literals").WithLocation(14, 34),
                // (15,42): error CS8652: The feature 'Utf8 String Literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static ReadOnlySpan<byte> Test3() => "cat"u8;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""cat""u8").WithArguments("Utf8 String Literals").WithLocation(15, 42)
                );
        }


        [Fact]
        public void MissingType_01()
        {
            var source = @"
using System;
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
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS0518: Predefined type 'System.Byte' is not defined or imported
                //         _ = "hello"u8;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello""u8").WithArguments("System.Byte").WithLocation(7, 13)
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
        Helpers.Print(Test1());
        Helpers.Print(Test2());
        Helpers.Print(Test3());
    }

    static byte[] Test1() => ""hello""u8;
    static Span<byte> Test2() => ""dog""u8;
    static ReadOnlySpan<byte> Test3() => ""cat""u8;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Array);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Pointer);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
{ 0x64 0x6F 0x67 }
{ 0x63 0x61 0x74 }
").VerifyDiagnostics();

            verifier.VerifyIL("C.Test1()", @"
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.CD6357EFDD966DE8C0CB2F876CC89EC74CE35F0968E11743987084BD42FB8944""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.Span<byte> System.Span<byte>.op_Implicit(byte[])""
  IL_0016:  ret
}
");

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldsflda    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.77AF778B51ABD4A3C51C5DDD97204A9C3AE614EBCCB75A606C3B6865AED6744E""
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ReadOnlySpan<byte>..ctor(void*, int)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void MissingHelpers_08()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Helpers.Print(Test2());
    }

    static Span<byte> Test2() => ""dog""u8;
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Pointer);
            var verifier = CompileAndVerify(comp, expectedOutput: "{ 0x64 0x6F 0x67 }").VerifyDiagnostics();

            verifier.VerifyIL("C.Test2()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.CD6357EFDD966DE8C0CB2F876CC89EC74CE35F0968E11743987084BD42FB8944""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.Span<byte> System.Span<byte>.op_Implicit(byte[])""
  IL_0016:  ret
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
            var verifier = CompileAndVerify(comp, expectedOutput: "{ 0x63 0x61 0x74 }").VerifyDiagnostics();

            verifier.VerifyIL("C.Test3()", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.77AF778B51ABD4A3C51C5DDD97204A9C3AE614EBCCB75A606C3B6865AED6744E""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
  IL_0016:  ret
}
");
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
            CompileAndVerify(comp, expectedOutput: @"array").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_12()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(Test(""s""u8));
    }

    static string Test(Span<byte> a) => ""Span"";
    static string Test(byte[] a) => ""array"";
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"array").VerifyDiagnostics();
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
            CompileAndVerify(comp, expectedOutput: @"Span").VerifyDiagnostics();
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
            CompileAndVerify(comp, expectedOutput: @"array").VerifyDiagnostics();
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
    public static implicit operator C1(byte[] x)
    {
        Helpers.Print(x);
        return new C1();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
").VerifyDiagnostics();
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
    public static implicit operator C1(byte[] x)
    {
        Helpers.Print(x);
        return new C1();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            // PROTOTYPE(UTF8StringLiterals) : Confirm this is the behavior we want. We are relying on user-defined conversions to convert from byte[] to span types.
            comp.VerifyDiagnostics(
                // (7,16): error CS0029: Cannot implicitly convert type 'byte[]' to 'C2'
                //         C2 y = "dog"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""u8").WithArguments("byte[]", "C2").WithLocation(7, 16),
                // (8,16): error CS0029: Cannot implicitly convert type 'byte[]' to 'C3'
                //         C3 z = "cat"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""u8").WithArguments("byte[]", "C3").WithLocation(8, 16)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
    public static implicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            // PROTOTYPE(UTF8StringLiterals) : Confirm this is the behavior we want. We are relying on user-defined conversions to convert from byte[] to span types.
            comp.VerifyDiagnostics(
                // (7,17): error CS0030: Cannot convert type 'byte[]' to 'C2'
                //         var y = (C2)"dog"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C2)""dog""u8").WithArguments("byte[]", "C2").WithLocation(7, 17),
                // (8,17): error CS0030: Cannot convert type 'byte[]' to 'C3'
                //         var z = (C3)"cat"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C3)""cat""u8").WithArguments("byte[]", "C3").WithLocation(8, 17)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
                // (7,16): error CS0266: Cannot implicitly convert type 'byte[]' to 'C1'. An explicit conversion exists (are you missing a cast?)
                //         C1 x = "hello"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"""hello""u8").WithArguments("byte[]", "C1").WithLocation(7, 16),
                // (8,16): error CS0029: Cannot implicitly convert type 'byte[]' to 'C2'
                //         C2 y = "dog"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""dog""u8").WithArguments("byte[]", "C2").WithLocation(8, 16),
                // (9,16): error CS0029: Cannot implicitly convert type 'byte[]' to 'C3'
                //         C3 z = "cat"u8;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""cat""u8").WithArguments("byte[]", "C3").WithLocation(9, 16)
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
    public static explicit operator C1(byte[] x)
    {
        Helpers.Print(x);
        return new C1();
    }
}
";
            var comp = CreateCompilation(source + HelpersSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"
{ 0x68 0x65 0x6C 0x6C 0x6F }
").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
    public static explicit operator C3(ReadOnlySpan<byte> x)
    {
        return new C3();
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            // PROTOTYPE(UTF8StringLiterals) : Confirm this is the behavior we want. We are relying on user-defined conversions to convert from byte[] to span types.
            comp.VerifyDiagnostics(
                // (7,17): error CS0030: Cannot convert type 'byte[]' to 'C2'
                //         var y = (C2)"dog"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C2)""dog""u8").WithArguments("byte[]", "C2").WithLocation(7, 17),
                // (8,17): error CS0030: Cannot convert type 'byte[]' to 'C3'
                //         var z = (C3)"cat"u8;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(C3)""cat""u8").WithArguments("byte[]", "C3").WithLocation(8, 17)
                );
        }

        [Fact]
        public void NaturalType_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine((""hello""u8).GetType());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"System.Byte[]").VerifyDiagnostics();
        }

        // PROTOTYPE(UTF8StringLiterals) : Test default parameter values and attribute applications
    }
}
