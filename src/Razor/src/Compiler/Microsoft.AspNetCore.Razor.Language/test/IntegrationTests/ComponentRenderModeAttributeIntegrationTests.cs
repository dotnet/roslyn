// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentRenderModeAttributeIntegrationTests : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal string ComponentName = "TestComponent";

    internal override string DefaultFileName => ComponentName + ".cshtml";

    internal override bool UseTwoPhaseCompilation => true;

    [Fact]
    public void RenderMode_Attribute_With_Diagnostics()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="@Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer)" />
                """);

        // Assert

        //x:\dir\subdir\Test\TestComponent.cshtml(1, 29): Error RZ9986: Component attributes do not support complex content(mixed C# and markup). Attribute: '@rendermode', text: 'Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer)'
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ9986", diagnostic.Id);
    }

    [Fact]
    public void RenderMode_Attribute_On_Html_Element()
    {
        var generated = CompileToCSharp("""
                <input @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                """);

        // Assert
        //x:\dir\subdir\Test\TestComponent.cshtml(1,21): Error RZ10023: Attribute 'rendermode' is only valid when used on a component.
        var diag = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10023", diag.Id);
    }

    [Fact]
    public void RenderMode_Attribute_On_Component_With_Directive()
    {
        var generated = CompileToCSharp($$"""
                @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer

                <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                """);

        // Assert

        // x:\dir\subdir\Test\TestComponent.cshtml(3,29): Error RZ10024: Cannot override render mode for component 'Test.TestComponent' as it explicitly declares one.
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10024", diagnostic.Id);
    }
}

