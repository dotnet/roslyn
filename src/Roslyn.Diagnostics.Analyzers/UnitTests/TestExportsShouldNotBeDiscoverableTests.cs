// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task Discoverable_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{ }}
";
            var fixedSource = $@"
using {mefNamespace};

[Export]
[PartNotDiscoverable]
class C {{ }}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(4, 2, 4, 8).WithArguments("C") },
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
        public async Task NotDiscoverable_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
[PartNotDiscoverable]
class C {{ }}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task Discoverable_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports {mefNamespace}

<Export>
Class C
End Class
";
            var fixedSource = $@"
Imports {mefNamespace}

<Export>
<PartNotDiscoverable>
Class C
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(4, 2, 4, 8).WithArguments("C") },
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
        public async Task NotDiscoverable_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports {mefNamespace}

<Export>
<PartNotDiscoverable>
Class C
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }
    }
}
