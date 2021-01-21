// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer>;
using VerifyCS = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer,
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer>,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class MultipleProjectsTests
    {
        [Fact]
        public async Task TwoCSharpProjects_Independent()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : {|CS0246:Base2|} [|{|] }",
                        @"public class Base1 [|{|] }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : {|CS0246:Base1|} [|{|] }",
                                @"public class Base2 [|{|] }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_IndependentWithMarkupLocations()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : {|#0:Base2|} {|#1:{|} }",
                        @"public class Base1 {|#2:{|} }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : {|#3:Base1|} {|#4:{|} }",
                                @"public class Base2 {|#5:{|} }",
                            },
                        },
                    },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(1,25): error CS0246: The type or namespace name 'Base2' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("Base2"),

                        // /0/Test0.cs(1,31): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(1),

                        // /0/Test1.cs(1,20): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(2),

                        // /Secondary/Test0.cs(1,25): error CS0246: The type or namespace name 'Base1' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithLocation(3).WithArguments("Base1"),

                        // /Secondary/Test0.cs(1,31): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(4),

                        // /Secondary/Test1.cs(1,20): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(5),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_PrimaryReferencesSecondary()
        {
            // TestProject references Secondary
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : Base2 [|{|] }",
                        @"public class Base1 [|{|] }",
                    },
                    AdditionalProjectReferences = { "Secondary", },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : {|CS0246:Base1|} [|{|] }",
                                @"public class Base2 [|{|] }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_SecondaryReferencesPrimary()
        {
            // Secondary references TestProject
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : {|CS0246:Base2|} [|{|] }",
                        @"public class Base1 [|{|] }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : Base1 [|{|] }",
                                @"public class Base2 [|{|] }",
                            },
                            AdditionalProjectReferences = { "TestProject" },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_DefaultPaths()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<EmptyDiagnosticAnalyzer>
                {
                    TestState =
                    {
                        Sources =
                        {
                            @"public class Derived1 : Base1 { }",
                        },
                        AdditionalProjects =
                        {
                            ["Secondary"] =
                            {
                                Sources =
                                {
                                    @"public class Derived2 : Base2 { }",
                                },
                            },
                        },
                    },
                }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"2\"" + Environment.NewLine
                + Environment.NewLine
                + "Diagnostics:" + Environment.NewLine
                + "// /0/Test0.cs(1,25): error CS0246: The type or namespace name 'Base1' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine
                + "DiagnosticResult.CompilerError(\"CS0246\").WithSpan(1, 25, 1, 30).WithArguments(\"Base1\")," + Environment.NewLine
                + "// /Secondary/Test0.cs(1,25): error CS0246: The type or namespace name 'Base2' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine
                + "DiagnosticResult.CompilerError(\"CS0246\").WithSpan(\"/Secondary/Test0.cs\", 1, 25, 1, 30).WithArguments(\"Base2\")," + Environment.NewLine
                + Environment.NewLine;

            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }
    }
}
