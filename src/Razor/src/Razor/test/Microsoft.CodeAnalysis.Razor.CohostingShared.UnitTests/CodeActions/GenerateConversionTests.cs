// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateConversionTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task GenerateExplicitConversion_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private int M()
                {
                    return [||](int)this;
                }
            }
            """;

        var expected = """
            @using System
            @code
            {
                private int M()
                {
                    return (int)this;
                }

                public static explicit operator int(File1 v)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateConversion,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateExplicitConversion_FromExplicitExpression_WithoutCodeBlock()
    {
        var input = """
            @(([||]int)this)
            """;

        var expected = """
            @using System
            @((int)this)
            @code {
                public static explicit operator int(File1 v)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateConversion,
            makeDiagnosticsRequest: true);
    }
}
