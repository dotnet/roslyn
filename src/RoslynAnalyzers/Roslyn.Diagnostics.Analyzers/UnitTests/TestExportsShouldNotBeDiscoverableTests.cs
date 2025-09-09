// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.TestExportsShouldNotBeDiscoverable,
    Roslyn.Diagnostics.Analyzers.TestExportsShouldNotBeDiscoverableCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.TestExportsShouldNotBeDiscoverable,
    Roslyn.Diagnostics.Analyzers.TestExportsShouldNotBeDiscoverableCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class TestExportsShouldNotBeDiscoverableTests
    {
        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task Discoverable_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C { }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [Export]
                [PartNotDiscoverable]
                class C { }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(3, 2, 3, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task DiscoverableAddImport_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                [{{mefNamespace}}.Export]
                class C { }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [{{mefNamespace}}.Export]
                [PartNotDiscoverable]
                class C { }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(1, 2, 1, mefNamespace.Length + 9).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task NotDiscoverable_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                [PartNotDiscoverable]
                class C { }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task Discoverable_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <Export>
                <PartNotDiscoverable>
                Class C
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(3, 2, 3, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task DiscoverableAddImport_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                <{mefNamespace}.Export>
                Class C
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <{mefNamespace}.Export>
                <PartNotDiscoverable>
                Class C
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(1, 2, 1, mefNamespace.Length + 9).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task NotDiscoverable_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                <PartNotDiscoverable>
                Class C
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }
    }
}
