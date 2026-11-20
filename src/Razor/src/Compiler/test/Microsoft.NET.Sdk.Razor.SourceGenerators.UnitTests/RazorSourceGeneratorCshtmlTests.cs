// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
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
        driver = await GetDriverAsync(project);
        result = RunGenerator(compilation!, ref driver, out _, _ => { });

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
        driver = await GetDriverAsync(project);
        result = RunGenerator(compilation!, ref driver, out _, _ => { });

        // Assert - should switch back to string literals
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain("u8)", result.GeneratedSources[0].SourceText.ToString());
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3050748")]
    public async Task Utf8HtmlLiterals_IncrementalUpdate_DoesNotDuplicateComponentTypeInferenceMethods()
    {
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Components/Page.razor"] = """
                    @using MyApp
                    <GenericComponent Value="42" />
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits MyApp.MyPageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["GenericComponent.cs"] = """
                    using Microsoft.AspNetCore.Components;

                    namespace MyApp;

                    public sealed class GenericComponent<T> : ComponentBase
                    {
                        [Parameter]
                        public T Value { get; set; } = default!;
                    }
                    """,
                ["MyPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp;

                    public abstract class MyPageBase : RazorPage
                    {
                    }
                    """,
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        var result = RunGenerator(compilation!, ref driver);
        var originalComponentSource = result.GeneratedSources
            .Single(s => s.HintName.EndsWith("Page_razor.g.cs", StringComparison.Ordinal))
            .SourceText;

        var baseClassDocument = project.Documents.Single(d => d.Name == "MyPageBase.cs");
        project = baseClassDocument.WithText(SourceText.From("""
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace MyApp;

                public abstract class MyPageBase : RazorPage
                {
                    public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                    {
                        WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                    }
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        result = RunGenerator(compilation!, ref driver);

        var updatedComponentSource = result.GeneratedSources
            .Single(s => s.HintName.EndsWith("Page_razor.g.cs", StringComparison.Ordinal))
            .SourceText;

        Assert.Equal(originalComponentSource.ToString(), updatedComponentSource.ToString());
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3050748")]
    public async Task IncrementalUpdate_WhenInheritsTypeChanges_DoesNotDuplicateComponentTypeInferenceMethods()
    {
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Components/Page.razor"] = """
                    @using MyApp
                    <GenericComponent Value="42" />
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits PageBaseAlias
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["GenericComponent.cs"] = """
                    using Microsoft.AspNetCore.Components;

                    namespace MyApp;

                    public sealed class GenericComponent<T> : ComponentBase
                    {
                        [Parameter]
                        public T Value { get; set; } = default!;
                    }
                    """,
                ["PageBases.cs"] = """
                    global using PageBaseAlias = MyApp.FirstPageBase;

                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp;

                    public abstract class FirstPageBase : RazorPage
                    {
                    }

                    public abstract class SecondPageBase : RazorPage
                    {
                    }
                    """,
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        var result = RunGenerator(compilation!, ref driver);
        var originalComponentSource = result.GeneratedSources
            .Single(s => s.HintName.EndsWith("Page_razor.g.cs", StringComparison.Ordinal))
            .SourceText;

        var pageBasesDocument = project.Documents.Single(d => d.Name == "PageBases.cs");
        project = pageBasesDocument.WithText(SourceText.From("""
                global using PageBaseAlias = MyApp.SecondPageBase;

                using Microsoft.AspNetCore.Mvc.Razor;

                namespace MyApp;

                public abstract class FirstPageBase : RazorPage
                {
                }

                public abstract class SecondPageBase : RazorPage
                {
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        result = RunGenerator(compilation!, ref driver);

        var updatedComponentSource = result.GeneratedSources
            .Single(s => s.HintName.EndsWith("Page_razor.g.cs", StringComparison.Ordinal))
            .SourceText;

        Assert.Equal(originalComponentSource.ToString(), updatedComponentSource.ToString());
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3052471")]
    public async Task Utf8HtmlLiterals_IncrementalUpdate_DoesNotDuplicateMetadataAttributes()
    {
        // A change to one .cshtml's @inherits base type flips the project-wide UTF-8 support map,
        // which re-runs code generation for an unrelated bystander .cshtml. That bystander's
        // [assembly: RazorCompiledItem(...)] metadata attribute (appended by an optimization pass)
        // must not be duplicated by the re-run.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Bystander.cshtml"] = """
                    @inherits MyApp.MyPageBase
                    <h1>Bystander</h1>
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits MyApp.MyPageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp;

                    public abstract class MyPageBase : RazorPage
                    {
                    }
                    """,
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        var result = RunGenerator(compilation!, ref driver, out _, _ => { });
        var originalBystanderSource = result.GeneratedSources
            .Single(s => s.HintName.Contains("Bystander"))
            .SourceText.ToString();
        Assert.Equal(1, CountOccurrences(originalBystanderSource, "RazorCompiledItemAttribute("));

        var baseClassDocument = project.Documents.Single(d => d.Name == "MyPageBase.cs");
        project = baseClassDocument.WithText(SourceText.From("""
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace MyApp;

                public abstract class MyPageBase : RazorPage
                {
                    public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                    {
                        WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                    }
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        result = RunGenerator(compilation!, ref driver, out _, _ => { });

        var updatedBystanderSource = result.GeneratedSources
            .Single(s => s.HintName.Contains("Bystander"))
            .SourceText.ToString();
        Assert.Equal(1, CountOccurrences(updatedBystanderSource, "RazorCompiledItemAttribute("));

        static int CountOccurrences(string text, string value)
        {
            var count = 0;
            for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
            {
                count++;
            }
            return count;
        }
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3050748")]
    public async Task Utf8HtmlLiterals_IncrementalUpdate_SwitchesBackToStringLiteralsWhenOverloadRemoved()
    {
        // The same driver is reused across runs, and the UTF-8 overload is removed by editing the
        // base class's .cs file rather than the .cshtml. The .cshtml document is therefore unchanged
        // and its parsed/optimized output is cached; only the UTF-8 support map changes. Emission
        // must recompute the decision so support that disappears turns byte literals back off.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyApp.MyPageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyPageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp;

                    public abstract class MyPageBase : RazorPage
                    {
                        public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                        {
                            WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                        }
                    }
                    """,
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        var result = RunGenerator(compilation!, ref driver, out _, _ => { });
        Assert.Contains("u8)", result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString());

        var baseClassDocument = project.Documents.Single(d => d.Name == "MyPageBase.cs");
        project = baseClassDocument.WithText(SourceText.From("""
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace MyApp;

                public abstract class MyPageBase : RazorPage
                {
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        result = RunGenerator(compilation!, ref driver, out _, _ => { });

        Assert.DoesNotContain("u8)", result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString());
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3050748")]
    public async Task Utf8HtmlLiterals_IncrementalUpdate_OnlyReRunsEmissionNotOptimization()
    {
        // Adding the UTF-8 overload edits a .cs source, not the .cshtml, so the document's parsed and
        // optimized intermediate tree is unaffected and only the project-wide UTF-8 support map changes.
        // The optimization step must stay cached (replaying it is what duplicated appended nodes); only
        // read-only C# emission re-runs.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyApp.MyPageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp;

                    public abstract class MyPageBase : RazorPage
                    {
                    }
                    """,
            });

        var compilation = await project.GetCompilationAsync();
        var (driver, _, _) = await GetDriverWithAdditionalTextAndProviderAsync(project, trackSteps: true);

        var result = RunGenerator(compilation!, ref driver, out _, _ => { });
        Assert.DoesNotContain("u8)", result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString());

        var baseClassDocument = project.Documents.Single(d => d.Name == "MyPageBase.cs");
        project = baseClassDocument.WithText(SourceText.From("""
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace MyApp;

                public abstract class MyPageBase : RazorPage
                {
                    public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                    {
                        WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                    }
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        result = RunGenerator(compilation!, ref driver, out _, _ => { });
        Assert.Contains("u8)", result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString());

        // The optimized intermediate tree is reused; the UTF-8 decision flips, so only C# emission
        // re-runs.
        result.VerifyIncrementalSteps("OptimizedDocuments", IncrementalStepRunReason.Cached);
        result.VerifyIncrementalSteps("Utf8Documents", IncrementalStepRunReason.Modified);
        result.VerifyIncrementalSteps("GeneratedCode", IncrementalStepRunReason.Modified);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3050748")]
    public async Task Utf8HtmlLiterals_IncrementalUpdate_DoesNotReEmitUnaffectedComponent()
    {
        // Editing the .cshtml base class flips only that page's UTF-8 decision. An unrelated generic
        // component's optimized tree and UTF-8 flag are unchanged, so its C# emission stays cached --
        // it is neither re-lowered (which duplicated its type-inference methods) nor re-emitted.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Components/Page.razor"] = """
                    @using MyApp
                    <GenericComponent Value="42" />
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits MyApp.MyPageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["GenericComponent.cs"] = """
                    using Microsoft.AspNetCore.Components;

                    namespace MyApp;

                    public sealed class GenericComponent<T> : ComponentBase
                    {
                        [Parameter]
                        public T Value { get; set; } = default!;
                    }
                    """,
                ["MyPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp;

                    public abstract class MyPageBase : RazorPage
                    {
                    }
                    """,
            });

        var compilation = await project.GetCompilationAsync();
        var (driver, _, _) = await GetDriverWithAdditionalTextAndProviderAsync(project, trackSteps: true);

        RunGenerator(compilation!, ref driver, out _, _ => { });

        var baseClassDocument = project.Documents.Single(d => d.Name == "MyPageBase.cs");
        project = baseClassDocument.WithText(SourceText.From("""
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace MyApp;

                public abstract class MyPageBase : RazorPage
                {
                    public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                    {
                        WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                    }
                }
                """, Encoding.UTF8)).Project;

        compilation = await project.GetCompilationAsync();
        var result = RunGenerator(compilation!, ref driver, out _, _ => { });

        // The .cshtml re-emits (its UTF-8 decision flipped); the unrelated component stays cached.
        // Key the run reason by source file path so we verify *which* document re-emitted -- the
        // "GeneratedCode" step output value is a (filePath, document) tuple; read the first element
        // via ITuple to avoid naming the internal document type.
        var reasonsByFile = result.TrackedSteps["GeneratedCode"]
            .SelectMany(step => step.Outputs)
            .ToDictionary(
                output => (string)((System.Runtime.CompilerServices.ITuple)output.Value)[0]!,
                output => output.Reason);

        Assert.Equal(2, reasonsByFile.Count);
        var componentReason = reasonsByFile.Single(kvp => kvp.Key.EndsWith(".razor", StringComparison.Ordinal)).Value;
        var pageReason = reasonsByFile.Single(kvp => kvp.Key.EndsWith(".cshtml", StringComparison.Ordinal)).Value;
        Assert.NotEqual(IncrementalStepRunReason.Modified, componentReason);
        Assert.Equal(IncrementalStepRunReason.Modified, pageReason);
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

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_FullyQualifiedInherits()
    {
        // Arrange - @inherits with fully-qualified type name
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyApp.Infrastructure.MyUtf8PageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert - fully-qualified name resolves correctly
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.Contains("u8)", result.GeneratedSources[0].SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_ShortNameInherits_WithUsing()
    {
        // Arrange - @inherits with short name, namespace imported via _ViewImports
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @using MyApp.Infrastructure
                    """,
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

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert - short name resolves via augmented compilation with the @using directives
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_PartiallyQualifiedInherits()
    {
        // Arrange - @inherits with partially-qualified name
        // Note: Razor requires fully-qualified names in @inherits, so this produces a compile error.
        // This test documents that we gracefully handle this case (no UTF-8 detection).
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits Infrastructure.MyUtf8PageBase
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _, _ => { });

        // Assert - partially-qualified name won't match the metadata name.
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.DoesNotContain("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_AliasedInherits_WithUsing()
    {
        // Arrange - @inherits with a type alias defined via @using alias in _ViewImports
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @using Utf8Base = MyApp.Infrastructure.MyUtf8PageBase
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits Utf8Base
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert - alias resolves via augmented compilation
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_AliasShadowsExistingType_GracefulFallback()
    {
        // Arrange - "MyPageBase" exists as a non-UTF-8 type in the global namespace.
        // AliasedPage uses "@using MyPageBase = MyApp.Infrastructure.MyUtf8PageBase"
        // which creates a C# compile error (CS0576: definition conflicting with alias).
        // This test verifies we handle this gracefully — no crash, falls back to string literals.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/RegularPage.cshtml"] = """
                    @inherits MyPageBase
                    <h1>Regular Page</h1>
                    """,
                ["Pages/AliasedPage.cshtml"] = """
                    @using MyPageBase = MyApp.Infrastructure.MyUtf8PageBase
                    @inherits MyPageBase
                    <h1>Aliased Page</h1>
                    """,
            },
            sources: new()
            {
                ["MyPageBase.cs"] = """
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyPageBase : RazorPage
                    {
                    }
                    """,
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert - no generator crash. The alias conflicts with the global type (CS0576),
        // so both files fall back to string literals. This is correct behavior since the
        // user's code has a compile error.
        Assert.Empty(result.Diagnostics);

        var regularSource = result.GeneratedSources.Single(s => s.HintName.Contains("RegularPage")).SourceText.ToString();
        var aliasedSource = result.GeneratedSources.Single(s => s.HintName.Contains("AliasedPage")).SourceText.ToString();

        Assert.DoesNotContain("u8)", regularSource);
        Assert.DoesNotContain("u8)", aliasedSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_AddingUsingMakesShortNameResolve()
    {
        // Arrange - @inherits uses a short name that doesn't resolve without a @using
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

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var (driver, additionalTexts, optionsProvider) = await GetDriverWithAdditionalTextAndProviderAsync(project);

        // Act 1 - short name doesn't resolve, falls back to string literals
        var result = RunGenerator(compilation!, ref driver, out _, _ => { });

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain("u8)", result.GeneratedSources[0].SourceText.ToString());

        // Act 2 - add a _ViewImports with @using that makes the short name resolve
        var viewImports = new TestAdditionalText("Pages/_ViewImports.cshtml",
            SourceText.From("@using MyApp.Infrastructure", Encoding.UTF8));
        driver = driver.AddAdditionalTexts([viewImports]);
        optionsProvider.AdditionalTextOptions[viewImports.Path] = new TestAnalyzerConfigOptions
        {
            ["build_metadata.AdditionalFiles.TargetPath"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(viewImports.Path))
        };
        driver = driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);

        result = RunGenerator(compilation!, ref driver, out _, _ => { });

        // Assert - now the short name resolves and UTF-8 is used
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_InheritsFromViewImports()
    {
        // Arrange - @inherits is in _ViewImports.cshtml, not in the page itself
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @inherits MyUtf8PageBase
                    """,
                ["Pages/Index.cshtml"] = """
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

        // Assert - @inherits from _ViewImports should be detected and UTF-8 used
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_InheritsFromViewImports_ShortNameWithUsing()
    {
        // Arrange - @inherits with short name + @using both in _ViewImports.cshtml.
        // The page itself has neither directive.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @using MyApp.Infrastructure
                    @inherits MyUtf8PageBase
                    """,
                ["Pages/Index.cshtml"] = """
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert - short name resolved via @using in _ViewImports should enable UTF-8
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_CascadingViewImports_MostSpecificWins()
    {
        // Arrange - root _ViewImports has @inherits with UTF-8 support,
        // but Pages/_ViewImports overrides with a base that does NOT support UTF-8.
        // A page in Pages/Admin/ has its own @inherits that DOES support UTF-8.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["_ViewImports.cshtml"] = """
                    @inherits MyUtf8PageBase
                    """,
                ["Pages/_ViewImports.cshtml"] = """
                    @inherits MyRegularPageBase
                    """,
                ["Pages/Index.cshtml"] = """
                    <h1>Hello</h1>
                    """,
                ["Pages/Admin/Dashboard.cshtml"] = """
                    @inherits MyUtf8PageBase
                    <h1>Hello</h1>
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

        // Pages/Index.cshtml inherits MyRegularPageBase (from Pages/_ViewImports) - no UTF-8
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.DoesNotContain("u8)", indexSource);

        // Pages/Admin/Dashboard.cshtml has its own @inherits MyUtf8PageBase - UTF-8
        var dashboardSource = result.GeneratedSources.Single(s => s.HintName.Contains("Dashboard")).SourceText.ToString();
        Assert.Contains("u8)", dashboardSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_GenericBaseClass()
    {
        // Arrange - @inherits uses a generic base class that has the UTF-8 overload
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyUtf8PageBase<MyModel>
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyModel.cs"] = """
                    public class MyModel
                    {
                        public string Name { get; set; } = "";
                    }
                    """,
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyUtf8PageBase<TModel> : RazorPage<TModel>
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

        // Assert - generic base class with UTF-8 overload should be detected
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_NonGenericBaseClass_ResolvedViaCSharpGlobalUsing()
    {
        // Arrange - @inherits uses a short name with no @using in Razor.
        // The namespace is imported via a C# global using (e.g. from GlobalUsings.cs).
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
                ["GlobalUsings.cs"] = """
                    global using MyApp.Infrastructure;
                    """,
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
                        }
                    }
                    """
            });

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert - should detect UTF-8 even though the using comes from C# not Razor
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_GenericBaseClass_InNamespace()
    {
        // Arrange - @inherits uses a generic base class in a namespace, resolved via @using
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @using MyApp.Infrastructure
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits MyUtf8PageBase<MyModel>
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyModel.cs"] = """
                    public class MyModel { }
                    """,
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase<TModel> : RazorPage<TModel>
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
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
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_GenericBaseClass_NestedInGenericClass()
    {
        // Arrange - @inherits uses a generic class nested inside another generic class in a namespace
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @using MyApp.Infrastructure
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits PageFactory<MyModel>.Utf8Page
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyModel.cs"] = """
                    public class MyModel { }
                    """,
                ["PageFactory.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public class PageFactory<TModel>
                        {
                            public abstract class Utf8Page : RazorPage<TModel>
                            {
                                public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                                {
                                    WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                                }
                            }
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
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_GenericBaseClass_MultipleTypeParameters()
    {
        // Arrange - @inherits uses a generic base class with multiple type parameters
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyUtf8PageBase<string, int>
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyUtf8PageBase<TModel, TKey> : RazorPage<TModel>
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
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_GenericBaseClass_FromMetadataReference()
    {
        // Arrange - the generic base class comes from a pre-compiled assembly (metadata reference),
        // not from source in the same compilation.
        var baseProject = CreateTestProject(new() { ["_dummy.cshtml"] = "" }, sources: new()
        {
            ["MyUtf8PageBase.cs"] = """
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace ExternalLib
                {
                    public abstract class MyUtf8PageBase<TModel> : RazorPage<TModel>
                    {
                        public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                        {
                            WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                        }
                    }
                }
                """
        });

        var baseCompilation = await baseProject.GetCompilationAsync();
        using var peStream = new MemoryStream();
        var emitResult = baseCompilation!.Emit(peStream);
        Assert.True(emitResult.Success);
        peStream.Position = 0;
        var metadataRef = MetadataReference.CreateFromStream(peStream);

        // Now create the actual test project that references the pre-compiled assembly
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @using ExternalLib
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits MyUtf8PageBase<string>
                    <h1>Hello World</h1>
                    """,
            });

        project = project.AddMetadataReference(metadataRef);

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_GenericBaseClass_WithTModel_AndModelDirective()
    {
        // Arrange - the common MVC pattern: @inherits SomeBase<TModel> + @model SomeType.
        // ModelDirective.Pass substitutes TModel with the @model type during code generation,
        // but the support map probes the raw @inherits text. The probe must still bind
        // SomeBase<TModel> by aliasing TModel to a known type.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits MyUtf8PageBase<TModel>
                    @model MyModel
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyModel.cs"] = """
                    public class MyModel
                    {
                        public string Name { get; set; } = "";
                    }
                    """,
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    public abstract class MyUtf8PageBase<TModel> : RazorPage<TModel>
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

        // Assert - even though the raw @inherits text contains the unresolved `TModel`,
        // the probe should still resolve the open generic and detect the UTF-8 overload.
        Assert.Empty(result.Diagnostics);
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_LanguageVersionBelowCSharp11_UsesStringLiterals()
    {
        // Arrange - project pinned to C# 10 (no UTF-8 string literal support). Even when the
        // base class exposes WriteLiteral(ReadOnlySpan<byte>), emitting "..."u8 would produce
        // uncompilable code, so the generator must fall back to plain string literals.
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
            },
            cSharpParseOptions: new CSharpParseOptions(LanguageVersion.CSharp10));

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain("u8)", result.GeneratedSources[0].SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_PrivateOverloadOnReferencedAssembly_NotDetected()
    {
        // Arrange - the WriteLiteral(ReadOnlySpan<byte>) overload is `private` on the base.
        // A generated subclass cannot call a private method on its base, so the page must
        // fall back to plain string literals (otherwise the generated code won't compile).
        var baseProject = CreateTestProject(new() { ["_dummy.cshtml"] = "" }, sources: new()
        {
            ["MyUtf8PageBase.cs"] = """
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace ExternalLib
                {
                    public abstract class MyUtf8PageBase : RazorPage
                    {
                        private void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                        {
                            WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                        }
                    }
                }
                """
        });

        var baseCompilation = await baseProject.GetCompilationAsync();
        using var peStream = new MemoryStream();
        var emitResult = baseCompilation!.Emit(peStream);
        Assert.True(emitResult.Success);
        peStream.Position = 0;
        var metadataRef = MetadataReference.CreateFromStream(peStream);

        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits ExternalLib.MyUtf8PageBase
                    <h1>Hello World</h1>
                    """,
            });
        project = project.AddMetadataReference(metadataRef);

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain("u8)", result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_InternalOverloadOnReferencedAssembly_NotDetected()
    {
        // Arrange - the WriteLiteral(ReadOnlySpan<byte>) overload is `internal` on a base
        // type that lives in a *different* assembly with no InternalsVisibleTo. The generated
        // subclass in our compilation cannot call it, so we must fall back to string literals.
        var baseProject = CreateTestProject(new() { ["_dummy.cshtml"] = "" }, sources: new()
        {
            ["MyUtf8PageBase.cs"] = """
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace ExternalLib
                {
                    public abstract class MyUtf8PageBase : RazorPage
                    {
                        internal void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                        {
                            WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                        }
                    }
                }
                """
        });

        var baseCompilation = await baseProject.GetCompilationAsync();
        using var peStream = new MemoryStream();
        var emitResult = baseCompilation!.Emit(peStream);
        Assert.True(emitResult.Success);
        peStream.Position = 0;
        var metadataRef = MetadataReference.CreateFromStream(peStream);

        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits ExternalLib.MyUtf8PageBase
                    <h1>Hello World</h1>
                    """,
            });
        project = project.AddMetadataReference(metadataRef);

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain("u8)", result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_ProtectedOverload_Detected()
    {
        // Arrange - `protected` WriteLiteral overload should still be callable from the
        // generated subclass (regardless of which assembly the base is in).
        var baseProject = CreateTestProject(new() { ["_dummy.cshtml"] = "" }, sources: new()
        {
            ["MyUtf8PageBase.cs"] = """
                using System;
                using Microsoft.AspNetCore.Mvc.Razor;

                namespace ExternalLib
                {
                    public abstract class MyUtf8PageBase : RazorPage
                    {
                        protected void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                        {
                            WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                        }
                    }
                }
                """
        });

        var baseCompilation = await baseProject.GetCompilationAsync();
        using var peStream = new MemoryStream();
        var emitResult = baseCompilation!.Emit(peStream);
        Assert.True(emitResult.Success);
        peStream.Position = 0;
        var metadataRef = MetadataReference.CreateFromStream(peStream);

        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/Index.cshtml"] = """
                    @inherits ExternalLib.MyUtf8PageBase
                    <h1>Hello World</h1>
                    """,
            });
        project = project.AddMetadataReference(metadataRef);

        var compilation = await project.GetCompilationAsync();
        var driver = await GetDriverAsync(project);

        // Act
        var result = RunGenerator(compilation!, ref driver, out _);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Contains("u8)", result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public async Task Utf8HtmlLiterals_AliasFromImports_ResolvesInProbe()
    {
        // Arrange - the page's @inherits depends on a `using Foo = ...` alias defined in
        // _ViewImports.cshtml. The probe compilation must see the import's usings before
        // the page's so that the alias is in scope when binding the @inherits text.
        var project = CreateTestProject(
            additionalSources: new()
            {
                ["Pages/_ViewImports.cshtml"] = """
                    @using MyAlias = MyApp.Infrastructure.MyUtf8PageBase
                    """,
                ["Pages/Index.cshtml"] = """
                    @inherits MyAlias
                    <h1>Hello World</h1>
                    """,
            },
            sources: new()
            {
                ["MyUtf8PageBase.cs"] = """
                    using System;
                    using Microsoft.AspNetCore.Mvc.Razor;

                    namespace MyApp.Infrastructure
                    {
                        public abstract class MyUtf8PageBase : RazorPage
                        {
                            public void WriteLiteral(ReadOnlySpan<byte> utf8HtmlLiteral)
                            {
                                WriteLiteral(System.Text.Encoding.UTF8.GetString(utf8HtmlLiteral));
                            }
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
        var indexSource = result.GeneratedSources.Single(s => s.HintName.Contains("Index")).SourceText.ToString();
        Assert.Contains("u8)", indexSource);
    }
}
