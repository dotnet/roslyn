// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateDeconstructMethodTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task GenerateDeconstructMethod_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private void M()
                {
                    (int x, int y) = [||]this;
                }
            }
            """;

        var expected = """
            @using System
            @code
            {
                private void M()
                {
                    (int x, int y) = this;
                }

                private void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateDeconstructMethod,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateDeconstructMethod_WithoutCodeBlock()
    {
        var input = """
            @{
                (int x, int y) = [||]this;
            }
            """;

        var expected = """
            @using System
            @{
                (int x, int y) = this;
            }
            @code {
                private void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateDeconstructMethod,
            makeDiagnosticsRequest: true);
    }
}
