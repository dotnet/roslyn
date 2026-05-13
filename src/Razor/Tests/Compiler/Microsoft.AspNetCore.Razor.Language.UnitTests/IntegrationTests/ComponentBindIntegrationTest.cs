// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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
}
