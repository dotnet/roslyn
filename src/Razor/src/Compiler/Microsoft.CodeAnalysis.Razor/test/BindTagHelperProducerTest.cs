// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class BindTagHelperProducerTest : TagHelperDescriptorProviderTestBase
{
    protected override void ConfigureEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new BindTagHelperProducer.Factory());
        builder.Features.Add(new ComponentTagHelperProducer.Factory());
    }

    [Fact]
    public void GetTagHelpers_FindsBindTagHelperOnComponentType_Delegate_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

        [Parameter]
        public string MyProperty { get; set; }

        [Parameter]
        public Action<string> MyPropertyChanged { get; set; }

        [Parameter]
        public Expression<Func<string>> MyPropertyExpression { get; set; }
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 1);
        var bind = Assert.Single(matches);

        // These are features Bind Tags Helpers don't use. Verifying them once here and
        // then ignoring them.
        Assert.Empty(bind.AllowedChildTags);
        Assert.Null(bind.TagOutputHint);

        // These are features that are invariants of all Bind Tag Helpers. Verifying them once
        // here and then ignoring them.
        Assert.Empty(bind.Diagnostics);
        Assert.False(bind.HasErrors);
        Assert.Equal(TagHelperKind.Bind, bind.Kind);
        Assert.Equal(RuntimeKind.None, bind.RuntimeKind);
        Assert.False(bind.IsDefaultKind());
        Assert.False(bind.KindUsesDefaultTagHelperRuntime());
        Assert.False(bind.IsComponentOrChildContentTagHelper());
        Assert.True(bind.CaseSensitive);

        Assert.Equal("MyProperty", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("MyPropertyChanged", ((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.Equal("MyPropertyExpression", ((BindMetadata)bind.Metadata).ExpressionAttribute);

        Assert.Equal(
            "Binds the provided expression to the 'MyProperty' property and a change event " +
                "delegate to the 'MyPropertyChanged' property of the component.",
            bind.Documentation);

        // These are all trivially derived from the assembly/namespace/type name
        Assert.Equal("TestAssembly", bind.AssemblyName);
        Assert.Equal("Test.MyComponent", bind.Name);
        Assert.Equal("Test.MyComponent", bind.DisplayName);
        Assert.Equal("Test.MyComponent", bind.TypeName);

        Assert.Collection(bind.TagMatchingRules.OrderBy(r => r.Attributes.Length),
            rule =>
            {
                Assert.Empty(rule.Diagnostics);
                Assert.False(rule.HasErrors);
                Assert.Null(rule.ParentTag);
                Assert.Equal("MyComponent", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                var requiredAttribute = Assert.Single(rule.Attributes);
                Assert.Empty(requiredAttribute.Diagnostics);
                Assert.Equal("@bind-MyProperty", requiredAttribute.DisplayName);
                Assert.Equal("@bind-MyProperty", requiredAttribute.Name);
                Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                Assert.Null(requiredAttribute.Value);
                Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);
            },
            rule =>
            {
                Assert.Empty(rule.Diagnostics);
                Assert.False(rule.HasErrors);
                Assert.Null(rule.ParentTag);
                Assert.Equal("MyComponent", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(rule.Attributes.OrderBy(a => a.Name),
                    requiredAttribute =>
                    {
                        Assert.Empty(requiredAttribute.Diagnostics);
                        Assert.Equal("@bind-MyProperty:get", requiredAttribute.DisplayName);
                        Assert.Equal("@bind-MyProperty:get", requiredAttribute.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                        Assert.Null(requiredAttribute.Value);
                        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);
                    },
                    requiredAttribute =>
                    {
                        Assert.Empty(requiredAttribute.Diagnostics);
                        Assert.Equal("@bind-MyProperty:set", requiredAttribute.DisplayName);
                        Assert.Equal("@bind-MyProperty:set", requiredAttribute.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                        Assert.Null(requiredAttribute.Value);
                        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);
                    });
            });

        var attribute = Assert.Single(bind.BoundAttributes);

        // Invariants
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(TagHelperKind.Bind, attribute.Parent.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.HasIndexer);
        Assert.Null(attribute.IndexerNamePrefix);
        Assert.Null(attribute.IndexerTypeName);
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);

        Assert.Equal(
            "Binds the provided expression to the 'MyProperty' property and a change event " +
                "delegate to the 'MyPropertyChanged' property of the component.",
            attribute.Documentation);

        Assert.Equal("@bind-MyProperty", attribute.Name);
        Assert.Equal("MyProperty", attribute.PropertyName);
        Assert.Equal("System.Action<System.String> Test.MyComponent.MyProperty", attribute.DisplayName);

        // Defined from the property type
        Assert.Equal("System.Action<System.String>", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);
    }

    [Fact]
    public void GetTagHelpers_BindTagHelperReturnsEmptyWhenCompilationAssemblyTargetSymbol()
    {
        // When BindTagHelperDescriptorProvider is given a compilation that references
        // API assemblies with "BindConverter", and a target symbol that does not match the
        // assembly containing "BindConverter", it will NOT find the expected tag helpers.

        // Arrange
        var compilation = BaseCompilation;

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        Assert.Empty(matches);
    }

    [Fact]
    public void GetTagHelpers_FindsBindTagHelperOnComponentType_EventCallback_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

        [Parameter]
        public string MyProperty { get; set; }

        [Parameter]
        public EventCallback<string> MyPropertyChanged { get; set; }
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 1);
        var bind = Assert.Single(matches);

        // These are features Bind Tags Helpers don't use. Verifying them once here and
        // then ignoring them.
        Assert.Empty(bind.AllowedChildTags);
        Assert.Null(bind.TagOutputHint);

        // These are features that are invariants of all Bind Tag Helpers. Verifying them once
        // here and then ignoring them.
        Assert.Empty(bind.Diagnostics);
        Assert.False(bind.HasErrors);
        Assert.Equal(TagHelperKind.Bind, bind.Kind);
        Assert.Equal(RuntimeKind.None, bind.RuntimeKind);
        Assert.False(bind.IsDefaultKind());
        Assert.False(bind.KindUsesDefaultTagHelperRuntime());
        Assert.False(bind.IsComponentOrChildContentTagHelper());
        Assert.True(bind.CaseSensitive);

        Assert.Equal("MyProperty", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("MyPropertyChanged", ((BindMetadata)bind.Metadata).ChangeAttribute);

        Assert.Equal(
            "Binds the provided expression to the 'MyProperty' property and a change event " +
                "delegate to the 'MyPropertyChanged' property of the component.",
            bind.Documentation);

        // These are all trivially derived from the assembly/namespace/type name
        Assert.Equal("TestAssembly", bind.AssemblyName);
        Assert.Equal("Test.MyComponent", bind.Name);
        Assert.Equal("Test.MyComponent", bind.DisplayName);
        Assert.Equal("Test.MyComponent", bind.TypeName);

        Assert.Collection(bind.TagMatchingRules.OrderBy(o => o.Attributes.Length),
            rule =>
            {
                Assert.Empty(rule.Diagnostics);
                Assert.False(rule.HasErrors);
                Assert.Null(rule.ParentTag);
                Assert.Equal("MyComponent", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                var requiredAttribute = Assert.Single(rule.Attributes);
                Assert.Empty(requiredAttribute.Diagnostics);
                Assert.Equal("@bind-MyProperty", requiredAttribute.DisplayName);
                Assert.Equal("@bind-MyProperty", requiredAttribute.Name);
                Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                Assert.Null(requiredAttribute.Value);
                Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);
            },
            rule =>
            {
                Assert.Empty(rule.Diagnostics);
                Assert.False(rule.HasErrors);
                Assert.Null(rule.ParentTag);
                Assert.Equal("MyComponent", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(rule.Attributes.OrderBy(a => a.Name),
                    requiredAttribute =>
                    {
                        Assert.Empty(requiredAttribute.Diagnostics);
                        Assert.Equal("@bind-MyProperty:get", requiredAttribute.DisplayName);
                        Assert.Equal("@bind-MyProperty:get", requiredAttribute.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                        Assert.Null(requiredAttribute.Value);
                        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);
                    },
                    requiredAttribute =>
                    {
                        Assert.Empty(requiredAttribute.Diagnostics);
                        Assert.Equal("@bind-MyProperty:set", requiredAttribute.DisplayName);
                        Assert.Equal("@bind-MyProperty:set", requiredAttribute.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                        Assert.Null(requiredAttribute.Value);
                        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);
                    });
            });

        var attribute = Assert.Single(bind.BoundAttributes);

        // Invariants
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(TagHelperKind.Bind, attribute.Parent.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.HasIndexer);
        Assert.Null(attribute.IndexerNamePrefix);
        Assert.Null(attribute.IndexerTypeName);
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);

        Assert.Equal(
            "Binds the provided expression to the 'MyProperty' property and a change event " +
                "delegate to the 'MyPropertyChanged' property of the component.",
            attribute.Documentation);

        Assert.Equal("@bind-MyProperty", attribute.Name);
        Assert.Equal("MyProperty", attribute.PropertyName);
        Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.String> Test.MyComponent.MyProperty", attribute.DisplayName);

        // Defined from the property type
        Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.String>", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);
    }

    [Fact]
    public void GetTagHelpers_NoMatchedPropertiesOnComponent_IgnoresComponent()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

        public string MyProperty { get; set; }

        public Action<string> MyPropertyChangedNotMatch { get; set; }
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
        Assert.Empty(matches);
    }

    [Fact]
    public void GetTagHelpers_BindOnElement_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myprop"", ""myevent"")]
    public class BindAttributes
    {
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
        var bind = Assert.Single(matches);

        // These are features Bind Tags Helpers don't use. Verifying them once here and
        // then ignoring them.
        Assert.Empty(bind.AllowedChildTags);
        Assert.Null(bind.TagOutputHint);

        // These are features that are invariants of all Bind Tag Helpers. Verifying them once
        // here and then ignoring them.
        Assert.Empty(bind.Diagnostics);
        Assert.False(bind.HasErrors);
        Assert.Equal(TagHelperKind.Bind, bind.Kind);
        Assert.Equal(RuntimeKind.None, bind.RuntimeKind);
        Assert.False(bind.IsDefaultKind());
        Assert.False(bind.KindUsesDefaultTagHelperRuntime());
        Assert.False(bind.IsComponentOrChildContentTagHelper());
        Assert.True(bind.CaseSensitive);
        Assert.True(bind.ClassifyAttributesOnly);

        Assert.Equal("myprop", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("myevent", ((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.False(bind.IsInputElementBindTagHelper());
        Assert.False(bind.IsInputElementFallbackBindTagHelper());

        Assert.Equal(
            "Binds the provided expression to the 'myprop' attribute and a change event " +
                "delegate to the 'myevent' attribute.",
            bind.Documentation);

        // These are all trivially derived from the assembly/namespace/type name
        Assert.Equal("Microsoft.AspNetCore.Components", bind.AssemblyName);
        Assert.Equal("Bind", bind.Name);
        Assert.Equal("Test.BindAttributes", bind.DisplayName);
        Assert.Equal("Test.BindAttributes", bind.TypeName);

        // The tag matching rule for a bind-Component is always the component name + the attribute name
        Assert.Collection(bind.TagMatchingRules.OrderBy(o => o.Attributes.Length),
            rule =>
            {
                Assert.Empty(rule.Diagnostics);
                Assert.False(rule.HasErrors);
                Assert.Null(rule.ParentTag);
                Assert.Equal("div", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                var requiredAttribute = Assert.Single(rule.Attributes);
                Assert.Empty(requiredAttribute.Diagnostics);
                Assert.Equal("@bind", requiredAttribute.DisplayName);
                Assert.Equal("@bind", requiredAttribute.Name);
                Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                Assert.Null(requiredAttribute.Value);
                Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);

                var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
                AssertAttribute(attribute);
            },
            rule =>
            {
                Assert.Empty(rule.Diagnostics);
                Assert.False(rule.HasErrors);
                Assert.Null(rule.ParentTag);
                Assert.Equal("div", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(rule.Attributes.OrderBy(a => a.Name),
                    requiredAttribute =>
                    {
                        Assert.Empty(requiredAttribute.Diagnostics);
                        Assert.Equal("@bind:get", requiredAttribute.DisplayName);
                        Assert.Equal("@bind:get", requiredAttribute.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                        Assert.Null(requiredAttribute.Value);
                        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);

                        var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
                        AssertAttribute(attribute);
                    },
                    requiredAttribute =>
                    {
                        Assert.Empty(requiredAttribute.Diagnostics);
                        Assert.Equal("@bind:set", requiredAttribute.DisplayName);
                        Assert.Equal("@bind:set", requiredAttribute.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
                        Assert.Null(requiredAttribute.Value);
                        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);

                        var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
                        AssertAttribute(attribute);
                    });
            });

        static void AssertAttribute(BoundAttributeDescriptor attribute)
        {
            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(TagHelperKind.Bind, attribute.Parent.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Binds the provided expression to the 'myprop' attribute and a change event " +
                    "delegate to the 'myevent' attribute.",
                attribute.Documentation);

            Assert.Equal("@bind", attribute.Name);
            Assert.Equal("Bind", attribute.PropertyName);
            Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Object", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);

            var parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("format"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(TagHelperKind.Bind, parameter.Parent.Parent.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies a format to convert the value specified by the '@bind' attribute. " +
                "The format string can currently only be used with expressions of type <code>DateTime</code>.",
                parameter.Documentation);

            Assert.Equal("format", parameter.Name);
            Assert.Equal("Format_myprop", parameter.PropertyName);
            Assert.Equal(":format", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.String", parameter.TypeName);
            Assert.True(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);

            parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("culture"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(TagHelperKind.Bind, parameter.Parent.Parent.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies the culture to use for conversions.",
                parameter.Documentation);

            Assert.Equal("culture", parameter.Name);
            Assert.Equal("Culture", parameter.PropertyName);
            Assert.Equal(":culture", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Globalization.CultureInfo", parameter.TypeName);
            Assert.False(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);

            parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("get"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(TagHelperKind.Bind, parameter.Parent.Parent.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies the expression to use for binding the value to the attribute.",
                parameter.Documentation);

            Assert.Equal("get", parameter.Name);
            Assert.Equal("Get", parameter.PropertyName);
            Assert.Equal(":get", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Object", parameter.TypeName);
            Assert.False(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);

            parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("set"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(TagHelperKind.Bind, parameter.Parent.Parent.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies the expression to use for updating the bound value when a new value is available.",
                parameter.Documentation);

            Assert.Equal("set", parameter.Name);
            Assert.Equal("Set", parameter.PropertyName);
            Assert.Equal(":set", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Delegate", parameter.TypeName);
            Assert.False(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);

            parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("after"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(TagHelperKind.Bind, parameter.Parent.Parent.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies an action to run after the new value has been set.",
                parameter.Documentation);

            Assert.Equal("after", parameter.Name);
            Assert.Equal("After", parameter.PropertyName);
            Assert.Equal(":after", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Delegate", parameter.TypeName);
            Assert.False(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);
        }
    }

    [Fact]
    public void GetTagHelpers_BindOnElementWithSuffix_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""myprop"", ""myprop"", ""myevent"")]
    public class BindAttributes
    {
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
        var bind = Assert.Single(matches);

        Assert.Equal("myprop", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("myevent", ((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.False(bind.IsInputElementBindTagHelper());
        Assert.False(bind.IsInputElementFallbackBindTagHelper());

        Assert.Collection(bind.TagMatchingRules.OrderBy(o => o.Attributes.Length),
            rule =>
            {
                Assert.Equal("div", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                var requiredAttribute = Assert.Single(rule.Attributes);
                Assert.Equal("@bind-myprop", requiredAttribute.DisplayName);
                Assert.Equal("@bind-myprop", requiredAttribute.Name);

                var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
                Assert.Equal("@bind-myprop", attribute.Name);
                Assert.Equal("Bind_myprop", attribute.PropertyName);
                Assert.Equal("object Test.BindAttributes.Bind_myprop", attribute.DisplayName);

                attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format", StringComparison.Ordinal));
                Assert.Equal("format-myprop", attribute.Name);
                Assert.Equal("Format_myprop", attribute.PropertyName);
                Assert.Equal("string Test.BindAttributes.Format_myprop", attribute.DisplayName);
            },
            rule =>
            {
                Assert.Equal("div", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(rule.Attributes.OrderBy(a => a.Name),
                    requiredAttribute =>
                    {
                        Assert.Equal("@bind-myprop:get", requiredAttribute.DisplayName);
                        Assert.Equal("@bind-myprop:get", requiredAttribute.Name);
                    },
                    requiredAttribute =>
                    {
                        Assert.Equal("@bind-myprop:set", requiredAttribute.DisplayName);
                        Assert.Equal("@bind-myprop:set", requiredAttribute.Name);
                    });
            });

        var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
        Assert.Equal("@bind-myprop", attribute.Name);
        Assert.Equal("Bind_myprop", attribute.PropertyName);
        Assert.Equal("object Test.BindAttributes.Bind_myprop", attribute.DisplayName);

        attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format", StringComparison.Ordinal));
        Assert.Equal("format-myprop", attribute.Name);
        Assert.Equal("Format_myprop", attribute.PropertyName);
        Assert.Equal("string Test.BindAttributes.Format_myprop", attribute.DisplayName);
    }

    [Fact]
    public void GetTagHelpers_BindOnInputElementWithoutTypeAttribute_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(null, null, ""myprop"", ""myevent"", false, null)]
    public class BindAttributes
    {
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
        var bind = Assert.Single(matches);

        Assert.Equal("myprop", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("myevent", ((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.Null(((BindMetadata)bind.Metadata).TypeAttribute);
        Assert.True(bind.IsInputElementBindTagHelper());
        Assert.True(bind.IsInputElementFallbackBindTagHelper());

        Assert.Collection(bind.TagMatchingRules.OrderBy(r => r.Attributes.Length),
            rule =>
            {
                Assert.Equal("input", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                var requiredAttribute = Assert.Single(rule.Attributes);
                Assert.Equal("@bind", requiredAttribute.DisplayName);
                Assert.Equal("@bind", requiredAttribute.Name);
            },
            rule =>
            {
                Assert.Equal("input", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(rule.Attributes.OrderBy(o => o.Name),
                    requiredAttribute =>
                    {
                        Assert.Equal("@bind:get", requiredAttribute.DisplayName);
                        Assert.Equal("@bind:get", requiredAttribute.Name);
                    },
                    requiredAttribute =>
                    {
                        Assert.Equal("@bind:set", requiredAttribute.DisplayName);
                        Assert.Equal("@bind:set", requiredAttribute.Name);
                    });
            });

        var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
        Assert.Equal("@bind", attribute.Name);
        Assert.Equal("Bind", attribute.PropertyName);
        Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

        var parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("format"));
        Assert.Equal("format", parameter.Name);
        Assert.Equal("Format_myprop", parameter.PropertyName);
        Assert.Equal(":format", parameter.DisplayName);
    }

    [Fact]
    public void GetTagHelpers_BindOnInputElementWithTypeAttribute_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""checkbox"", null, ""myprop"", ""myevent"", false, null)]
    public class BindAttributes
    {
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
        var bind = Assert.Single(matches);

        Assert.Equal("myprop", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("myevent", ((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.Equal("checkbox", ((BindMetadata)bind.Metadata).TypeAttribute);
        Assert.True(bind.IsInputElementBindTagHelper());
        Assert.False(bind.IsInputElementFallbackBindTagHelper());

        Assert.Collection(bind.TagMatchingRules.OrderBy(r => r.Attributes.Length),
            rule =>
            {
                Assert.Equal("input", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(
                    rule.Attributes,
                    a =>
                    {
                        Assert.Equal("type", a.DisplayName);
                        Assert.Equal("type", a.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, a.NameComparison);
                        Assert.Equal("checkbox", a.Value);
                        Assert.Equal(RequiredAttributeValueComparison.FullMatch, a.ValueComparison);
                    },
                    a =>
                    {
                        Assert.Equal("@bind", a.DisplayName);
                        Assert.Equal("@bind", a.Name);
                    });
            },
            rule =>
            {
                Assert.Equal("input", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(
                    rule.Attributes,
                    a =>
                    {
                        Assert.Equal("type", a.DisplayName);
                        Assert.Equal("type", a.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, a.NameComparison);
                        Assert.Equal("checkbox", a.Value);
                        Assert.Equal(RequiredAttributeValueComparison.FullMatch, a.ValueComparison);
                    },
                    a =>
                    {
                        Assert.Equal("@bind:get", a.DisplayName);
                        Assert.Equal("@bind:get", a.Name);
                    },
                    a =>
                    {
                        Assert.Equal("@bind:set", a.DisplayName);
                        Assert.Equal("@bind:set", a.Name);
                    });
            });

        var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
        Assert.Equal("@bind", attribute.Name);
        Assert.Equal("Bind", attribute.PropertyName);
        Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

        var parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("format"));
        Assert.Equal("format", parameter.Name);
        Assert.Equal("Format_myprop", parameter.PropertyName);
        Assert.Equal(":format", parameter.DisplayName);
    }

    [Fact]
    public void GetTagHelpers_BindOnInputElementWithTypeAttributeAndSuffix_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""checkbox"", ""somevalue"", ""myprop"", ""myevent"", false, null)]
    public class BindAttributes
    {
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
        var bind = Assert.Single(matches);

        Assert.Equal("myprop", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("myevent", ((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.Equal("checkbox", ((BindMetadata)bind.Metadata).TypeAttribute);
        Assert.True(bind.IsInputElementBindTagHelper());
        Assert.False(bind.IsInputElementFallbackBindTagHelper());
        Assert.False(bind.IsInvariantCultureBindTagHelper());
        Assert.Null(bind.GetFormat());

        Assert.Collection(bind.TagMatchingRules.OrderBy(o => o.Attributes.Length),
            rule =>
            {
                Assert.Equal("input", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(
                    rule.Attributes,
                    a =>
                    {
                        Assert.Equal("type", a.DisplayName);
                        Assert.Equal("type", a.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, a.NameComparison);
                        Assert.Equal("checkbox", a.Value);
                        Assert.Equal(RequiredAttributeValueComparison.FullMatch, a.ValueComparison);
                    },
                    a =>
                    {
                        Assert.Equal("@bind-somevalue", a.DisplayName);
                        Assert.Equal("@bind-somevalue", a.Name);
                    });
            },
            rule =>
            {
                Assert.Equal("input", rule.TagName);
                Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

                Assert.Collection(
                    rule.Attributes,
                    a =>
                    {
                        Assert.Equal("type", a.DisplayName);
                        Assert.Equal("type", a.Name);
                        Assert.Equal(RequiredAttributeNameComparison.FullMatch, a.NameComparison);
                        Assert.Equal("checkbox", a.Value);
                        Assert.Equal(RequiredAttributeValueComparison.FullMatch, a.ValueComparison);
                    },
                    a =>
                    {
                        Assert.Equal("@bind-somevalue:get", a.DisplayName);
                        Assert.Equal("@bind-somevalue:get", a.Name);
                    },
                    a =>
                    {
                        Assert.Equal("@bind-somevalue:set", a.DisplayName);
                        Assert.Equal("@bind-somevalue:set", a.Name);
                    });
            });

        var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
        Assert.Equal("@bind-somevalue", attribute.Name);
        Assert.Equal("Bind_somevalue", attribute.PropertyName);
        Assert.Equal("object Test.BindAttributes.Bind_somevalue", attribute.DisplayName);

        var parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("format"));
        Assert.Equal("format", parameter.Name);
        Assert.Equal("Format_somevalue", parameter.PropertyName);
        Assert.Equal(":format", parameter.DisplayName);
    }

    [Fact]
    public void GetTagHelpers_BindOnInputElementWithTypeAttributeAndSuffixAndInvariantCultureAndFormat_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""number"", null, ""value"", ""onchange"", isInvariantCulture: true, format: ""0.00"")]
    public class BindAttributes
    {
    }
}
"));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetBindTagHelpers(result);
        matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
        var bind = Assert.Single(matches);

        Assert.Equal("value", ((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Equal("onchange", ((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.Equal("number", ((BindMetadata)bind.Metadata).TypeAttribute);
        Assert.True(bind.IsInputElementBindTagHelper());
        Assert.False(bind.IsInputElementFallbackBindTagHelper());
        Assert.True(bind.IsInvariantCultureBindTagHelper());
        Assert.Equal("0.00", bind.GetFormat());
    }

    [Fact]
    public void GetTagHelpers_BindFallback_CreatesTagHelper()
    {
        // Arrange
        var compilation = BaseCompilation;
        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var bind = Assert.Single(result, r => r.IsFallbackBindTagHelper());

        // These are features Bind Tags Helpers don't use. Verifying them once here and
        // then ignoring them.
        Assert.Empty(bind.AllowedChildTags);
        Assert.Null(bind.TagOutputHint);

        // These are features that are invariants of all Bind Tag Helpers. Verifying them once
        // here and then ignoring them.
        Assert.Empty(bind.Diagnostics);
        Assert.False(bind.HasErrors);
        Assert.Equal(TagHelperKind.Bind, bind.Kind);
        Assert.Equal(RuntimeKind.None, bind.RuntimeKind);
        Assert.False(bind.IsDefaultKind());
        Assert.False(bind.KindUsesDefaultTagHelperRuntime());
        Assert.False(bind.IsComponentOrChildContentTagHelper());
        Assert.True(bind.CaseSensitive);
        Assert.True(bind.ClassifyAttributesOnly);

        Assert.Null(((BindMetadata)bind.Metadata).ValueAttribute);
        Assert.Null(((BindMetadata)bind.Metadata).ChangeAttribute);
        Assert.True(bind.IsFallbackBindTagHelper());

        Assert.Equal(
            "Binds the provided expression to an attribute and a change event, based on the naming of " +
                "the bind attribute. For example: <code>@bind-value=\"...\"</code> and <code>@bind-value:event=\"onchange\"</code> will assign the " +
                "current value of the expression to the 'value' attribute, and assign a delegate that attempts " +
                "to set the value to the 'onchange' attribute.",
            bind.Documentation);

        // These are all trivially derived from the assembly/namespace/type name
        Assert.Equal("Microsoft.AspNetCore.Components", bind.AssemblyName);
        Assert.Equal("Bind", bind.Name);
        Assert.Equal("Microsoft.AspNetCore.Components.Bind", bind.DisplayName);
        Assert.Equal("Microsoft.AspNetCore.Components.Bind", bind.TypeName);

        // The tag matching rule for a bind-Component is always the component name + the attribute name
        var rule = Assert.Single(bind.TagMatchingRules);
        Assert.Empty(rule.Diagnostics);
        Assert.False(rule.HasErrors);
        Assert.Null(rule.ParentTag);
        Assert.Equal("*", rule.TagName);
        Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

        var requiredAttribute = Assert.Single(rule.Attributes);
        Assert.Empty(requiredAttribute.Diagnostics);
        Assert.Equal("@bind-...", requiredAttribute.DisplayName);
        Assert.Equal("@bind-", requiredAttribute.Name);
        Assert.Equal(RequiredAttributeNameComparison.PrefixMatch, requiredAttribute.NameComparison);
        Assert.Null(requiredAttribute.Value);
        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);

        var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));

        // Invariants
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(TagHelperKind.Bind, attribute.Parent.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);

        Assert.True(attribute.HasIndexer);
        Assert.Equal("@bind-", attribute.IndexerNamePrefix);
        Assert.Equal("System.Object", attribute.IndexerTypeName);

        Assert.Equal(
            "Binds the provided expression to an attribute and a change event, based on the naming of " +
                "the bind attribute. For example: <code>@bind-value=\"...\"</code> and <code>@bind-value:event=\"onchange\"</code> will assign the " +
                "current value of the expression to the 'value' attribute, and assign a delegate that attempts " +
                "to set the value to the 'onchange' attribute.",
            attribute.Documentation);

        Assert.Equal("@bind-...", attribute.Name);
        Assert.Equal("Bind", attribute.PropertyName);
        Assert.Equal(
            "System.Collections.Generic.Dictionary<string, object> Microsoft.AspNetCore.Components.Bind.Bind",
            attribute.DisplayName);

        // Defined from the property type
        Assert.Equal("System.Collections.Generic.Dictionary<string, object>", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);

        var parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("format"));

        // Invariants
        Assert.Empty(parameter.Diagnostics);
        Assert.False(parameter.HasErrors);
        Assert.Equal(TagHelperKind.Bind, parameter.Parent.Parent.Kind);
        Assert.False(parameter.IsDefaultKind());

        Assert.Equal(
            "Specifies a format to convert the value specified by the corresponding bind attribute. " +
                "For example: <code>@bind-value:format=\"...\"</code> will apply a format string to the value " +
                "specified in <code>@bind-value=\"...\"</code>. The format string can currently only be used with " +
                "expressions of type <code>DateTime</code>.",
            parameter.Documentation);

        Assert.Equal("format", parameter.Name);
        Assert.Equal("Format", parameter.PropertyName);
        Assert.Equal(":format", parameter.DisplayName);

        // Defined from the property type
        Assert.Equal("System.String", parameter.TypeName);
        Assert.True(parameter.IsStringProperty);
        Assert.False(parameter.IsBooleanProperty);
        Assert.False(parameter.IsEnum);

        parameter = Assert.Single(attribute.Parameters, a => a.Name.Equals("culture"));

        // Invariants
        Assert.Empty(parameter.Diagnostics);
        Assert.False(parameter.HasErrors);
        Assert.Equal(TagHelperKind.Bind, parameter.Parent.Parent.Kind);
        Assert.False(parameter.IsDefaultKind());

        Assert.Equal(
            "Specifies the culture to use for conversions.",
            parameter.Documentation);

        Assert.Equal("culture", parameter.Name);
        Assert.Equal("Culture", parameter.PropertyName);
        Assert.Equal(":culture", parameter.DisplayName);

        // Defined from the property type
        Assert.Equal("System.Globalization.CultureInfo", parameter.TypeName);
        Assert.False(parameter.IsStringProperty);
        Assert.False(parameter.IsBooleanProperty);
        Assert.False(parameter.IsEnum);
    }

    private static TagHelperCollection GetBindTagHelpers(TagHelperCollection collection)
        => collection.Where(static t => t.Kind == TagHelperKind.Bind && !IsBuiltInComponent(t));
}
