// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GeneratePropertyTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    private const int PropertyActionIndex = 0;

    [Fact]
    public async Task GenerateProperty_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private int M()
                {
                    return [||]NewProperty;
                }
            }
            """;

        var expected = """
            @code
            {
                private int M()
                {
                    return NewProperty;
                }

                public int NewProperty { get; private set; }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            codeActionIndex: PropertyActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateProperty_FromImplicitExpression_WithoutCodeBlock()
    {
        var input = """
            @New[||]Property
            """;

        var expected = """
            @NewProperty
            @code {
                public string NewProperty { get; private set; }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            codeActionIndex: PropertyActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateProperty_Legacy_WithoutFunctionsBlock()
    {
        var input = """
            @{
                var value = [||]NewProperty;
            }
            """;

        var expected = """
            @{
                var value = NewProperty;
            }
            @functions {
                public object NewProperty { get; private set; }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            codeActionIndex: PropertyActionIndex,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }
}
