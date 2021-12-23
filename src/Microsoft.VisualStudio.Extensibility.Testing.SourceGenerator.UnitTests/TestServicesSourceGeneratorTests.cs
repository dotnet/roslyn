// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Testing;
    using Xunit;
    using VerifyCS = Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<
        Microsoft.VisualStudio.Extensibilty.Testing.SourceGenerator.TestServicesSourceGenerator>;

    public class TestServicesSourceGeneratorTests
    {
        [Fact]
        public async Task TestGenerationForVS2022()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net472.Default,
                    Sources =
                    {
                    },
                    GeneratedSources =
                    {
                    },
                },
            }.RunAsync();
        }
    }
}
