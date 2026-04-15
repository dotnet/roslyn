// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
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

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_AutoDetectedFromInherits()
    {
        // Arrange
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyUtf8PageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyUtf8PageBase : RazorPage
                    {
                        public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                        {
                            WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        result.VerifyOutputsMatchBaseline();
        Assert.Contains("u8)", result.GeneratedSources[0].SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_WithoutOverload_UsesStringLiterals()
    {
        // Arrange
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyPageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyPageBase : RazorPage
                    {
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        result.VerifyOutputsMatchBaseline();
        Assert.DoesNotContain("u8)", result.GeneratedSources[0].SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_MixedFiles_OnlyUtf8ForInheritsWithOverload()
    {
        // Arrange - one file inherits a base with the overload, the other inherits one without
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Utf8Page.cshtml"] = """
                    @inherits MyUtf8PageBase
                    <h1>UTF-8 Page</h1>
                    """,
                ["Pages/RegularPage.cshtml"] = """
                    @inherits MyRegularPageBase
                    <h1>Regular Page</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyUtf8PageBase : RazorPage
                    {
                        public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                        {
                            WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                        }
                    }
                    """,
                ["MyRegularPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyRegularPageBase : RazorPage
                    {
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedSources.Length);

        var utf8Source = result.GeneratedSources.Single(s => s.HintName.Contains("Utf8Page")).SourceText.ToString();
        var regularSource = result.GeneratedSources.Single(s => s.HintName.Contains("RegularPage")).SourceText.ToString();

        Assert.Contains("u8)", utf8Source);
        Assert.DoesNotContain("u8)", regularSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_SwitchesWhenOverloadAddedOrRemoved()
    {
        // Arrange - start without the UTF-8 overload
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyPageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyPageBase : RazorPage
                    {
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act 1 - verify string literals are used
        var result = RunGenerator(compilation!, ref driver, out _);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain("u8)", result.GeneratedSources[0].SourceText.ToString());

        // Act 2 - add the UTF-8 overload to the base class
        var baseClassDoc = project.Documents.Single(d => d.Name == "MyPageBase.cs");
        project = project.RemoveDocument(baseClassDoc.Id)
            .AddDocument("MyPageBase.cs", SourceText.From("""
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                public abstract class MyPageBase : RazorPage
                {
                    public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                    {
                        WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                    }
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        result = RunGenerator(compilation!, ref driver, out _);

        // Assert - should now use UTF-8 literals
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.Contains("u8)", result.GeneratedSources[0].SourceText.ToString());

        // Act 3 - remove the UTF-8 overload from the base class
        baseClassDoc = project.Documents.Single(d => d.Name == "MyPageBase.cs");
        project = project.RemoveDocument(baseClassDoc.Id)
            .AddDocument("MyPageBase.cs", SourceText.From("""
                using Microsoft.AspNetCore.Mvc.Razor;

                public abstract class MyPageBase : RazorPage
                {
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        result = RunGenerator(compilation!, ref driver, out _);

        // Assert - should switch back to string literals
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain("u8)", result.GeneratedSources[0].SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_NoInheritsDirective_UsesStringLiterals()
    {
        // Arrange - no @inherits directive, default base class
        var project = CreateTestProject(new()
        {
            ["Pages/Index.cshtml"] = """
                @page
                <h1>Hello World</h1>
                """,
        });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert - default base classes don't support UTF-8 WriteLiteral
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain("u8)", result.GeneratedSources[0].SourceText.ToString());
    }
}
