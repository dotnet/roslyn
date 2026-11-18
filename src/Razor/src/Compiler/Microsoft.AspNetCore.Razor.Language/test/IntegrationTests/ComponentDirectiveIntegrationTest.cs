// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Integration tests for component directives
public class ComponentDirectiveIntegrationTest : RazorIntegrationTestBase
{
    private bool _enableMarkupSplit = true;

    public ComponentDirectiveIntegrationTest()
    {
        AdditionalSyntaxTrees.Add(Parse(AdditionalCode));
    }

    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool EnableMarkupSplit => _enableMarkupSplit;

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
    public void SupportsLayoutNullDeclaration()
    {
        var component = CompileToComponent("""
            @layout null
            Hello
            """);

        // An explicit empty layout also takes precedence over the router's default layout.
        AssertEmptyLayout(component);
    }

    [Fact]
    public void LayoutNullDeclarationOverridesImportedLayout()
    {
        ImportItems.Add(CreateProjectItem("_Imports.razor", "@layout TestNamespace.TestLayout", RazorFileKind.ComponentImport));

        var component = CompileToComponent("""
            @layout null
            Hello
            """);

        AssertEmptyLayout(component);
    }

    [Fact]
    public void LayoutNullDeclarationOverridesImportedNull()
    {
        ImportItems.Add(CreateProjectItem("_Imports.razor", "@layout null", RazorFileKind.ComponentImport));

        var component = CompileToComponent("""
            @layout null
            Hello
            """);

        AssertEmptyLayout(component);
    }

    [Fact]
    public void SupportsImportedLayoutNullDeclaration()
    {
        ImportItems.Add(CreateProjectItem("_Imports.razor", "@layout null", RazorFileKind.ComponentImport));

        var component = CompileToComponent("Hello");

        AssertEmptyLayout(component);
    }

    [Fact]
    public void LayoutNullDeclarationOverridesInheritedLayout()
    {
        var component = CompileToComponent("""
            @layout null
            @inherits TestNamespace.TestBaseClassWithLayout
            Hello
            """);

        AssertEmptyLayout(component);
        Assert.NotEmpty(component.BaseType.GetAttributes());
    }

    [Fact]
    public void LayoutDeclarationOverridesImportedNull()
    {
        ImportItems.Add(CreateProjectItem("_Imports.razor", "@layout null", RazorFileKind.ComponentImport));

        var component = CompileToComponent("""
            @layout TestNamespace.TestLayout
            Hello
            """);

        var layoutAttribute = component.GetAttributes().Single(a => a.AttributeClass.Name == "LayoutAttribute");
        Assert.Equal("TestNamespace.TestLayout", ((ITypeSymbol)layoutAttribute.ConstructorArguments.Single().Value).ToTestDisplayString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LayoutNullDeclarationsDoNotCollide(bool enableMarkupSplit)
    {
        _enableMarkupSplit = enableMarkupSplit;
        AdditionalSyntaxTrees.Clear();
        const string content = """
            @layout null
            @typeparam TItem
            <p>@typeof(TItem).Name</p>
            """;

        var first = CompileToCSharp("First.razor", cshtmlContent: content);
        var second = CompileToCSharp("Second.razor", cshtmlContent: content);
        var firstAssembly = CompileToAssembly(first);

        // Compile both components together: each file must resolve its own non-generic helper.
        second.BaseCompilation = (CSharpCompilation)firstAssembly.Compilation;
        var assembly = CompileToAssembly(second);
        var firstLayout = AssertEmptyLayout(CompileToComponent(assembly, "Test.First`1"));
        var secondLayout = AssertEmptyLayout(CompileToComponent(assembly, "Test.Second`1"));
        Assert.False(SymbolEqualityComparer.Default.Equals(firstLayout, secondLayout));

        if (enableMarkupSplit)
        {
            Assert.Contains("file sealed class __EmptyLayout", first.DeclCode);
            Assert.DoesNotContain("__EmptyLayout", first.Code);
        }
        else
        {
            Assert.Null(first.DeclCode);
            Assert.Contains("file sealed class __EmptyLayout", first.Code);
        }
    }

    [Fact]
    public void LayoutNullInRazor11PreservesExistingGeneration()
    {
        var result = CompileToCSharp(
            cshtmlContent: "@layout null",
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_11_0 });

        Assert.Empty(result.RazorDiagnostics);
        Assert.DoesNotContain("__EmptyLayout", result.Code);
        Assert.DoesNotContain("__EmptyLayout", result.DeclCode);
        Assert.Contains("LayoutAttribute(typeof(", result.DeclCode);
        // The layout token keeps its source mapping, with line pragmas on either side.
        Assert.Contains("\nnull\n", result.DeclCode.Replace("\r\n", "\n"));
        Assert.Contains("))]", result.DeclCode);
    }

    [Fact]
    public void LayoutNullInRazor11WithCSharp10PreservesExistingGeneration()
    {
        var result = CompileToCSharp(
            cshtmlContent: "@layout null",
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_11_0 },
            csharpParseOptions: CSharpParseOptions.WithLanguageVersion(LanguageVersion.CSharp10));

        Assert.Empty(result.RazorDiagnostics);
        Assert.DoesNotContain("__EmptyLayout", result.Code);
        Assert.DoesNotContain("__EmptyLayout", result.DeclCode);
        Assert.Contains("LayoutAttribute(typeof(", result.DeclCode);
        // The layout token keeps its source mapping, with line pragmas on either side.
        Assert.Contains("\nnull\n", result.DeclCode.Replace("\r\n", "\n"));
        Assert.Contains("))]", result.DeclCode);
    }

