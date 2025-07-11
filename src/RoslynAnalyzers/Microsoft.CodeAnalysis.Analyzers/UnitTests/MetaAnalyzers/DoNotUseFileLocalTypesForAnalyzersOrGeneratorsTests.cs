// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DoNotUseFileTypesForAnalyzersOrGenerators,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.MetaAnalyzers
{
    public sealed partial class MetaAnalyzersTests
    {
        private const string CompilerReferenceVersion = "4.6.0";

        [Fact]
        public Task FiresOnFileLocalType_CodeFixProvider()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                file class Type : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
                {
                    public override ImmutableArray<string> FixableDiagnosticIds => throw null!;
                    public override FixAllProvider GetFixAllProvider() => throw null!;
                    public override Task RegisterCodeFixesAsync(CodeFixContext context) => throw null!;
                }
                """,
                LanguageVersion = LanguageVersion.CSharp11,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(5,12): error RS1043: Type 'Type' should not be marked with 'file'
                    VerifyCS.Diagnostic().WithSpan(5, 12, 5, 16).WithArguments("Type"),
                }
            }.RunAsync();

        [Fact]
        public Task FiresOnFileLocalType_DiagnosticAnalyzer()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;
                file class Type : Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer
                {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null!;
                    public override void Initialize(AnalysisContext context) => throw null!;
                   
                }
                """,
                LanguageVersion = LanguageVersion.CSharp11,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(4,12): error RS1043: Type 'Type' should not be marked with 'file'
                    VerifyCS.Diagnostic().WithSpan(4, 12, 4, 16).WithArguments("Type"),
                }
            }.RunAsync();

        [Fact]
        public Task FiresOnFileLocalType_ISourceGenerator()
            => new VerifyCS.Test
            {
                TestCode = """
                using Microsoft.CodeAnalysis;
                file class Type : Microsoft.CodeAnalysis.ISourceGenerator
                {
                    public void Initialize(GeneratorInitializationContext context) => throw null!;
                    public void Execute(GeneratorExecutionContext context) => throw null!;
                }
                """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages([new PackageIdentity("Microsoft.CodeAnalysis.Common", CompilerReferenceVersion)]),
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(2,12): error RS1043: Type 'Type' should not be marked with 'file'
                    VerifyCS.Diagnostic().WithSpan(2, 12, 2, 16).WithArguments("Type"),
                },
            }.RunAsync();

        [Fact]
        public Task FiresOnFileLocalType_IIncrementalGenerator()
            => new VerifyCS.Test
            {
                TestCode = """
                using Microsoft.CodeAnalysis;
                file class Type : Microsoft.CodeAnalysis.IIncrementalGenerator
                {
                    public void Initialize(IncrementalGeneratorInitializationContext context) => throw null!;
                }
                """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages([new PackageIdentity("Microsoft.CodeAnalysis.Common", CompilerReferenceVersion)]),
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(2,12): error RS1043: Type 'Type' should not be marked with 'file'
                    VerifyCS.Diagnostic().WithSpan(2, 12, 2, 16).WithArguments("Type"),
                },
            }.RunAsync();
    }
}
