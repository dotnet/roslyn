// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Diagnostics.CSharp.Analyzers.BlankLines;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests.BlankLines
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpBlankLinesDiagnosticAnalyzer,
        CSharpBlankLinesCodeFixProvider>;

    public class BlankLinesTests
    {
        [Fact]
        public async Task TestOneBlankLineAtTopOfFile()
        {
            var code =
@"
// comment";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAtTopOfFile()
        {
            var code =
@"[||]

// comment";
            var fixedCode =
@"
// comment";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAtTopOfFile()
        {
            var code =
@"[||]


// comment";
            var fixedCode =
@"
// comment";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAtTopOfFile()
        {
            var code =
@"[||]



// comment";
            var fixedCode =
@"
// comment";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAtTopOfEmptyFile()
        {
            var code =
@"
";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLinesAtTopOfEmptyFile()
        {
            var code =
@"[||]

";
            var fixedCode =
@"
";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLinesAtTopOfEmptyFile()
        {
            var code =
@"[||]


";
            var fixedCode =
@"
";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLinesAtTopOfEmptyFile()
        {
            var code =
@"[||]



";
            var fixedCode =
@"
";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAtEndOfFile_1()
        {
            var code =
@"class C
{
}";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAtEndOfFile_2()
        {
            var code =
@"class C
{
}
";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}

";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}
[||]

";
            var fixedCode =
@"class C
{
}

";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}
[||]


";
            var fixedCode =
@"class C
{
}

";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}
[||]



";
            var fixedCode =
@"class C
{
}

";

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }
    }
}
