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
        public async Task TestSingleBlankLineAtTopOfFile()
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
        public async Task TestMultipleBlankLineAtTopOfFile()
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
    }
}
