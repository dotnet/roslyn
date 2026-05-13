// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Integration tests for component directives
public class ComponentDirectiveIntegrationTest : RazorIntegrationTestBase
{
    public ComponentDirectiveIntegrationTest()
    {
        AdditionalSyntaxTrees.Add(Parse(AdditionalCode));
    }

    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    [Fact]
    public void ComponentsDoNotHaveLayoutAttributeByDefault()
    {
        // Arrange/Act
        var component = CompileToComponent("Hello");

        // Assert
        Assert.Null(component.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == "LayoutAttribute"));
    }

    [Fact]
    public void SupportsLayoutDeclarations()
    {
        // Arrange/Act
        var component = CompileToComponent(
            "@layout TestNamespace.TestLayout\n" +
            "Hello");

        // Assert
        var layoutAttribute = component.GetAttributes().Single(a => a.AttributeClass.Name == "LayoutAttribute");
        Assert.NotNull(layoutAttribute);
    }

    [Fact]
    public void SupportsImplementsDeclarations()
    {
        // Arrange/Act
        var component = CompileToComponent(
            "@implements TestNamespace.ITestInterface\n" +
            "Hello");

        // Assert
        AssertEx.Equal("TestNamespace.ITestInterface", component.Interfaces.Single().ToTestDisplayString());
    }

    [Fact]
    public void SupportsMultipleImplementsDeclarations()
    {
        // Arrange/Act
        var component = CompileToComponent(
            "@implements TestNamespace.ITestInterface\n" +
            "@implements TestNamespace.ITestInterface2\n" +
            "Hello");

        // Assert
        Assert.Collection(component.Interfaces.OrderBy(p => p.Name),
            s => AssertEx.Equal("TestNamespace.ITestInterface", s.ToTestDisplayString()),
            s => AssertEx.Equal("TestNamespace.ITestInterface2", s.ToTestDisplayString()));
    }

    [Fact]
    public void SupportsInheritsDirective()
    {
        // Arrange/Act
        var component = CompileToComponent(
            "@inherits TestNamespace.TestBaseClass" + Environment.NewLine +
            "Hello");

        // Assert
        AssertEx.Equal("TestNamespace.TestBaseClass", component.BaseType.ToTestDisplayString());
    }

    [Fact]
    public void SupportsInjectDirective()
    {
        // Arrange/Act 1: Compilation
        var component = CompileToComponent(
            "@inject TestNamespace.IMyService1 MyService1\n" +
            "@inject TestNamespace.IMyService2 MyService2\n" +
            "Hello from @MyService1 and @MyService2");

        // Assert 1: Compiled type has correct properties
        var injectableProperties = component.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass.Name == "InjectAttribute"));
        Assert.Collection(injectableProperties.OrderBy(p => p.Name),
            s => AssertEx.Equal("private TestNamespace.IMyService1 Test.TestComponent.MyService1 { get; set; }", s.ToTestDisplayString()),
            s => AssertEx.Equal("private TestNamespace.IMyService2 Test.TestComponent.MyService2 { get; set; }", s.ToTestDisplayString()));
    }

    [Fact]
    public void SupportsIncompleteInjectDirectives()
    {
        var component = CompileToComponent("""
            @inject 
            @inject DateTime 
            @inject DateTime Value
            """);

        // Assert 1: Compiled type has correct properties
        var injectableProperties = component.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass.Name == "InjectAttribute"));
        Assert.Collection(injectableProperties.OrderBy(p => p.Name),
            s => AssertEx.Equal("private System.DateTime Test.TestComponent.Member___UniqueIdSuppressedForTesting__ { get; set; }", s.ToTestDisplayString()),
            s => AssertEx.Equal("private System.DateTime Test.TestComponent.Value { get; set; }", s.ToTestDisplayString()));
    }

    private const string AdditionalCode =
    """
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;
    using System;
    using System.Threading.Tasks;
    
    namespace TestNamespace;

    public class TestLayout : IComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }
    }

    public interface ITestInterface { }

    public interface ITestInterface2 { }

    public class TestBaseClass : ComponentBase { }

    public interface IMyService1 { }
    public interface IMyService2 { }
    public class MyService1Impl : IMyService1 { }
    public class MyService2Impl : IMyService2 { }
    """;
}
