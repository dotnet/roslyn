// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class RefStructs : ParsingTests
    {
        public RefStructs(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void RefStructSimple()
        {
            var text = @"
class Program
{
    ref struct S1{}

    public ref struct S2{}
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefStructSimpleLangVer()
        {
            var text = @"
class Program
{
    ref struct S1{}

    public ref struct S2{}
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (4,5): error CS8107: Feature 'ref structs' is not available in C# 7. Please use language version 7.2 or greater.
                //     ref struct S1{}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "ref").WithArguments("ref structs", "7.2").WithLocation(4, 5),
                // (6,12): error CS8107: Feature 'ref structs' is not available in C# 7. Please use language version 7.2 or greater.
                //     public ref struct S2{}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "ref").WithArguments("ref structs", "7.2").WithLocation(6, 12)
            );
        }

        [Fact]
        public void RefStructErr()
        {
            var text = @"
class Program
{
    ref class S1{}

    public ref unsafe struct S2{}

    ref interface I1{};

    public ref delegate ref int D1();
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (4,15): error CS0106: The modifier 'ref' is not valid for this item
                //     ref class S1{}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S1").WithArguments("ref").WithLocation(4, 15),
                // (6,30): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S2").WithLocation(6, 30),
                // (6,12): error CS9380: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(6, 12),
                // (8,19): error CS0106: The modifier 'ref' is not valid for this item
                //     ref interface I1{};
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I1").WithArguments("ref").WithLocation(8, 19),
                // (10,33): error CS0106: The modifier 'ref' is not valid for this item
                //     public ref delegate ref int D1();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "D1").WithArguments("ref").WithLocation(10, 33)
            );
        }

        [Fact]
        public void PartialRefStruct()
        {
            var text = @"
class Program
{
    partial ref struct S {}
    partial ref struct S {}
}
";
            var comp = CreateCompilation(text);
            // Phase 3 (relaxing 'ref' type-modifier ordering) will produce cleaner diagnostics;
            // for now the mix of relaxed 'partial' + strict 'ref' produces cascading parse errors.
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefPartialStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    ref partial struct S {}
    ref partial struct S {}
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefPartialReadonlyStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    ref partial readonly struct S {}
    ref partial readonly struct S {}
}");
            comp.VerifyDiagnostics(
                // (4,5): error CS9380: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(4, 5),
                // (5,5): error CS9380: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(5, 5));
        }

        [Fact]
        public void RefReadonlyPartialStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    partial ref readonly struct S {}
    partial ref readonly struct S {}
}");
            comp.VerifyDiagnostics(
                // (4,13): error CS9380: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(4, 13),
                // (5,13): error CS9380: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(5, 13));
        }

        [Fact]
        public void ReadonlyPartialRefStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    readonly partial ref struct S {}
    readonly partial ref struct S {}
}");
            // Phase 3 (relaxing 'ref' type-modifier ordering) will produce cleaner diagnostics;
            // for now the mix of relaxed 'partial' + strict 'ref' produces cascading parse errors.
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadonlyRefPartialStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    readonly ref partial struct S {}
    readonly ref partial struct S {}
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StackAllocParsedAsSpan_Declaration()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    unsafe public void M()
    {
        int* a = stackalloc int[10];
        var b = stackalloc int[10];
        Span<int> c = stackalloc int [10];
    }
}", TestOptions.UnsafeDebugDll).GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_LocalFunction()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        unsafe void local()
        {
            int* x = stackalloc int[10];
        }
    }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_MethodCall()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        Visit(stackalloc int [10]);
    }
    public void Visit(Span<int> s) { }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_DotAccess()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        Console.WriteLine((stackalloc int [10]).Length);
    }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_Cast()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        void* x = (void*)(stackalloc int[10]);
    }
}").GetParseDiagnostics().Verify();
        }
    }
}
