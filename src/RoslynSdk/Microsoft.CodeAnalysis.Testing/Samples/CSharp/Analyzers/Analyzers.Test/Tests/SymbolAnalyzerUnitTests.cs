// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sample.Analyzers.SymbolAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sample.Analyzers.Test
{
    public class SymbolAnalyzerUnitTests
    {
        [Fact]
        public async Task SymbolAnalyzerTest()
        {
            string test = @"
class BadOne
{
    public void BadOne() {}
}

class GoodOne
{
}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic().WithLocation(2, 7).WithArguments("BadOne"),
                DiagnosticResult.CompilerError("CS0542").WithLocation(4, 17).WithMessage("'BadOne': member names cannot be the same as their enclosing type"),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
