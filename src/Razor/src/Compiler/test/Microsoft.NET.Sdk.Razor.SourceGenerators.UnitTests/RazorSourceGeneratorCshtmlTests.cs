// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators;

public sealed class RazorSourceGeneratorCshtmlTests : RazorSourceGeneratorTestsBase
{
    [Fact, WorkItem("https://github.com/dotnet/razor/issues/9034")]
    public async Task CssScoping()
    {
        // Arrange
        var project = CreateTestProject(new()
        {
            ["Pages/Index.cshtml"] = """
                @page
                <h1>heading</h1>
                <header>header</header>
                <thead>table head</thead>
                <form>
                    <select>
                        <option>choose</option>
                    </select>
                </form>
                < div />
                <div
                >multiline</div>
                <!div>bang</div>
                <input multiple value="test" disabled="@("<input".Length > 0)" />
                <div></div>
                @* <div>Razor comment</div> *@
                <!-- <div>HTML comment</div> -->
                @{
                    <div>code block</div>
                    //<div>C# comment</div>
                    Func<dynamic, object> template = @<div>C# template</div>;
                    string x = "attr";
                }
                @template(null!)
                <div @x="1"></div>
                <div@x="1"></div>

                Ignored:
                - <head />
                - <meta />
                - <title />
                - <link />
                - <base />
                - <script />
                - <style />
                - <html />
                """,
        });
        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project, options =>
        {
            options.AdditionalTextOptions["Pages/Index.cshtml"]["build_metadata.AdditionalFiles.CssScope"] = "test-css-scope";
        });

        // Act
        var result = RunGenerator(compilation!, ref driver, out compilation);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        await VerifyRazorPageMatchesBaselineAsync(compilation, "Pages_Index");
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public async Task ConditionalAttributes([CombinatorialValues("@null", "@n")] string value)
    {
        // Arrange
        var project = CreateTestProject(new()
        {
            ["Pages/Index.cshtml"] = $$"""
                @page
                @{ var s = "str"; string? n = null; }
                <div class="@s" style="{{value}}">x</div>
                <div @s style="{{value}}">x</div>
                <div style="{{value}}" class="@s">x</div>
                <div style="{{value}}" @s>x</div>
                <div @s style="{{value}}" @s>x</div>

                <div @* comment *@ style="{{value}}">x</div>
                <div style="{{value}}" @* comment *@>x</div>
                
                <div @* comment *@ @s style="{{value}}">x</div>
                
                <div @(s + s) style="{{value}}">x</div>
                <div @{if (s.Length != 0) { @s } } style="{{value}}">x</div>
                <div @@s style="{{value}}">x</div>

                <div @s {{value}}>x</div>
                <div {{value}} @s>x</div>
                """,
        });
        var compilation = await project.GetCompilationAsync();
        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions([.. compilation.Options.SpecificDiagnosticOptions,
            // warning CS0219: The variable 'n' is assigned but its value is never used
            new("CS0219", ReportDiagnostic.Suppress)]));
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out compilation);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        var html = await VerifyRazorPageMatchesBaselineAsync(compilation, "Pages_Index");

        // The style attribute should not be rendered at all.
        Assert.DoesNotContain("style", html);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11518")]
    public async Task OpenAngle()
    {
        // Arrange
        var project = CreateTestProject(new()
        {
            ["Pages/Index.cshtml"] = """
                < @("a").@("b")
                """,
        });
        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out compilation);

        // Assert
        result.Diagnostics.Verify();
        Assert.Single(result.GeneratedSources);
        await VerifyRazorPageMatchesBaselineAsync(compilation, "Pages_Index");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11327")]
    public async Task QuoteInAttributeName()
    {
        // Arrange
        var project = CreateTestProject(new()
        {
            ["Pages/Index.cshtml"] = """
                <div class="test"">
                    <img src="~/test">
                </div>
                """,
        });
        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out compilation);

        // Assert
        result.Diagnostics.Verify();
        Assert.Single(result.GeneratedSources);
        var html = await VerifyRazorPageMatchesBaselineAsync(compilation, "Pages_Index");

        // The img tag helper should rewrite `~/` to `/`.
        Assert.Contains("""
            <img src="/test">
            """, html);
    }
}
