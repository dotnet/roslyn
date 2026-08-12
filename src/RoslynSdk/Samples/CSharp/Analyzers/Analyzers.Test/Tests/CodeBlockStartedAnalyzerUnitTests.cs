// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sample.Analyzers.CodeBlockStartedAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sample.Analyzers.Test
{
    public class CodeBlockStartedAnalyzerUnitTests
    {
        [Fact]
        public async Task CodeBlockStartedAnalyzerTest()
        {
            string test = @"
class C
{
    public int M1(int p1, int p2)
    {
        return M2(p1, p1);
    }

    public int M2(int p1, int p2)
    {
        return p1 + p2;
    }
}";
            DiagnosticResult expected = Verify.Diagnostic().WithArguments("p2", "M1").WithLocation(4, 31);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
