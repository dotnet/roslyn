// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Settings;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateMethodTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task GenerateMethod_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private void M()
                {
                    [||]NewMethod();
                }
            }
            """;

        var expected = """
            @using System
            @code
            {
                private void M()
                {
                    NewMethod();
                }

                private void NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromCodeBlock_ExistingCodeBlock_WithParameter()
    {
        var input = """
            @code
            {
                private void M()
                {
                    var value = 1;
                    [||]NewMethod(value);
                }
            }
            """;

        var expected = """
            @using System
            @code
            {
                private void M()
                {
                    var value = 1;
                    NewMethod(value);
                }

                private void NewMethod(int value)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromCodeBlock_ExistingCodeBlock_ExpressionBodiedMethod1()
    {
        var input = """
            @code
            {
                private void M() =>
                    [||]NewMethod();
            }
            """;

        var expected = """
            @using System
            @code
            {
                private void M() =>
                    NewMethod();

                private void NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromCodeBlock_ExistingCodeBlock_ExpressionBodiedMethod2()
    {
        var input = """
            @code
            {
                private void M()
                    => [||]NewMethod();
            }
            """;

        var expected = """
            @using System
            @code
            {
                private void M()
                    => NewMethod();

                private void NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromImplicitExpression_ExistingCodeBlock_WithParameter()
    {
        var input = """
            @New[||]Method(Value)

            @code
            {
                private string Value { get; } = "Hello";
            }
            """;

        var expected = """
            @using System
            @NewMethod(Value)

            @code
            {
                private string Value { get; } = "Hello";

                private string NewMethod(string value)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromImplicitExpression_WithoutCodeBlock()
    {
        var input = """
            @New[||]Method()
            """;

        var expected = """
            @using System
            @NewMethod()
            @code {
                private string NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromImplicitExpression_WithoutCodeBlock_CodeBlockBraceOnNextLine()
    {
        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { CodeBlockBraceOnNextLine = true });

        var input = """
            @New[||]Method()
            """;

        var expected = """
            @using System
            @NewMethod()
            @code
            {
                private string NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromImplicitExpression_WithoutCodeBlock_UsesTabsWhenConfigured()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 4));

        var input = """
            @New[||]Method()
            """;

        var expected = """
            @using System
            @NewMethod()
            @code {
            	private string NewMethod()
            	{
            		throw new NotImplementedException();
            	}
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromImplicitExpression_EmptyCodeBlock()
    {
        var input = """
            @New[||]Method()

            @code
            {
            }
            """;

        var expected = """
            @using System
            @NewMethod()

            @code
            {
                private string NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromImplicitExpression_EmptyCodeBlock_UsesConfiguredIndentSize()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: false, IndentSize: 2));

        var input = """
            @New[||]Method()

            @code
            {
            }
            """;

        var expected = """
            @using System
            @NewMethod()

            @code
            {
              private string NewMethod()
              {
                throw new NotImplementedException();
              }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromImplicitExpression_SingleLineCodeBlock()
    {
        var input = """
            @New[||]Method()

            @code { }
            """;

        var expected = """
            @using System
            @NewMethod()

            @code {
                private string NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromCodeBlock_ExistingCodeBlock_WithBlankLineBeforeCloseBrace()
    {
        var input = """
            @code
            {
                private void M()
                {
                    [||]NewMethod();
                }

            }
            """;

        var expected = """
            @using System
            @code
            {
                private void M()
                {
                    NewMethod();
                }

                private void NewMethod()
                {
                    throw new NotImplementedException();
                }

            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_FromCodeBlock_MultipleCodeBlocks_UsesFirstCodeBlock()
    {
        var input = """
            @[||]NewMethod()

            @code { }

            @code { }
            """;

        var expected = """
            @using System
            @NewMethod()

            @code {
                private string NewMethod()
                {
                    throw new NotImplementedException();
                }
            }

            @code {}
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public async Task GenerateMethod_Legacy_WithoutFunctionsBlock_CodeBlockBraceOnNextLine()
    {
        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { CodeBlockBraceOnNextLine = true });

        var input = """
            @{
                [||]NewMethod();
            }
            """;

        var expected = """
            @{
                NewMethod();
            }
            @functions
            {
                private void NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod, fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task GenerateMethod_Legacy_WithoutFunctionsBlock()
    {
        var input = """
            @{
                [||]NewMethod();
            }
            """;

        var expected = """
            @{
                NewMethod();
            }
            @functions {
                private void NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod, fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task GenerateMethod_Legacy_WithFunctionsBlock()
    {
        var input = """
            @functions
            {
                private void M()
                {
                    [||]NewMethod();
                }
            }
            """;

        var expected = """
            @functions
            {
                private void M()
                {
                    NewMethod();
                }

                private void NewMethod()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod, fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task GenerateMethod_Legacy_MultipleFunctionsBlocks_UsesFirstFunctionsBlock()
    {
        var input = """
            @[||]NewMethod()

            @functions { }

            @functions { }
            """;

        var expected = """
            @NewMethod()

            @functions {
                private object NewMethod()
                {
                    throw new NotImplementedException();
                }
            }

            @functions { }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.GenerateMethod, fileKind: RazorFileKind.Legacy);
    }
}
