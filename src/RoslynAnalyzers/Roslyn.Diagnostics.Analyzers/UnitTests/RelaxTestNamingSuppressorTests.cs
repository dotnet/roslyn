// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.UnitTests.RelaxTestNamingSuppressorTests.WarnForMissingAsyncSuffix,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class RelaxTestNamingSuppressorTests
    {
        private static Solution WithoutSuppressedDiagnosticsTransform(Solution solution, ProjectId projectId)
        {
            var compilationOptions = solution.GetProject(projectId)!.CompilationOptions;
            return solution.WithProjectCompilationOptions(projectId, compilationOptions!.WithReportSuppressedDiagnostics(false));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41584")]
        public async Task TestClassWithFactAsync()
        {
            var code = """
                using System.Threading.Tasks;
                using Xunit;

                public class SomeClass {
                [Fact]
                public async Task [|TestMethod|]() { }
                }
                """;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestState = { Sources = { code } },
                SolutionTransforms = { WithoutSuppressedDiagnosticsTransform },
            }.RunAsync();

            await new TestWithSuppressor
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestState = { Sources = { code }, MarkupHandling = MarkupMode.Ignore, },
                SolutionTransforms = { WithoutSuppressedDiagnosticsTransform },
            }.RunAsync();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41584")]
        public async Task TestClassWithTheoryAsync()
        {
            var code = """
                using Xunit;

                public class [|SomeClass|] {
                [Theory, InlineData(0)]
                public void TestMethod(int arg) { }
                }
                """;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestState = { Sources = { code } },
                SolutionTransforms = { WithoutSuppressedDiagnosticsTransform },
            }.RunAsync();

            await new TestWithSuppressor
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestState = { Sources = { code }, MarkupHandling = MarkupMode.Ignore, },
                SolutionTransforms = { WithoutSuppressedDiagnosticsTransform },
            }.RunAsync();
        }

        [Fact]
        public async Task TestAlreadyHasAsyncSuffixAsync()
        {
            var code = """
                using System.Threading.Tasks;
                using Xunit;

                public class SomeClass {
                [Fact]
                public async Task TestMethodAsync() { }
                }
                """;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestState = { Sources = { code } },
            }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class WarnForMissingAsyncSuffix : DiagnosticAnalyzer
        {
            [SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1017:DiagnosticId for analyzers must be a non-null constant.", Justification = "For suppression test only.")]
            public static readonly DiagnosticDescriptor Rule = new(RelaxTestNamingSuppressor.Rule.SuppressedDiagnosticId, "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
                context.EnableConcurrentExecution();

                context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
            }

            private void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var method = (IMethodSymbol)context.Symbol;
                if (method.Name.EndsWith("Async", StringComparison.Ordinal))
                {
                    return;
                }

                if (method.ReturnType.MetadataName != "Task")
                {
                    // Not asynchronous (incomplete checking is sufficient for this test)
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0]));
            }
        }

        internal sealed class TestWithSuppressor : VerifyCS.Test
        {
            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                foreach (var analyzer in base.GetDiagnosticAnalyzers())
                    yield return analyzer;

                yield return new RelaxTestNamingSuppressor();
            }
        }
    }
}
