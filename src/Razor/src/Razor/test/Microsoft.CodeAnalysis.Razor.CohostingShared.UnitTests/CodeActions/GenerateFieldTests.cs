// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateFieldTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    private const int FieldActionIndex = 0;
    private const int ReadonlyFieldActionIndex = 1;

    [Fact]
    public async Task GenerateField_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private int M()
                {
                    return [||]newField;
                }
            }
            """;

        var expected = """
            @code
            {
                private int M()
                {
                    return newField;
                }

                private int newField;
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            codeActionIndex: FieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateReadonlyField_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private int M()
                {
                    return [||]newField;
                }
            }
            """;

        var expected = """
            @code
            {
                private int M()
                {
                    return newField;
                }

                private readonly int newField;
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            codeActionIndex: ReadonlyFieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateField_FromImplicitExpression_WithoutCodeBlock()
    {
        var input = """
            @new[||]Field
            """;

        var expected = """
            @newField
            @code {
                private string newField;
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            codeActionIndex: FieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstField_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private void M(bool value = [||]newConstant)
                {
                }
            }
            """;

        var expected = """
            @code
            {
                private void M(bool value = newConstant)
                {
                }

                private const bool newConstant;
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateField_Legacy_WithoutFunctionsBlock()
    {
        var input = """
            @{
                var value = [||]newField;
            }
            """;

        var expected = """
            @{
                var value = newField;
            }
            @functions {
                private object newField;
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateVariable,
            codeActionIndex: FieldActionIndex,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }
}
