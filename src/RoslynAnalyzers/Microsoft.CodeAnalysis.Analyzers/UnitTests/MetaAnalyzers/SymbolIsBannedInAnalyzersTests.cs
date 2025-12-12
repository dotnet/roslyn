// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.CSharpSymbolIsBannedInAnalyzersAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.BasicSymbolIsBannedInAnalyzersAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class SymbolIsBannedInAnalyzersTests
    {
        [Fact]
        public Task UseBannedApi_EnforcementEnabled_CSharp()
            => new VerifyCS.Test
            {
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9,
                TestCode = """
                using System.IO;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer
                {
                }

                class C
                {
                    void M()
                    {
                        _ = File.Exists("something");
                    }
                }
                """,
                ExpectedDiagnostics = {
                    // /0/Test0.cs(15,13): error RS1035: The symbol 'File' is banned for use by analyzers: Do not do file IO in analyzers
                    VerifyCS.Diagnostic("RS1035").WithSpan(14, 13, 14, 37).WithArguments("File", ": Do not do file IO in analyzers"),
                },
                TestState = {
                    AnalyzerConfigFiles = { ("/.editorconfig", $"""
                        root = true

                        [*]
                        build_property.EnforceExtendedAnalyzerRules = true
                        """), },
                }
            }.RunAsync();

        [Fact]
        public Task UseBannedApi_EnforcementEnabled_Generator_CSharp()
            => new VerifyCS.Test
            {
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9,
                TestCode = """
                using System.IO;
                using Microsoft.CodeAnalysis;

                namespace Microsoft.CodeAnalysis
                {
                    public class GeneratorAttribute : System.Attribute { }
                }

                [Generator]
                class MyAnalyzer
                {
                }

                class C
                {
                    void M()
                    {
                        _ = File.Exists("something");
                    }
                }
                """,
                ExpectedDiagnostics = {
                    // /0/Test0.cs(19,13): error RS1035: The symbol 'File' is banned for use by analyzers: Do not do file IO in analyzers
                    VerifyCS.Diagnostic("RS1035").WithSpan(18, 13, 18, 37).WithArguments("File", ": Do not do file IO in analyzers"),
                },
                TestState = {
                    AnalyzerConfigFiles = { ("/.editorconfig", $"""
                        root = true

                        [*]
                        build_property.EnforceExtendedAnalyzerRules = true
                        """), },
                }
            }.RunAsync();

        [Fact]
        public Task UseBannedApi_EnforcementNotSpecified_CSharp()
            => new VerifyCS.Test
            {
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9,
                TestCode = """
                using System.IO;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer
                {
                }

                class C
                {
                    void M()
                    {
                        _ = File.Exists("something");
                    }
                }
                """,
                ExpectedDiagnostics = {
                    // /0/Test0.cs(7,7): warning RS1036: 'MyAnalyzer': A project containing analyzers or source generators should specify the property '<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>'
                    VerifyCS.Diagnostic("RS1036").WithSpan(6, 7, 6, 17).WithArguments("MyAnalyzer"),
                }
            }.RunAsync();

        [Fact]
        public Task UseBannedApi_EnforcementDisabled_CSharp()
            => new VerifyCS.Test
            {
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9,
                TestCode = """
                using System.IO;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer
                {
                }

                class C
                {
                    void M()
                    {
                        _ = File.Exists("something");
                    }
                }
                """,
                TestState = {
                    AnalyzerConfigFiles = { ("/.editorconfig", $"""
                        root = true

                        [*]
                        build_property.EnforceExtendedAnalyzerRules = false
                        """),
                    },
                }
            }.RunAsync();

        [Fact]
        public Task UseBannedApi_EnforcementEnabled_Basic()
            => new VerifyVB.Test
            {
                LanguageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.Latest,
                TestCode = """
                Imports System.IO
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Class MyDiagnosticAnalyzer
                End Class

                Class C
                    Function M()
                        File.Exists("something")
                    End Function
                End Class
                """,
                ExpectedDiagnostics =
                {
                    // /0/Test0.vb(12,9,12,33): error RS1035: The symbol 'File' is banned for use by analyzers: Do not do file IO in analyzers
                    VerifyVB.Diagnostic("RS1035").WithSpan(11, 9, 11, 33).WithArguments("File", ": Do not do file IO in analyzers"),
                },
                TestState = {
                    AnalyzerConfigFiles = { ("/.editorconfig", $"""
                        root = true

                        [*]
                        build_property.EnforceExtendedAnalyzerRules = true
                        """),
                    },
                }
            }.RunAsync();

        [Fact]
        public Task UseBannedApi_EnforcementNotSpecified_Basic()
            => new VerifyVB.Test
            {
                LanguageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.Latest,
                TestCode = """
                Imports System.IO
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Class MyDiagnosticAnalyzer
                End Class

                Class C
                    Function M()
                        File.Exists("something")
                    End Function
                End Class
                """,
                ExpectedDiagnostics =
                {
                    // /0/Test0.vb(7,7): warning RS1036: 'MyDiagnosticAnalyzer': A project containing analyzers or source generators should specify the  property '<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>'
                    VerifyVB.Diagnostic("RS1036").WithSpan(6, 7, 6, 27).WithArguments("MyDiagnosticAnalyzer"),
                }
            }.RunAsync();

        [Fact]
        public Task UseBannedApi_EnforcementDisabled_Basic()
            => new VerifyVB.Test
            {
                LanguageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.Latest,
                TestCode = """
                Imports System.IO
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Class MyDiagnosticAnalyzer
                End Class

                Class C
                    Function M()
                        File.Exists("something")
                    End Function
                End Class
                """,
                TestState = {
                    AnalyzerConfigFiles = { ("/.editorconfig", $"""
                        root = true

                        [*]
                        build_property.EnforceExtendedAnalyzerRules = false
                        """),
                    },
                }
            }.RunAsync();

        [Fact]
        public Task UseBannedApi_ISourceGenerator()
            => new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.Default.WithPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis.Common", "4.5.0"))),
                TestCode = """
                    using Microsoft.CodeAnalysis;
                    
                    [Generator]
                    class MyGenerator : ISourceGenerator
                    {
                        public void Initialize(GeneratorInitializationContext context)
                        {
                            {|#0:context.RegisterForPostInitialization(_ => { })|};
                        }
                        public void Execute(GeneratorExecutionContext context)
                        {
                            {|#1:context.AddSource("Generated.cs", "// <auto-generated/>")|};
                        }
                    }
                    """,
                TestState =
                {
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", """
                        root = true

                        [*]
                        build_property.EnforceExtendedAnalyzerRules = true
                        """),
                    },
                },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(8,9): error RS1035: The symbol 'GeneratorInitializationContext' is banned for use by analyzers: Non-incremental source generators should not be used, implement IIncrementalGenerator instead
                    VerifyCS.Diagnostic("RS1035").WithLocation(0).WithArguments("GeneratorInitializationContext", ": Non-incremental source generators should not be used, implement IIncrementalGenerator instead"),
                    // /0/Test0.cs(14,9): error RS1035: The symbol 'GeneratorExecutionContext' is banned for use by analyzers: Non-incremental source generators should not be used, implement IIncrementalGenerator instead
                    VerifyCS.Diagnostic("RS1035").WithLocation(1).WithArguments("GeneratorExecutionContext", ": Non-incremental source generators should not be used, implement IIncrementalGenerator instead"),
                }
            }.RunAsync();
    }
}
