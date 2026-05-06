// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class AddUsingTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task FullyQualify()
    {
        var input = """
            @code
            {
                private [||]StringBuilder _x = new StringBuilder();
            }
            """;

        var expected = """
            @code
            {
                private System.Text.StringBuilder _x = new StringBuilder();
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.FullyQualify);
    }

    [Fact]
    public async Task FullyQualify_LegacyWithPageDirective()
    {
        var input = """
            @page "/"

            @functions
            {
                private [||]StringBuilder _x = new StringBuilder();
            }
            """;

        var expected = """
            @page "/"

            @functions
            {
                private System.Text.StringBuilder _x = new StringBuilder();
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            LanguageServerConstants.CodeActions.FullyQualify,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

#if !VSCODE
    // This uses a nested code action in Roslyn which we don't support in VS Code
    // https://github.com/dotnet/razor/issues/11832
    [Fact]
    public async Task FullyQualify_Multiple()
    {
        await VerifyCodeActionAsync(
            input: """
                @code
                {
                    private [||]StringBuilder _x = new StringBuilder();
                }
                """,
            expected: """
                @code
                {
                    private System.Text.StringBuilder _x = new StringBuilder();
                }
                """,
            additionalFiles: [
                (FilePath("StringBuilder.cs"), """
                    namespace Not.Built.In;

                    public class StringBuilder
                    {
                    }
                    """)],
            codeActionName: LanguageServerConstants.CodeActions.FullyQualify,
            childActionIndex: 0);
    }

    // This uses a nested code action in Roslyn which we don't support in VS Code
    // https://github.com/dotnet/razor/issues/11832
    [Fact]
    public async Task FullyQualify_Multiple2()
    {
        await VerifyCodeActionAsync(
            input: """
                @code
                {
                    private [||]StringBuilder _x = new StringBuilder();
                }
                """,
            expected: """
                @code
                {
                    private Not.Built.In.StringBuilder _x = new StringBuilder();
                }
                """,
            additionalFiles: [
                (FilePath("StringBuilder.cs"), """
                    namespace Not.Built.In;

                    public class StringBuilder
                    {
                    }
                    """)],
            codeActionName: LanguageServerConstants.CodeActions.FullyQualify,
            childActionIndex: 1);
    }
#endif

    [Fact]
    public async Task AddUsing_Multiple()
    {
        await VerifyCodeActionAsync(
            input: """
                @code
                {
                    private [||]StringBuilder _x = new StringBuilder();
                }
                """,
            expected: """
                @using Not.Built.In
                @code
                {
                    private StringBuilder _x = new StringBuilder();
                }
                """,
            additionalFiles: [
                (FilePath("StringBuilder.cs"), """
                    namespace Not.Built.In;

                    public class StringBuilder
                    {
                    }
                    """)],
            codeActionName: RazorPredefinedCodeFixProviderNames.AddImport,
            codeActionIndex: 0);
    }

    [Fact]
    public async Task AddUsing_Multiple2()
    {
        await VerifyCodeActionAsync(
            input: """
                @code
                {
                    private [||]StringBuilder _x = new StringBuilder();
                }
                """,
            expected: """
                @using System.Text
                @code
                {
                    private StringBuilder _x = new StringBuilder();
                }
                """,
            additionalFiles: [
                (FilePath("StringBuilder.cs"), """
                    namespace Not.Built.In;

                    public class StringBuilder
                    {
                    }
                    """)],
            codeActionName: RazorPredefinedCodeFixProviderNames.AddImport,
            codeActionIndex: 1);
    }

    [Fact]
    public async Task AddUsing()
    {
        var input = """
            @code
            {
                private [||]StringBuilder _x = new StringBuilder();
            }
            """;

        var expected = """
            @using System.Text
            @code
            {
                private StringBuilder _x = new StringBuilder();
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.AddImport);
    }

    [Fact]
    public async Task AddUsing_LegacyWithPageDirective()
    {
        var input = """
            @page "/"

            @functions
            {
                private [||]StringBuilder _x = new StringBuilder();
            }
            """;

        var expected = """
            @page "/"
            @using System.Text

            @functions
            {
                private StringBuilder _x = new StringBuilder();
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.AddImport,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task AddUsing_LegacyWithPageDirectiveAndTagHelpers_HtmlString()
    {
        var input = """
            @page "/"
            @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

            @{
             var z = new [||]HtmlString("asdf");
            }
            """;

        var expected = """
            @page "/"
            @using Microsoft.AspNetCore.Html
            @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

            @{
             var z = new HtmlString("asdf");
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.AddImport,
            fileKind: RazorFileKind.Legacy,
            addDefaultImports: false,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task AddUsing_LegacyWithPageDirectiveWithoutRouteAndTagHelpers_HtmlString()
    {
        var input = """
            @page
            @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

            @{
             var z = new [||]HtmlString("asdf");
            }
            """;

        var expected = """
            @page
            @using Microsoft.AspNetCore.Html
            @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

            @{
             var z = new HtmlString("asdf");
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeFixProviderNames.AddImport,
            fileKind: RazorFileKind.Legacy,
            addDefaultImports: false,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task AddUsing_Typo()
    {
        var input = """
            @code
            {
                private [||]Stringbuilder _x = new Stringbuilder();
            }
            """;

        var expected = """
            @using System.Text
            @code
            {
                private StringBuilder _x = new Stringbuilder();
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.AddImport);
    }

    [Fact]
    public async Task AddUsing_WithExisting()
    {
        var input = """
            @using System
            @using System.Collections.Generic

            @code
            {
                private [||]StringBuilder _x = new StringBuilder();
            }
            """;

        var expected = """
            @using System
            @using System.Collections.Generic
            @using System.Text

            @code
            {
                private StringBuilder _x = new StringBuilder();
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.AddImport);
    }
}
