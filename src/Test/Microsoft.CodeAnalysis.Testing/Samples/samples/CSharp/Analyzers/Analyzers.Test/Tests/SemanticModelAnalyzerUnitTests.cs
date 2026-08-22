// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sample.Analyzers.SemanticModelAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sample.Analyzers.Test
{
    public class SemanticModelAnalyzerUnitTests
    {
        [Fact]
        public async Task SemanticModelAnalyzerTest()
        {
            string test = @"
class C
{
    public async int M()
    {
    }
}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic().WithArguments("Test0.cs", 1),
                DiagnosticResult.CompilerError("CS0161").WithLocation(4, 22).WithMessage("'C.M()': not all code paths return a value"),
                DiagnosticResult.CompilerError("CS1983").WithLocation(4, 22).WithMessage("The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>"),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