    [Fact]
    public void LayoutNullInRazor12WithCSharp11()
    {
        AdditionalSyntaxTrees.Clear();
        var result = CompileToCSharp(
            cshtmlContent: "@layout null",
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_12_0 },
            csharpParseOptions: CSharpParseOptions.WithLanguageVersion(LanguageVersion.CSharp11));

        Assert.Empty(result.RazorDiagnostics);
        AssertEmptyLayout(CompileToComponent(result, "Test.TestComponent"));
    }

    [Fact]
    public void LayoutNullInRazor12WithCSharp10ReportsDiagnostic()
    {
        var result = CompileToCSharp(
            cshtmlContent: "@layout null",
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_12_0 },
            csharpParseOptions: CSharpParseOptions.WithLanguageVersion(LanguageVersion.CSharp10));

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ10031", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("'@layout null' requires C# language version 11.0 or higher.", diagnostic.GetMessage());
        Assert.Equal(8, diagnostic.Span.AbsoluteIndex);
        Assert.Equal(4, diagnostic.Span.Length);
        Assert.DoesNotContain("__EmptyLayout", result.Code);
        Assert.DoesNotContain("__EmptyLayout", result.DeclCode);
        Assert.DoesNotContain("LayoutAttribute", result.DeclCode);
    }

    [Fact]
    public void ImportedLayoutNullInRazor12WithCSharp10ReportsDiagnostic()
    {
        var import = CreateProjectItem("_Imports.razor", "@layout null", RazorFileKind.ComponentImport);
        ImportItems.Add(import);

        var result = CompileToCSharp(
            cshtmlContent: "Hello",
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_12_0 },
            csharpParseOptions: CSharpParseOptions.WithLanguageVersion(LanguageVersion.CSharp10));

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ10031", diagnostic.Id);
        Assert.Equal(import.PhysicalPath, diagnostic.Span.FilePath);
        Assert.Equal(8, diagnostic.Span.AbsoluteIndex);
        Assert.Equal(4, diagnostic.Span.Length);
    }

    [Fact]
    public void LayoutTypeOverridesImportedNullWithCSharp10()
    {
        AdditionalSyntaxTrees.Clear();
        AdditionalSyntaxTrees.Add(Parse(AdditionalCode, CSharpParseOptions.WithLanguageVersion(LanguageVersion.CSharp10)));
        ImportItems.Add(CreateProjectItem("_Imports.razor", "@layout null", RazorFileKind.ComponentImport));

        var result = CompileToCSharp(
            cshtmlContent: "@layout TestNamespace.TestLayout",
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_12_0 },
            csharpParseOptions: CSharpParseOptions.WithLanguageVersion(LanguageVersion.CSharp10));

        Assert.Empty(result.RazorDiagnostics);
        var component = CompileToComponent(result, "Test.TestComponent");
        var layoutAttribute = component.GetAttributes().Single(a => a.AttributeClass.Name == "LayoutAttribute");
        Assert.Equal("TestNamespace.TestLayout", ((ITypeSymbol)layoutAttribute.ConstructorArguments.Single().Value).ToTestDisplayString());
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

    private static INamedTypeSymbol AssertEmptyLayout(INamedTypeSymbol component)
    {
        var attribute = component.GetAttributes().Single(a => a.AttributeClass.Name == "LayoutAttribute");
        var layout = Assert.IsAssignableFrom<INamedTypeSymbol>(attribute.ConstructorArguments.Single().Value);
        Assert.Equal("__EmptyLayout", layout.Name);
        Assert.True(layout.IsFileLocal);
        Assert.Equal("Microsoft.AspNetCore.Components.LayoutComponentBase", layout.BaseType.ToTestDisplayString());
        Assert.Empty(layout.GetAttributes());
        Assert.True(Assert.Single(layout.GetMembers("BuildRenderTree").OfType<IMethodSymbol>()).IsOverride);
        return layout;
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

    [Layout(typeof(TestLayout))]
    public class TestBaseClassWithLayout : ComponentBase { }

    public interface IMyService1 { }
    public interface IMyService2 { }
    public class MyService1Impl : IMyService1 { }
    public class MyService2Impl : IMyService2 { }
    """;
}
