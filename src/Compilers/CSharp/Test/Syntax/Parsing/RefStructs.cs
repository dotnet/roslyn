// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.ReadonlyReferences)]
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

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
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
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (4,9): error CS1031: Type expected
                //     ref class S1{}
                Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(4, 9),
                // (6,16): error CS1031: Type expected
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_TypeExpected, "unsafe").WithLocation(6, 16),
                // (6,30): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S2").WithLocation(6, 30)
            );
        }

        [Fact]
        public void RefStructPartial()
        {
            var text = @"
class Program
{
    partial ref struct S1{}

    partial ref struct S1{}
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
            );
        }

    }
}
