// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.ExplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class IgnoreTests
    {
        [Fact]
        public async Task AnalyzeProgram_TakesIgnoredAttributesIntoAccount()
        {
            const string sampleProgram =
                @"using System;
                using Roslyn.Utilities;
                
                class TypeName
                {
                    [PerformanceSensitive(""uri"")]
                    [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                    public void CreateString1() {
                        string str = new string('a', 5);
                    }

                    [PerformanceSensitive(""uri"")]
                    [System.CodeDom.Compiler.GeneratedCodeAttribute(""MyCompiler"", ""1.0.0.3"")]
                    public void CreateString2() {
                        string str = new string('a', 5);
                    }

                    [PerformanceSensitive(""uri"")]
                    [System.ObsoleteAttribute]
                    public void CreateString3() {
                        string str = new string('a', 5);
                    }
                }";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(21,38): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(21, 38));
        }
    }
}