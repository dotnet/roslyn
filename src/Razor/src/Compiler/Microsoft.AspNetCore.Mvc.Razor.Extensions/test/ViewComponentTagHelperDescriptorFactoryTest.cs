// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class ViewComponentTagHelperDescriptorFactoryTest
{
    private static readonly Compilation _compilation = TestCompilation.Create(syntaxTrees: [CSharpSyntaxTree.ParseText(AdditionalCode)], references: []);

    [Fact]
    public void CreateDescriptor_UnderstandsStringParameters()
    {
        // Arrange
        var testCompilation = _compilation;
        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.StringParameterViewComponent");
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var expectedDescriptor = TagHelperDescriptorBuilder.CreateViewComponent("__Generated__StringParameterViewComponentTagHelper", TestCompilation.AssemblyName)
            .TypeName("__Generated__StringParameterViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("StringParameter", TypeNameObject.From("StringParameter")))
            .DisplayName("StringParameterViewComponentTagHelper")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("vc:string-parameter")
                .RequireAttributeDescriptor(attribute => attribute.Name("foo"))
                .RequireAttributeDescriptor(attribute => attribute.Name("bar")))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("foo")
                .PropertyName("foo")
                .TypeName(typeof(string).FullName)
                .DisplayName("string StringParameterViewComponentTagHelper.foo"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("bar")
                .PropertyName("bar")
                .TypeName(typeof(string).FullName)
                .DisplayName("string StringParameterViewComponentTagHelper.bar"))
            .Build();

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_UnderstandsVariousParameterTypes()
    {
        // Arrange
        var testCompilation = _compilation;
        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.VariousParameterViewComponent");
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var expectedDescriptor = TagHelperDescriptorBuilder.CreateViewComponent("__Generated__VariousParameterViewComponentTagHelper", TestCompilation.AssemblyName)
            .TypeName("__Generated__VariousParameterViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("VariousParameter", TypeNameObject.From("VariousParameter")))
            .DisplayName("VariousParameterViewComponentTagHelper")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("vc:various-parameter")
                .RequireAttributeDescriptor(attribute => attribute.Name("test-enum"))
                .RequireAttributeDescriptor(attribute => attribute.Name("test-string")))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("test-enum")
                .PropertyName("testEnum")
                .TypeName("TestNamespace.VariousParameterViewComponent.TestEnum")
                .AsEnum()
                .DisplayName("TestNamespace.VariousParameterViewComponent.TestEnum VariousParameterViewComponentTagHelper.testEnum"))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("test-string")
                .PropertyName("testString")
                .TypeName(typeof(string).FullName)
                .DisplayName("string VariousParameterViewComponentTagHelper.testString"))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("baz")
                .PropertyName("baz")
                .TypeName(typeof(int).FullName)
                .DisplayName("int VariousParameterViewComponentTagHelper.baz"))
            .Build();

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_UnderstandsGenericParameters()
    {
        // Arrange
        var testCompilation = _compilation;
        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.GenericParameterViewComponent");
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var expectedDescriptor = TagHelperDescriptorBuilder.CreateViewComponent("__Generated__GenericParameterViewComponentTagHelper", TestCompilation.AssemblyName)
            .TypeName("__Generated__GenericParameterViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("GenericParameter", TypeNameObject.From("GenericParameter")))
            .DisplayName("GenericParameterViewComponentTagHelper")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("vc:generic-parameter")
                .RequireAttributeDescriptor(attribute => attribute.Name("foo")))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("foo")
                .PropertyName("Foo")
                .TypeName("System.Collections.Generic.List<System.String>")
                .DisplayName("System.Collections.Generic.List<System.String> GenericParameterViewComponentTagHelper.Foo"))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("bar")
                .PropertyName("Bar")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                .AsDictionaryAttribute("bar-", typeof(int).FullName)
                .DisplayName("System.Collections.Generic.Dictionary<System.String, System.Int32> GenericParameterViewComponentTagHelper.Bar"))
            .Build();

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_ForSyncViewComponentWithInvokeInBaseType_Works()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var expectedDescriptor = TagHelperDescriptorBuilder.CreateViewComponent("__Generated__SyncDerivedViewComponentTagHelper", TestCompilation.AssemblyName)
            .TypeName("__Generated__SyncDerivedViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("SyncDerived", TypeNameObject.From("SyncDerived")))
            .DisplayName("SyncDerivedViewComponentTagHelper")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("vc:sync-derived")
                .RequireAttributeDescriptor(attribute => attribute.Name("foo"))
                .RequireAttributeDescriptor(attribute => attribute.Name("bar")))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("foo")
                .PropertyName("foo")
                .TypeName(typeof(string).FullName)
                .DisplayName("string SyncDerivedViewComponentTagHelper.foo"))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("bar")
                .PropertyName("bar")
                .TypeName(typeof(string).FullName)
                .DisplayName("string SyncDerivedViewComponentTagHelper.bar"))
            .Build();

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.SyncDerivedViewComponent");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_ForAsyncViewComponentWithInvokeInBaseType_Works()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var expectedDescriptor = TagHelperDescriptorBuilder.CreateViewComponent("__Generated__AsyncDerivedViewComponentTagHelper", TestCompilation.AssemblyName)
            .TypeName("__Generated__AsyncDerivedViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("AsyncDerived", TypeNameObject.From("AsyncDerived")))
            .DisplayName("AsyncDerivedViewComponentTagHelper")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("vc:async-derived"))
            .Build();

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.AsyncDerivedViewComponent");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_AddsDiagnostic_ForViewComponentWithNoInvokeMethod()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.ViewComponentWithoutInvokeMethod");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_CannotFindMethod.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_AddsDiagnostic_ForViewComponentWithNoInstanceInvokeMethod()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.StaticInvokeAsyncViewComponent");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_CannotFindMethod.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_AddsDiagnostic_ForViewComponentWithNoPublicInvokeMethod()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.NonPublicInvokeAsyncViewComponent");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_CannotFindMethod.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_ForViewComponentWithInvokeAsync_UnderstandsGenericTask()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.AsyncViewComponentWithGenericTask");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        Assert.Empty(descriptor.GetAllDiagnostics());
    }

    [Fact]
    public void CreateDescriptor_ForViewComponentWithInvokeAsync_UnderstandsNonGenericTask()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.AsyncViewComponentWithNonGenericTask");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        Assert.Empty(descriptor.GetAllDiagnostics());
    }

    [Fact]
    public void CreateDescriptor_ForViewComponentWithInvokeAsync_DoesNotUnderstandVoid()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.AsyncViewComponentWithString");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_AsyncMethod_ShouldReturnTask.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_ForViewComponentWithInvokeAsync_DoesNotUnderstandString()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.AsyncViewComponentWithString");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_AsyncMethod_ShouldReturnTask.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_ForViewComponentWithInvoke_DoesNotUnderstandVoid()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.SyncViewComponentWithVoid");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_SyncMethod_ShouldReturnValue.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_ForViewComponentWithInvoke_DoesNotUnderstandNonGenericTask()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.SyncViewComponentWithNonGenericTask");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_SyncMethod_CannotReturnTask.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_ForViewComponentWithInvoke_DoesNotUnderstandGenericTask()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.SyncViewComponentWithGenericTask");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_SyncMethod_CannotReturnTask.Id, diagnostic.Id);
    }

    [Fact]
    public void CreateDescriptor_ForViewComponent_WithAmbiguousMethods()
    {
        // Arrange
        var testCompilation = _compilation;
        var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

        var viewComponent = testCompilation.GetTypeByMetadataName("TestNamespace.DerivedViewComponentWithAmbiguity");

        // Act
        var descriptor = factory.CreateDescriptor(viewComponent);

        // Assert
        var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
        Assert.Equal(RazorExtensionsDiagnosticFactory.ViewComponent_AmbiguousMethods.Id, diagnostic.Id);
    }

    public const string AdditionalCode = """
        using System.Collections.Generic;
        using System.Threading.Tasks;

        namespace TestNamespace
        {
            public class StringParameterViewComponent
            {
                public string Invoke(string foo, string bar) => null;
            }

            public class VariousParameterViewComponent
            {
                public string Invoke(TestEnum testEnum, string testString, int baz = 5) => null;

                public enum TestEnum
                {
                    A = 1,
                    B = 2,
                    C = 3
                }
            }

            public class GenericParameterViewComponent
            {
                public string Invoke(List<string> Foo, Dictionary<string, int> Bar) => null;
            }

            public class ViewComponentWithoutInvokeMethod
            {
            }

            public class AsyncViewComponentWithGenericTask
            {
                public Task<string> InvokeAsync() => null;
            }

            public class AsyncViewComponentWithNonGenericTask
            {
                public Task InvokeAsync() => null;
            }

            public class AsyncViewComponentWithVoid
            {
                public void InvokeAsync() { }
            }

            public class AsyncViewComponentWithString
            {
                public string InvokeAsync() => null;
            }

            public class SyncViewComponentWithVoid
            {
                public void Invoke() { }
            }

            public class SyncViewComponentWithNonGenericTask
            {
                public Task Invoke() => null;
            }

            public class SyncViewComponentWithGenericTask
            {
                public Task<string> Invoke() => null;
            }

            public class SyncDerivedViewComponent : StringParameterViewComponent
            {
            }

            public class AsyncDerivedViewComponent : AsyncViewComponentWithNonGenericTask
            {
            }

            public class DerivedViewComponentWithAmbiguity : AsyncViewComponentWithNonGenericTask
            {
                public string Invoke() => null;
            }

            public class StaticInvokeAsyncViewComponent
            {
                public static Task<string> InvokeAsync() => null;
            }

            public class NonPublicInvokeAsyncViewComponent
            {
                protected Task<string> InvokeAsync() => null;
            }
        }
        """;
}
