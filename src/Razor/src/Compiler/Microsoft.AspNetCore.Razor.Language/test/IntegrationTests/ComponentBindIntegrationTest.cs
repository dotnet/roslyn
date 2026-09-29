// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentBindIntegrationTest : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    [Fact]
    public void BindDuplicates_ReportsDiagnostic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue2"", ""myevent2"")]
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var result = CompileToCSharp(@"
<div @bind-value=""@ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ9989", diagnostic.Id);
        Assert.Equal("""
            The attribute '@bind-value' was matched by multiple bind attributes. Duplicates:
            Test.BindAttributes
            Test.BindAttributes
            """,
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void BindFallback_InvalidSyntax_TooManyParts()
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<input type=""text"" @bind-first-second-third=""Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}");

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ9991", diagnostic.Id);
    }

    [Fact]
    public void BindFallback_InvalidSyntax_TrailingDash()
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<input type=""text"" @bind-first-=""Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}");

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ9991", diagnostic.Id);
    }

    [Fact]
    public void Bind_InvalidUseOfDirective_DoesNotThrow()
    {
        // We're looking for VS crash issues. Meaning if the parser returns
        // diagnostics we don't want to throw.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input type=""text"" @bind=""@page"" />
@functions {
    public string page { get; set; } = ""text"";
}");

        // Assert
        Assert.Collection(
            generated.RazorDiagnostics,
            d => Assert.Equal("RZ2005", d.Id),
            d => Assert.Equal("RZ1011", d.Id));
    }

    [Fact]
    public void BindToComponent_IncompleteDirectiveAttribute_ReportsDiagnostics()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class InputText : ComponentBase
                {
                    [Parameter]
                    public string Value { get; set; }

                    [Parameter]
                    public Action<string> ValueChanged { get; set; }
                }
            }
            """));

        var generated = CompileToCSharp("""
            @using Test
            <InputText @bind-F
            """);

        Assert.Collection(
            generated.RazorDiagnostics,
            diagnostic =>
            {
                Assert.Equal("RZ1035", diagnostic.Id);
                Assert.Equal("Missing close angle for tag helper 'InputText'.", diagnostic.GetMessage(CultureInfo.CurrentCulture));
            },
            diagnostic =>
            {
                Assert.Equal("RZ1034", diagnostic.Id);
                Assert.Equal("Found a malformed 'InputText' tag helper. Tag helpers must have a start and end tag or be self closing.", diagnostic.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_MissingTarget_NoWarningAtAnalysisLevel10()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
            }
            """));

        var generated = CompileToCSharp(
            """
            @using Test
            <MyComponent @bind-Missing="ParentValue" />

            @code {
                public int ParentValue { get; set; }
            }
            """,
            configuration: Configuration with { RazorWarningLevel = 10 });

        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_MissingTarget_ButAcceptsUnmatched_NoWarningAtAnalysisLevel11()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter(CaptureUnmatchedValues = true)]
                public Dictionary<string, object> AdditionalAttributes { get; set; }
            }
            """));

        var generated = CompileToCSharp(
            """
            @using Test
            <MyComponent @bind-Missing="ParentValue" />

            @code {
                public int ParentValue { get; set; }
            }
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_MissingTarget_WarnsAtAnalysisLevel11()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
            }
            """));

        const string content = """
            @using Test
            <MyComponent @bind-Missing="ParentValue" />

            @code {
                public int ParentValue { get; set; }
            }
            """;

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10026", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(11, diagnostic.WarningLevel);
        Assert.Equal(
            "The bind attribute '@bind-Missing' does not match any parameter on component 'MyComponent'.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
        AssertDiagnosticSpan(content, diagnostic, "Missing");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_MissingChangeParameter_NoWarningAtAnalysisLevel10()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter]
                public int Value { get; set; }
            }
            """));

        var generated = CompileToCSharp(
            """
            @using Test
            <MyComponent @bind-Value="ParentValue" />

            @code {
                public int ParentValue { get; set; }
            }
            """,
            configuration: Configuration with { RazorWarningLevel = 10 });

        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_MissingChangeParameter_WarnsAtAnalysisLevel11()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter]
                public int Value { get; set; }
            }
            """));

        const string content = """
            @using Test
            <MyComponent @bind-Value="ParentValue" />

            @code {
                public int ParentValue { get; set; }
            }
            """;

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10027", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(11, diagnostic.WarningLevel);
        Assert.Equal(
            "The bind attribute '@bind-Value' requires a matching change parameter named 'ValueChanged' on component 'MyComponent'.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
        AssertDiagnosticSpan(content, diagnostic, "Value");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_MissingChangeParameter_ButAcceptsUnmatched_NoWarningAtAnalysisLevel11()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter]
                public int Value { get; set; }

                [Parameter(CaptureUnmatchedValues = true)]
                public Dictionary<string, object> AdditionalAttributes { get; set; }
            }
            """));

        var generated = CompileToCSharp(
            """
            @using Test
            <MyComponent @bind-Value="ParentValue" />

            @code {
                public int ParentValue { get; set; }
            }
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_MissingExplicitChangeParameter_WarnsAtAnalysisLevel11()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter]
                public int Value { get; set; }
            }
            """));

        const string content = """
            @using Test
            <MyComponent @bind-Value="ParentValue" @bind-Value:event="OnChanged" />

            @code {
                public int ParentValue { get; set; }
            }
            """;

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10027", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(11, diagnostic.WarningLevel);
        Assert.Equal(
            "The bind attribute '@bind-Value' requires a matching change parameter named 'OnChanged' on component 'MyComponent'.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
        AssertDiagnosticSpan(content, diagnostic, "OnChanged");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_DynamicEvent_NoWarningAtAnalysisLevel11()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter]
                public int Value { get; set; }
            }
            """));

        var generated = CompileToCSharp(
            """
            @using Test
            <MyComponent @bind-Value="ParentValue" @bind-Value:event="@EventName" />

            @code {
                public int ParentValue { get; set; }

                private string EventName => "ValueChanged";
            }
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Empty(generated.RazorDiagnostics);
        CompileToAssembly(generated);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13125")]
    public void BindToComponent_ValidTargetsAndModifiers_NoWarningsAtAnalysisLevel11()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter]
                public int Value { get; set; }

                [Parameter]
                public Action<int> ValueChanged { get; set; }

                [Parameter]
                public Action<int> OnChanged { get; set; }
            }
            """));

        var generated = CompileToCSharp(
            """
            @using Test
            <MyComponent @bind-Value="ParentValue" />
            <MyComponent @bind-Value="ParentValue" @bind-Value:event="OnChanged" />
            <MyComponent @bind-Value:get="ParentValue" @bind-Value:set="UpdateValue" />
            <MyComponent @bind-Value:get="ParentValue" @bind-Value:after="After" />

            @code {
                public int ParentValue { get; set; }

                private void UpdateValue(int value) => ParentValue = value;

                private void After()
                {
                }
            }
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Empty(generated.RazorDiagnostics);
        CompileToAssembly(generated);
    }

    private static void AssertDiagnosticSpan(string content, RazorDiagnostic diagnostic, string expected)
    {
        var index = content.IndexOf(expected, StringComparison.Ordinal);
        Assert.NotNull(diagnostic.Span.FilePath);
        Assert.Equal(index, diagnostic.Span.AbsoluteIndex);
        Assert.Equal(expected.Length, diagnostic.Span.Length);
    }
}
