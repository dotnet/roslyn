// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateConstructorTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task GenerateConstructor_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private File1 Create(int value)
                {
                    return new [||]File1(value);
                }
            }
            """;

        var expected = """
            @code
            {
                private File1 Create(int value)
                {
                    return new File1(value);
                }

                private int value;

                public File1(int value)
                {
                    this.value = value;
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_ForClassInCodeBlock_WithoutParameter()
    {
        var input = """
            @code
            {
                private Goo Create()
                {
                    return new [||]Goo();
                }

                private class Goo
                {
                    public Goo(int value)
                    {
                    }
                }
            }
            """;

        var expected = """
            @code
            {
                private Goo Create()
                {
                    return new Goo();
                }

                private class Goo
                {
                    public Goo()
                    {
                    }

                    public Goo(int value)
                    {
                    }
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_WithoutCodeBlock()
    {
        var input = """
            @{
                var value = 1;
                var item = new [||]File1(value);
            }
            """;

        var expected = """
            @{
                var value = 1;
                var item = new File1(value);
            }
            @code {
                private int value;

                public File1(int value)
                {
                    this.value = value;
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_WithoutCodeBlock_WithoutParameter()
    {
        await VerifyCodeActionAsync(
            input: """
                @{
                    var item = new [||]File1();
                }
                """,
            expected: """
                @{
                    var item = new File1();
                }
                @code {
                    public File1()
                    {
                    }
                }
                """,
            additionalFiles: [
                (FilePath("File1.razor.cs"), """
                    namespace SomeProject;

                    public partial class File1
                    {
                        public File1(int value)
                        {
                        }
                    }
                    """)],
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateConstructor,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_ForClassInCodeBlock()
    {
        var input = """
            @code
            {
                private Goo Create(int value)
                {
                    return new [||]Goo(value);
                }

                private class Goo
                {
                }
            }
            """;

        var expected = """
            @code
            {
                private Goo Create(int value)
                {
                    return new Goo(value);
                }

                private class Goo
                {
                    private int value;

                    public Goo(int value)
                    {
                        this.value = value;
                    }
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_Legacy_WithoutFunctionsBlock()
    {
        var input = """
            @{
                var value = 1;
                var item = new [||]File1(value);
            }
            """;

        var expected = """
            @{
                var value = 1;
                var item = new File1(value);
            }
            @functions {
                private int value;

                public File1(int value)
                {
                    this.value = value;
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }
}
