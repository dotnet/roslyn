// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sample.Analyzers.CodeBlockAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sample.Analyzers.Test
{
    public class CodeBlockAnalyzerUnitTests
    {
        [Fact]
        public async Task CodeBlockAnalyzerTest()
        {
            string test = @"
class C
{
    public void M1()
    {
    }

    public virtual void M2()
    {
    }

    public int M3()
    {
    }
}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic().WithLocation(4, 17).WithArguments("M1"),
                DiagnosticResult.CompilerError("CS0161").WithLocation(12, 16).WithMessage("'C.M3()': not all code paths return a value"),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
