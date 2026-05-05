// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

public class DefaultTagHelperDescriptorFactoryTest : TagHelperDescriptorProviderTestBase
{
    public DefaultTagHelperDescriptorFactoryTest() : base(AdditionalCode)
    {
        Compilation = BaseCompilation;
    }

    private Compilation Compilation { get; }

    public static TheoryData<string, Action<RequiredAttributeDescriptorBuilder>> RequiredAttributeParserErrorData
        => new()
        {
            ("name,", static b => b
                .Name("name")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("name,"))),
            (" ", static b => b
                .Name(string.Empty)
                .AddDiagnostic(AspNetCore.Razor.Language.RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace())),
            ("n@me", static b => b
                .Name("n@me")
                .AddDiagnostic(AspNetCore.Razor.Language.RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName("n@me", '@'))),
            ("name extra", static b => b
                .Name("name")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeCharacter('e', "name extra"))),
            ("[[ ", static b => b
                .Name("[")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("[[ "))
            ),
            ("[ ", static b => b
                .Name("")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("[ "))
            ),
            ("[name='unended]", static b => b
                .Name("name")
                .ValueComparison(RequiredAttributeValueComparison.FullMatch)
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes('\'', "[name='unended]"))
            ),
            ("[name='unended", static b => b
                .Name("name")
                .ValueComparison(RequiredAttributeValueComparison.FullMatch)
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes('\'', "[name='unended"))
            ),
            ("[name", static b => b
                .Name("name")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("[name"))
            ),
            ("[ ]", static b => b
                .Name(string.Empty)
                .AddDiagnostic(AspNetCore.Razor.Language.RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace())
            ),
            ("[name='unended]", static b => b
                .Name("name")
                .ValueComparison(RequiredAttributeValueComparison.FullMatch)
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes('\'', "[name='unended]"))
            ),
            ("[name='unended", static b => b
                .Name("name")
                .ValueComparison(RequiredAttributeValueComparison.FullMatch)
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes('\'', "[name='unended"))
            ),
            ("[name", static b => b
                .Name("name")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("[name"))
            ),
            ("[n@me]", static b => b
                .Name("n@me")
                .AddDiagnostic(AspNetCore.Razor.Language.RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName("n@me", '@'))
            ),
            ("[name@]", static b => b
                .Name("name@")
                .AddDiagnostic(AspNetCore.Razor.Language.RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName("name@", '@'))
            ),
            ("[name^]", static b => b
                .Name("name")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_PartialRequiredAttributeOperator('^', "[name^]"))
            ),
            ("[name='value'", static b => b
                .Name("name")
                .Value("value", RequiredAttributeValueComparison.FullMatch)
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("[name='value'"))
            ),
            ("[name ", static b => b
                .Name("name")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("[name "))
            ),
            ("[name extra]", static b => b
                .Name("name")
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeOperator('e', "[name extra]"))
            ),
            ("[name=value ", static b => b
                .Name("name")
                .Value("value", RequiredAttributeValueComparison.FullMatch)
                .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace("[name=value "))
            )
        };

    [Theory]
    [MemberData(nameof(RequiredAttributeParserErrorData))]
    public void RequiredAttributeParser_ParsesRequiredAttributesAndLogsDiagnosticsCorrectly(
        string requiredAttributes,
        Action<RequiredAttributeDescriptorBuilder> configure)
    {
        // Arrange
        var expected = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(rule =>
            {
                rule.Attribute(configure);
            })
            .Build();

        // Act
        var actual = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(rule =>
            {
                RequiredAttributeParser.AddRequiredAttributes(requiredAttributes, rule);
            })
            .Build();

        // Assert
        Assert.Equal<RequiredAttributeDescriptor>(expected.TagMatchingRules[0].Attributes, actual.TagMatchingRules[0].Attributes);
    }

    public static TheoryData<string, ImmutableArray<Action<RequiredAttributeDescriptorBuilder>>> RequiredAttributeParserData
    {
        get
        {
            return new()
            {
                (null!, []),
                (string.Empty, []),
                ("name", [plain("name", RequiredAttributeNameComparison.FullMatch)]),
                ("name-*", [plain("name-", RequiredAttributeNameComparison.PrefixMatch)]),
                ("  name-*   ", [plain("name-", RequiredAttributeNameComparison.PrefixMatch)]),
                ("asp-route-*,valid  ,  name-*   ,extra", [
                    plain("asp-route-", RequiredAttributeNameComparison.PrefixMatch),
                    plain("valid", RequiredAttributeNameComparison.FullMatch),
                    plain("name-", RequiredAttributeNameComparison.PrefixMatch),
                    plain("extra", RequiredAttributeNameComparison.FullMatch)]),
                ("[name]", [css("name", null, RequiredAttributeValueComparison.None)]),
                ("[ name ]", [css("name", null, RequiredAttributeValueComparison.None)]),
                (" [ name ] ", [css("name", null, RequiredAttributeValueComparison.None)]),
                ("[name=]", [css("name", "", RequiredAttributeValueComparison.FullMatch)]),
                ("[name='']", [css("name", "", RequiredAttributeValueComparison.FullMatch)]),
                ("[name ^=]", [css("name", "", RequiredAttributeValueComparison.PrefixMatch)]),
                ("[name=hello]", [css("name", "hello", RequiredAttributeValueComparison.FullMatch)]),
                ("[name= hello]", [css("name", "hello", RequiredAttributeValueComparison.FullMatch)]),
                ("[name='hello']", [css("name", "hello", RequiredAttributeValueComparison.FullMatch)]),
                ("[name=\"hello\"]", [css("name", "hello", RequiredAttributeValueComparison.FullMatch)]),
                (" [ name  $= \" hello\" ]  ", [css("name", " hello", RequiredAttributeValueComparison.SuffixMatch)]),
                ("[name=\"hello\"],[other^=something ], [val = 'cool']", [
                    css("name", "hello", RequiredAttributeValueComparison.FullMatch),
                    css("other", "something", RequiredAttributeValueComparison.PrefixMatch),
                    css("val", "cool", RequiredAttributeValueComparison.FullMatch)]),
                ("asp-route-*,[name=\"hello\"],valid  ,[other^=something ],   name-*   ,[val = 'cool'],extra", [
                    plain("asp-route-", RequiredAttributeNameComparison.PrefixMatch),
                    css("name", "hello", RequiredAttributeValueComparison.FullMatch),
                    plain("valid", RequiredAttributeNameComparison.FullMatch),
                    css("other", "something", RequiredAttributeValueComparison.PrefixMatch),
                    plain("name-", RequiredAttributeNameComparison.PrefixMatch),
                    css("val", "cool", RequiredAttributeValueComparison.FullMatch),
                    plain("extra", RequiredAttributeNameComparison.FullMatch)]),
            };

            static Action<RequiredAttributeDescriptorBuilder> plain(string name, RequiredAttributeNameComparison nameComparison)
            {
                return builder => builder
                    .Name(name, nameComparison);
            }

            static Action<RequiredAttributeDescriptorBuilder> css(string name, string? value, RequiredAttributeValueComparison valueComparison)
            {
                return builder => builder
                    .Name(name)
                    .Value(value, valueComparison);
            }
        }
    }

    [Theory]
    [MemberData(nameof(RequiredAttributeParserData))]
    public void RequiredAttributeParser_ParsesRequiredAttributesCorrectly(
        string requiredAttributes,
        ImmutableArray<Action<RequiredAttributeDescriptorBuilder>> configures)
    {
        // Arrange
        var expected = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(rule =>
            {
                foreach (var configure in configures)
                {
                    rule.Attribute(configure);
                }
            })
            .Build();

        // Act
        var actual = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(rule =>
            {
                RequiredAttributeParser.AddRequiredAttributes(requiredAttributes, rule);
            })
            .Build();

        // Assert
        Assert.Equal<RequiredAttributeDescriptor>(expected.TagMatchingRules[0].Attributes, actual.TagMatchingRules[0].Attributes);
    }

    // tagHelperType, expectedDescriptor
    public static TheoryData<string, TagHelperDescriptor?> IsEnumData
        => new()
        {
            NameAndTagHelper("EnumTagHelper", static b => b
                .TagMatchingRule(tagName: "enum")
                .BoundAttribute<int>(name: "non-enum-property", propertyName: "NonEnumProperty")
                .BoundAttribute(name: "enum-property", propertyName: "EnumProperty", typeName: "TestNamespace.CustomEnum", static b => b
                    .AsEnum())),
            NameAndTagHelper("MultiEnumTagHelper", static b => b
                .TagMatchingRule(tagName: "p")
                .TagMatchingRule(tagName: "input")
                .BoundAttribute<int>(name: "non-enum-property", propertyName: "NonEnumProperty")
                .BoundAttribute(name: "enum-property", propertyName: "EnumProperty", typeName: "TestNamespace.CustomEnum", static b => b
                    .AsEnum())),
            NameAndTagHelper("NestedEnumTagHelper", static b => b
                .TagMatchingRule(tagName: "nested-enum")
                .BoundAttribute(name: "nested-enum-property", propertyName: "NestedEnumProperty", typeName: "TestNamespace.NestedEnumTagHelper.NestedEnum", static b => b
                    .AsEnum())
                .BoundAttribute<int>(name: "non-enum-property", propertyName: "NonEnumProperty")
                .BoundAttribute(name: "enum-property", propertyName: "EnumProperty", typeName: "TestNamespace.CustomEnum", static b => b
                    .AsEnum())),
        };

    [Theory]
    [MemberData(nameof(IsEnumData))]
    public void CreateDescriptor_IsEnumIsSetCorrectly(string tagHelperTypeName, TagHelperDescriptor? expectedDescriptor)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // tagHelperType, expectedDescriptor
    public static TheoryData<string, TagHelperDescriptor?> RequiredParentData
        => new()
        {
            NameAndTagHelper("RequiredParentTagHelper", static b => b
                .TagMatchingRule(tagName: "input", parentTagName: "div")),
            NameAndTagHelper("MultiSpecifiedRequiredParentTagHelper", static b => b
                .TagMatchingRule(tagName: "p", parentTagName: "div")
                .TagMatchingRule(tagName: "input", parentTagName: "section")),
            NameAndTagHelper("MultiWithUnspecifiedRequiredParentTagHelper", static b => b
                .TagMatchingRule(tagName: "p")
                .TagMatchingRule(tagName: "input", parentTagName: "div"))
        };

    [Theory]
    [MemberData(nameof(RequiredParentData))]
    public void CreateDescriptor_CreatesDesignTimeDescriptorsWithRequiredParent(string tagHelperTypeName, TagHelperDescriptor? expectedDescriptor)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // tagHelperType, expectedDescriptor
    public static TheoryData<string, TagHelperDescriptor?> RestrictChildrenData
        => new()
        {
            NameAndTagHelper("RestrictChildrenTagHelper", static b => b
                .TagMatchingRule(tagName: "restrict-children")
                .AllowedChildTag(tagName: "p")),
            NameAndTagHelper("DoubleRestrictChildrenTagHelper", static b => b
                .TagMatchingRule(tagName: "double-restrict-children")
                .AllowedChildTag(tagName: "p")
                .AllowedChildTag(tagName: "strong")),
            NameAndTagHelper("MultiTargetRestrictChildrenTagHelper", static b => b
                .TagMatchingRule(tagName: "p")
                .TagMatchingRule(tagName: "div")
                .AllowedChildTag(tagName: "p")
                .AllowedChildTag(tagName: "strong")),
        };

    [Theory]
    [MemberData(nameof(RestrictChildrenData))]
    public void CreateDescriptor_CreatesDescriptorsWithAllowedChildren(string tagHelperTypeName, TagHelperDescriptor? expectedDescriptor)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // tagHelperType, expectedDescriptor
    public static TheoryData<string, TagHelperDescriptor?> TagStructureData
        => new()
        {
            NameAndTagHelper("TagStructureTagHelper", static b => b
                .TagMatchingRule(tagName: "input", tagStructure: TagStructure.WithoutEndTag)),
            NameAndTagHelper("MultiSpecifiedTagStructureTagHelper", static b => b
                .TagMatchingRule(tagName: "p", tagStructure: TagStructure.NormalOrSelfClosing)
                .TagMatchingRule(tagName: "input", tagStructure: TagStructure.WithoutEndTag)),
            NameAndTagHelper("MultiWithUnspecifiedTagStructureTagHelper", static b => b
                .TagMatchingRule(tagName: "p")
                .TagMatchingRule(tagName: "input", tagStructure: TagStructure.WithoutEndTag))
        };

    [Theory]
    [MemberData(nameof(TagStructureData))]
    public void CreateDescriptor_CreatesDesignTimeDescriptorsWithTagStructure(string tagHelperTypeFullName, TagHelperDescriptor? expectedDescriptor)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeFullName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // tagHelperType, designTime, expectedDescriptor
    public static TheoryData<string, bool, TagHelperDescriptor?> EditorBrowsableData
    {
        get
        {
            return new()
            {
                NameAndTagHelper("InheritedEditorBrowsableTagHelper", designTime: true, static b => b
                    .TagMatchingRule(tagName: "inherited-editor-browsable")
                    .BoundAttribute<int>(name: "property", propertyName: "Property")),
                NameAndTagHelper("EditorBrowsableTagHelper", designTime: true, configure: null),
                NameAndTagHelper("EditorBrowsableTagHelper", designTime: false, static b => b
                    .TagMatchingRule(tagName: "editor-browsable")
                    .BoundAttribute<int>(name: "property", propertyName: "Property")),
                NameAndTagHelper("HiddenPropertyEditorBrowsableTagHelper", designTime: true, static b => b
                    .TagMatchingRule(tagName: "hidden-property-editor-browsable")),
                NameAndTagHelper("HiddenPropertyEditorBrowsableTagHelper", designTime: false, static b => b
                    .TagMatchingRule(tagName: "hidden-property-editor-browsable")
                    .BoundAttribute<int>(name: "property", propertyName: "Property")),
                NameAndTagHelper("OverriddenEditorBrowsableTagHelper", designTime: true, static b => b
                    .TagMatchingRule(tagName: "overridden-editor-browsable")
                    .BoundAttribute<int>(name: "property", propertyName: "Property")),
                NameAndTagHelper("MultiPropertyEditorBrowsableTagHelper", designTime: true, static b => b
                    .TagMatchingRule(tagName: "multi-property-editor-browsable")
                    .BoundAttribute<int>(name: "property2", propertyName: "Property2")),
                NameAndTagHelper("MultiPropertyEditorBrowsableTagHelper", designTime: false, static b => b
                    .TagMatchingRule(tagName: "multi-property-editor-browsable")
                    .BoundAttribute<int>(name: "property", propertyName: "Property")
                    .BoundAttribute<int>(name: "property2", propertyName: "Property2")),
                NameAndTagHelper("OverriddenPropertyEditorBrowsableTagHelper", designTime: true, static b => b
                    .TagMatchingRule(tagName: "overridden-property-editor-browsable")),
                NameAndTagHelper("OverriddenPropertyEditorBrowsableTagHelper", designTime: false, static b => b
                    .TagMatchingRule(tagName: "overridden-property-editor-browsable")
                    .BoundAttribute<int>(name: "property2", propertyName: "Property2")
                    .BoundAttribute<int>(name: "property", propertyName: "Property")),
                NameAndTagHelper("DefaultEditorBrowsableTagHelper", designTime: true, static b => b
                    .TagMatchingRule(tagName: "default-editor-browsable")
                    .BoundAttribute<int>(name: "property", propertyName: "Property")),
                NameAndTagHelper("MultiEditorBrowsableTagHelper", designTime: true, configure: null)
            };
        }
    }

    [Theory]
    [MemberData(nameof(EditorBrowsableData))]
    public void CreateDescriptor_UnderstandsEditorBrowsableAttribute(string tagHelperTypeName, bool designTime, TagHelperDescriptor? expectedDescriptor)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(designTime, designTime);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // tagHelperType, expectedDescriptor
    public static TheoryData<string, TagHelperDescriptor?> AttributeTargetData
        => new()
        {
            NameAndTagHelper("AttributeTargetingTagHelper", static b => b
                .TagMatchingRule(tagName: TagHelperMatchingConventions.ElementCatchAllName, static b => b
                    .RequiredAttribute(name: "class"))),
            NameAndTagHelper("MultiAttributeTargetingTagHelper", static b => b
                .TagMatchingRule(tagName: TagHelperMatchingConventions.ElementCatchAllName, static b => b
                    .RequiredAttribute(name: "class")
                    .RequiredAttribute(name: "style"))),
            NameAndTagHelper("MultiAttributeAttributeTargetingTagHelper", static b => b
                .TagMatchingRule(tagName: TagHelperMatchingConventions.ElementCatchAllName, static b => b
                    .RequiredAttribute(name: "custom"))
                .TagMatchingRule(tagName: TagHelperMatchingConventions.ElementCatchAllName, static b => b
                    .RequiredAttribute(name: "class")
                    .RequiredAttribute(name: "style"))),
            NameAndTagHelper("InheritedAttributeTargetingTagHelper", static b => b
                .TagMatchingRule(tagName: TagHelperMatchingConventions.ElementCatchAllName, static b => b
                    .RequiredAttribute(name: "style"))),
            NameAndTagHelper("RequiredAttributeTagHelper", static b => b
                .TagMatchingRule(tagName: "input", static b => b
                    .RequiredAttribute(name: "class"))),
            NameAndTagHelper("InheritedRequiredAttributeTagHelper", static b => b
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "class"))),
            NameAndTagHelper("MultiAttributeRequiredAttributeTagHelper", static b => b
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "class"))
                .TagMatchingRule(tagName: "input", static b => b
                    .RequiredAttribute(name: "class"))),
            NameAndTagHelper("MultiAttributeSameTagRequiredAttributeTagHelper", static b => b
                .TagMatchingRule(tagName: "input", static b => b
                    .RequiredAttribute(name: "style"))
                .TagMatchingRule(tagName: "input", static b => b
                    .RequiredAttribute(name: "class"))),
            NameAndTagHelper("MultiRequiredAttributeTagHelper", static b => b
                .TagMatchingRule(tagName: "input", static b => b
                    .RequiredAttribute(name: "class")
                    .RequiredAttribute(name: "style"))),
            NameAndTagHelper("MultiTagMultiRequiredAttributeTagHelper", static b => b
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "class")
                    .RequiredAttribute(name: "style"))
                .TagMatchingRule(tagName: "input", static b => b
                    .RequiredAttribute(name: "class")
                    .RequiredAttribute(name: "style"))),
            NameAndTagHelper("AttributeWildcardTargetingTagHelper", static b => b
                .TagMatchingRule(tagName: TagHelperMatchingConventions.ElementCatchAllName, static b => b
                    .RequiredAttribute(name: "class", nameComparison: RequiredAttributeNameComparison.PrefixMatch))),
            NameAndTagHelper("MultiAttributeWildcardTargetingTagHelper", static b => b
                .TagMatchingRule(tagName: TagHelperMatchingConventions.ElementCatchAllName, static b => b
                    .RequiredAttribute(name: "class", nameComparison: RequiredAttributeNameComparison.PrefixMatch)
                    .RequiredAttribute(name: "style", nameComparison: RequiredAttributeNameComparison.PrefixMatch)))
        };

    [Theory]
    [MemberData(nameof(AttributeTargetData))]
    public void CreateDescriptor_ReturnsExpectedDescriptors(string tagHelperTypeName, TagHelperDescriptor? expectedDescriptor)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // tagHelperType, expectedTagName, expectedAttributeName
    public static TheoryData<string, string, string> HtmlCaseData
        => new()
        {
            ("TestNamespace.SingleAttributeTagHelper", "single-attribute", "int-attribute"),
            ("TestNamespace.ALLCAPSTAGHELPER", "allcaps", "allcapsattribute"),
            ("TestNamespace.CAPSOnOUTSIDETagHelper", "caps-on-outside", "caps-on-outsideattribute"),
            ("TestNamespace.capsONInsideTagHelper", "caps-on-inside", "caps-on-insideattribute"),
            ("TestNamespace.One1Two2Three3TagHelper", "one1-two2-three3", "one1-two2-three3-attribute"),
            ("TestNamespace.ONE1TWO2THREE3TagHelper", "one1two2three3", "one1two2three3-attribute"),
            ("TestNamespace.First_Second_ThirdHiTagHelper", "first_second_third-hi", "first_second_third-attribute"),
            ("TestNamespace.UNSuffixedCLASS", "un-suffixed-class", "un-suffixed-attribute"),
        };

    [Theory]
    [MemberData(nameof(HtmlCaseData))]
    public void CreateDescriptor_HtmlCasesTagNameAndAttributeName(string tagHelperTypeName, string expectedTagName, string expectedAttributeName)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.NotNull(descriptor);
        var rule = Assert.Single(descriptor.TagMatchingRules);
        Assert.Equal(expectedTagName, rule.TagName, StringComparer.Ordinal);
        var attributeDescriptor = Assert.Single(descriptor.BoundAttributes);
        Assert.Equal(expectedAttributeName, attributeDescriptor.Name);
    }

    [Fact]
    public void CreateDescriptor_OverridesAttributeNameFromAttribute()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("OverriddenAttributeTagHelper", static b => b
            .TagMatchingRule(tagName: "overridden-attribute")
            .BoundAttribute<string>(name: "SomethingElse", propertyName: "ValidAttribute1")
            .BoundAttribute<string>(name: "Something-Else", propertyName: "ValidAttribute2"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.OverriddenAttributeTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_DoesNotInheritOverridenAttributeName()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("InheritedOverriddenAttributeTagHelper", static b => b
            .TagMatchingRule(tagName: "inherited-overridden-attribute")
            .BoundAttribute<string>(name: "valid-attribute1", propertyName: "ValidAttribute1")
            .BoundAttribute<string>(name: "Something-Else", propertyName: "ValidAttribute2"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.InheritedOverriddenAttributeTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_AllowsOverriddenAttributeNameOnUnimplementedVirtual()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("InheritedNotOverriddenAttributeTagHelper", static b => b
            .TagMatchingRule(tagName: "inherited-not-overridden-attribute")
            .BoundAttribute<string>(name: "SomethingElse", propertyName: "ValidAttribute1")
            .BoundAttribute<string>(name: "Something-Else", propertyName: "ValidAttribute2"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.InheritedNotOverriddenAttributeTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_BuildsDescriptorsWithInheritedProperties()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("InheritedSingleAttributeTagHelper", static b => b
            .TagMatchingRule(tagName: "inherited-single-attribute")
            .BoundAttribute<int>(name: "int-attribute", propertyName: "IntAttribute"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.InheritedSingleAttributeTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_BuildsDescriptorsWithConventionNames()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("SingleAttributeTagHelper", static b => b
            .TagMatchingRule(tagName: "single-attribute")
            .BoundAttribute<int>(name: "int-attribute", propertyName: "IntAttribute"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.SingleAttributeTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_OnlyAcceptsPropertiesWithGetAndSet()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("MissingAccessorTagHelper", static b => b
            .TagMatchingRule(tagName: "missing-accessor")
            .BoundAttribute<string>(name: "valid-attribute", propertyName: "ValidAttribute"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.MissingAccessorTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_OnlyAcceptsPropertiesWithPublicGetAndSet()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("NonPublicAccessorTagHelper", static b => b
            .TagMatchingRule(tagName: "non-public-accessor")
            .BoundAttribute<string>(name: "valid-attribute", propertyName: "ValidAttribute"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.NonPublicAccessorTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_DoesNotIncludePropertiesWithNotBound()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("NotBoundAttributeTagHelper", static b => b
            .TagMatchingRule(tagName: "not-bound-attribute")
            .BoundAttribute<object>(name: "bound-property", propertyName: "BoundProperty"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.NotBoundAttributeTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_ResolvesMultipleTagHelperDescriptorsFromSingleType()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("MultiTagTagHelper", static b => b
            .TagMatchingRule(tagName: "p")
            .TagMatchingRule(tagName: "div")
            .BoundAttribute<string>(name: "valid-attribute", propertyName: "ValidAttribute"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.MultiTagTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_DoesNotResolveInheritedTagNames()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("InheritedMultiTagTagHelper", static b => b
            .TagMatchingRule(tagName: "inherited-multi-tag")
            .BoundAttribute<string>(name: "valid-attribute", propertyName: "ValidAttribute"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.InheritedMultiTagTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_IgnoresDuplicateTagNamesFromAttribute()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("DuplicateTagNameTagHelper", static b => b
            .TagMatchingRule(tagName: "p")
            .TagMatchingRule(tagName: "div"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.DuplicateTagNameTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_OverridesTagNameFromAttribute()
    {
        // Arrange
        var expectedDescriptor = CreateTagHelper("OverrideNameTagHelper", static b => b
            .TagMatchingRule(tagName: "data-condition"));

        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName("TestNamespace.OverrideNameTagHelper");
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // name, expectedErrorMessages
    public static TheoryData<string, string[]> InvalidNameData
    {
        get
        {
            var whitespaceErrorString = "Targeted tag name cannot be null or whitespace.";

            var data = GetInvalidNameOrPrefixData(onNameError, whitespaceErrorString, onDataError: null);
            data.Add(string.Empty, [whitespaceErrorString]);

            return data;

            static string onNameError(string invalidText, string invalidCharacter)
            {
                return $"Tag helpers cannot target tag name '{invalidText}' because it contains a '{invalidCharacter}' character.";
            }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidNameData))]
    public void CreateDescriptor_CreatesErrorOnInvalidNames(string name, string[] expectedErrorMessages)
    {
        // Arrange
        name = name.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");
        var text = $$"""
            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("{{name}}")]
            public class DynamicTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }
            """;

        var syntaxTree = Parse(text);
        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var tagHelperType = compilation.GetTypeByMetadataName("DynamicTestTagHelper");
        Assert.NotNull(tagHelperType);

        var attribute = tagHelperType.GetAttributes().Single();
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);

        // Act
        var descriptor = factory.CreateDescriptor(tagHelperType);

        // Assert
        Assert.NotNull(descriptor);
        var rule = Assert.Single(descriptor.TagMatchingRules);
        var errorMessages = rule.GetAllDiagnostics().Select(diagnostic => diagnostic.GetMessage(CultureInfo.CurrentCulture)).ToArray();
        Assert.Equal(expectedErrorMessages.Length, errorMessages.Length);
        for (var i = 0; i < expectedErrorMessages.Length; i++)
        {
            Assert.Equal(expectedErrorMessages[i], errorMessages[i], StringComparer.Ordinal);
        }
    }

    // name, expectedNames
    public static TheoryData<string, ImmutableArray<string>> ValidNameData
        => new()
        {
            ("p", ["p"]),
            (" p", ["p"]),
            ("p ", ["p"]),
            (" p ", ["p"]),
            ("p,div", ["p", "div"]),
            (" p,div", ["p", "div"]),
            ("p ,div", ["p", "div"]),
            (" p ,div", ["p", "div"]),
            ("p, div", ["p", "div"]),
            ("p,div ", ["p", "div"]),
            ("p, div ", ["p", "div"]),
            (" p, div ", ["p", "div"]),
            (" p , div ", ["p", "div"]),
        };

    // type, expectedAttributeDescriptors
    public static TheoryData<string, ImmutableArray<BoundAttributeDescriptor>> InvalidTagHelperAttributeDescriptorData
        => new()
        {
            NameAndBoundAttributes("InvalidBoundAttribute", static b => b
                .BoundAttribute<string>(name: "data-something", propertyName: "DataSomething")),
            NameAndBoundAttributes("InvalidBoundAttributeWithValid", static b => b
                .BoundAttribute<string>(name: "data-something", propertyName: "DataSomething")
                .BoundAttribute<int>(name: "int-attribute", propertyName: "IntAttribute")),
            NameAndBoundAttributes("OverriddenInvalidBoundAttributeWithValid", static b => b
                .BoundAttribute<string>(name: "valid-something", propertyName: "DataSomething")),
            NameAndBoundAttributes("OverriddenValidBoundAttributeWithInvalid", static b => b
                .BoundAttribute<string>(name: "data-something", propertyName: "ValidSomething")),
            NameAndBoundAttributes("OverriddenValidBoundAttributeWithInvalidUpperCase", static b => b
                .BoundAttribute<string>(name: "DATA-SOMETHING", propertyName: "ValidSomething"))
        };

    [Theory]
    [MemberData(nameof(InvalidTagHelperAttributeDescriptorData))]
    public void CreateDescriptor_DoesNotAllowDataDashAttributes(
        string tagHelperTypeName,
        ImmutableArray<BoundAttributeDescriptor> expectedAttributes)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal<BoundAttributeDescriptor>(expectedAttributes, descriptor.BoundAttributes);

        var id = AspNetCore.Razor.Language.RazorDiagnosticFactory.TagHelper_InvalidBoundAttributeNameStartsWith.Id;
        foreach (var attribute in descriptor.BoundAttributes.Where(a => a.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase)))
        {
            var diagnostic = Assert.Single(attribute.Diagnostics);
            Assert.Equal(id, diagnostic.Id);
        }
    }

    public static TheoryData<string> ValidAttributeNameData
        =>
        [
            "data",
            "dataa-",
            "ValidName",
            "valid-name",
            "--valid--name--",
            ",,--__..oddly.valid::;;",
        ];

    [Theory]
    [MemberData(nameof(ValidAttributeNameData))]
    public void CreateDescriptor_WithValidAttributeName_HasNoErrors(string name)
    {
        // Arrange
        var text = $$"""
            public class DynamicTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute("{{name}}")]
                public string SomeAttribute { get; set; }
            }
            """;

        var syntaxTree = Parse(text);
        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var tagHelperType = compilation.GetTypeByMetadataName("DynamicTestTagHelper");
        Assert.NotNull(tagHelperType);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);

        // Act
        var descriptor = factory.CreateDescriptor(tagHelperType);

        // Assert
        Assert.NotNull(descriptor);
        Assert.False(descriptor.HasErrors);
    }

    public static TheoryData<string> ValidAttributePrefixData
        =>
        [
            string.Empty,
            "data",
            "dataa-",
            "ValidName",
            "valid-name",
            "--valid--name--",
            ",,--__..oddly.valid::;;",
        ];

    [Theory]
    [MemberData(nameof(ValidAttributePrefixData))]
    public void CreateDescriptor_WithValidAttributePrefix_HasNoErrors(string prefix)
    {
        // Arrange
        var text = $$"""
            public class DynamicTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute(DictionaryAttributePrefix = "{{prefix}}")]
                public System.Collections.Generic.IDictionary<string, int> SomeAttribute { get; set; }
            }
            """;

        var syntaxTree = Parse(text);
        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var tagHelperType = compilation.GetTypeByMetadataName("DynamicTestTagHelper");
        Assert.NotNull(tagHelperType);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);

        // Act
        var descriptor = factory.CreateDescriptor(tagHelperType);

        // Assert
        Assert.NotNull(descriptor);
        Assert.False(descriptor.HasErrors);
    }

    // name, expectedErrorMessages
    public static TheoryData<string, string[]> InvalidAttributeNameData
    {
        get
        {
            var whitespaceErrorString =
                "Invalid tag helper bound property 'string DynamicTestTagHelper.InvalidProperty' on tag helper 'DynamicTestTagHelper'. Tag helpers cannot " +
                "bind to HTML attributes with a null or empty name.";

            return GetInvalidNameOrPrefixData(onNameError, whitespaceErrorString, onDataError);

            static string onNameError(string invalidText, string invalidCharacter)
            {
                return "Invalid tag helper bound property 'string DynamicTestTagHelper.InvalidProperty' on tag helper 'DynamicTestTagHelper'. Tag helpers " +
                $"cannot bind to HTML attributes with name '{invalidText}' because the name contains a '{invalidCharacter}' character.";
            }

            static string onDataError(string invalidText)
            {
                return "Invalid tag helper bound property 'string DynamicTestTagHelper.InvalidProperty' on tag helper 'DynamicTestTagHelper'. Tag helpers cannot bind " +
                    $"to HTML attributes with name '{invalidText}' because the name starts with 'data-'.";
            }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidAttributeNameData))]
    public void CreateDescriptor_WithInvalidAttributeName_HasErrors(string name, string[] expectedErrorMessages)
    {
        // Arrange
        name = name.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");
        var text = $$"""
            public class DynamicTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute("{{name}}")]
                public string InvalidProperty { get; set; }
            }
            """;

        var syntaxTree = Parse(text);
        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var tagHelperType = compilation.GetTypeByMetadataName("DynamicTestTagHelper");
        Assert.NotNull(tagHelperType);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);

        // Act
        var descriptor = factory.CreateDescriptor(tagHelperType);
        Assert.NotNull(descriptor);

        // Assert
        var errorMessages = descriptor.GetAllDiagnostics().Select(diagnostic => diagnostic.GetMessage(CultureInfo.CurrentCulture));
        Assert.Equal(expectedErrorMessages, errorMessages);
    }

    // prefix, expectedErrorMessages
    public static TheoryData<string, string[]> InvalidAttributePrefixData
    {
        get
        {
            var whitespaceErrorString =
                "Invalid tag helper bound property 'System.Collections.Generic.IDictionary<System.String, System.Int32> DynamicTestTagHelper.InvalidProperty' " +
                "on tag helper 'DynamicTestTagHelper'. Tag helpers cannot bind to HTML attributes with a null or empty name.";

            return GetInvalidNameOrPrefixData(onPrefixError, whitespaceErrorString, onDataError);

            static string onPrefixError(string invalidText, string invalidCharacter)
            {
                return "Invalid tag helper bound property 'System.Collections.Generic.IDictionary<System.String, System.Int32> DynamicTestTagHelper.InvalidProperty' " +
                    "on tag helper 'DynamicTestTagHelper'. Tag helpers " +
                    $"cannot bind to HTML attributes with prefix '{invalidText}' because the prefix contains a '{invalidCharacter}' character.";
            }

            static string onDataError(string invalidText)
            {
                return "Invalid tag helper bound property 'System.Collections.Generic.IDictionary<System.String, System.Int32> DynamicTestTagHelper.InvalidProperty' " +
                    "on tag helper 'DynamicTestTagHelper'. Tag helpers cannot bind to HTML attributes " +
                    $"with prefix '{invalidText}' because the prefix starts with 'data-'.";
            }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidAttributePrefixData))]
    public void CreateDescriptor_WithInvalidAttributePrefix_HasErrors(string prefix, string[] expectedErrorMessages)
    {
        // Arrange
        prefix = prefix.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");
        var text = $$"""
            public class DynamicTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute(DictionaryAttributePrefix = "{{prefix}}")]
                public System.Collections.Generic.IDictionary<System.String, System.Int32> InvalidProperty { get; set; }
            }
            """;

        var syntaxTree = Parse(text);
        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var tagHelperType = compilation.GetTypeByMetadataName("DynamicTestTagHelper");
        Assert.NotNull(tagHelperType);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);

        // Act
        var descriptor = factory.CreateDescriptor(tagHelperType);

        // Assert
        Assert.NotNull(descriptor);
        var errorMessages = descriptor.GetAllDiagnostics().Select(diagnostic => diagnostic.GetMessage(CultureInfo.CurrentCulture));
        Assert.Equal(expectedErrorMessages, errorMessages);
    }

    public static TheoryData<string, string[]> InvalidRestrictChildrenNameData
    {
        get
        {
            var nullOrWhiteSpaceError = Resources.FormatTagHelper_InvalidRestrictedChildNullOrWhitespace("DynamicTestTagHelper");

            return GetInvalidNameOrPrefixData(
                onNameError: static (invalidInput, invalidCharacter) =>
                    Resources.FormatTagHelper_InvalidRestrictedChild("DynamicTestTagHelper", invalidInput, invalidCharacter),
                whitespaceErrorString: nullOrWhiteSpaceError,
                onDataError: null);
        }
    }

    [Theory]
    [MemberData(nameof(InvalidRestrictChildrenNameData))]
    public void CreateDescriptor_WithInvalidAllowedChildren_HasErrors(string name, string[] expectedErrorMessages)
    {
        // Arrange
        name = name.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");
        var text = $$"""
            [Microsoft.AspNetCore.Razor.TagHelpers.RestrictChildrenAttribute("{{name}}")]
            public class DynamicTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }
            """;

        var syntaxTree = Parse(text);
        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var tagHelperType = compilation.GetTypeByMetadataName("DynamicTestTagHelper");
        Assert.NotNull(tagHelperType);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);

        // Act
        var descriptor = factory.CreateDescriptor(tagHelperType);

        // Assert
        Assert.NotNull(descriptor);
        var errorMessages = descriptor.GetAllDiagnostics().Select(diagnostic => diagnostic.GetMessage(CultureInfo.CurrentCulture));
        Assert.Equal(expectedErrorMessages, errorMessages);
    }

    public static TheoryData<string, string[]> InvalidParentTagData
    {
        get
        {
            var nullOrWhiteSpaceError = Resources.TagHelper_InvalidTargetedParentTagNameNullOrWhitespace;

            return GetInvalidNameOrPrefixData(
                onNameError: static (invalidInput, invalidCharacter) =>
                    Resources.FormatTagHelper_InvalidTargetedParentTagName(invalidInput, invalidCharacter),
                whitespaceErrorString: nullOrWhiteSpaceError,
                onDataError: null);
        }
    }

    [Theory]
    [MemberData(nameof(InvalidParentTagData))]
    public void CreateDescriptor_WithInvalidParentTag_HasErrors(string name, string[] expectedErrorMessages)
    {
        // Arrange
        name = name.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");
        var text = $$"""
            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(ParentTag = "{{name}}")]
            public class DynamicTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }
            """;

        var syntaxTree = Parse(text);
        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var tagHelperType = compilation.GetTypeByMetadataName("DynamicTestTagHelper");
        Assert.NotNull(tagHelperType);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);

        // Act
        var descriptor = factory.CreateDescriptor(tagHelperType);

        // Assert
        Assert.NotNull(descriptor);
        var errorMessages = descriptor.GetAllDiagnostics().Select(diagnostic => diagnostic.GetMessage(CultureInfo.CurrentCulture));
        Assert.Equal(expectedErrorMessages, errorMessages);
    }

    [Fact]
    public void CreateDescriptor_BuildsDescriptorsFromSimpleTypes()
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(typeof(Enumerable).FullName!);

        Assert.NotNull(typeSymbol);

        var expectedDescriptor = CreateTagHelper(
            @namespace: typeSymbol.ContainingNamespace.GetDefaultDisplayString(),
            typeName: typeSymbol.Name,
            assemblyName: typeSymbol.ContainingAssembly.Identity.Name, static b => b
                .TagMatchingRule("enumerable"));

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    // tagHelperType, expectedAttributeDescriptors, expectedDiagnostics
    public static TheoryData<string, ImmutableArray<BoundAttributeDescriptor>, ImmutableArray<RazorDiagnostic>> TagHelperWithPrefixData
    {
        get
        {
            return new()
            {
                Combine(
                    NameAndBoundAttributes("DefaultValidHtmlAttributePrefix", static b => b
                        .BoundAttribute(name: "dictionary-property", propertyName: "DictionaryProperty", typeName: GetDictionaryTypeName<IDictionary<string, string>>(), static b => b
                            .AsDictionaryAttribute<string>("dictionary-property-"))),
                    diagnostics: []),
                Combine(
                    NameAndBoundAttributes("SingleValidHtmlAttributePrefix", static b => b
                        .BoundAttribute(name: "valid-name", propertyName: "DictionaryProperty", typeName: GetDictionaryTypeName<IDictionary<string, string>>(), static b => b
                            .AsDictionaryAttribute<string>("valid-name-"))),
                    diagnostics: []),
                Combine(
                    NameAndBoundAttributes("MultipleValidHtmlAttributePrefix", static b => b
                        .BoundAttribute(name: "valid-name1", propertyName: "DictionaryProperty", typeName:GetDictionaryTypeName<Dictionary<string, object>>(), static b => b
                            .AsDictionaryAttribute<object>("valid-prefix1-"))
                        .BoundAttribute(name: "valid-name2", propertyName: "DictionarySubclassProperty", typeName: "TestNamespace.DictionarySubclass", static b => b
                            .AsDictionaryAttribute<string>("valid-prefix2-"))
                        .BoundAttribute(name: "valid-name3", propertyName: "DictionaryWithoutParameterlessConstructorProperty", typeName: "TestNamespace.DictionaryWithoutParameterlessConstructor", static b => b
                            .AsDictionaryAttribute<string>("valid-prefix3-"))
                        .BoundAttribute(name: "valid-name4", propertyName: "GenericDictionarySubclassProperty", typeName: "TestNamespace.GenericDictionarySubclass<System.Object>", static b => b
                            .AsDictionaryAttribute<object>("valid-prefix4-"))
                        .BoundAttribute(name: "valid-name5", propertyName: "SortedDictionaryProperty", typeName: GetDictionaryTypeName<SortedDictionary<string, int>>(), static b => b
                            .AsDictionaryAttribute<int>("valid-prefix5-"))
                        .BoundAttribute(name: "valid-name6", propertyName: "StringProperty", typeName: typeof(string).FullName!)
                        .BoundAttribute(name: string.Empty, propertyName: "GetOnlyDictionaryProperty", typeName: GetDictionaryTypeName<IDictionary<string, int>>(), static b => b
                            .AsDictionaryAttribute<int>("get-only-dictionary-property-"))
                        .BoundAttribute(name: string.Empty, propertyName: "GetOnlyDictionaryPropertyWithAttributePrefix", typeName: GetDictionaryTypeName<IDictionary<string, string>>(), static b => b
                            .AsDictionaryAttribute<string>("valid-prefix6"))),
                    diagnostics: []),
                Combine(
                    NameAndBoundAttributes("SingleInvalidHtmlAttributePrefix", static b => b
                        .BoundAttribute(name: "valid-name", propertyName: "StringProperty", typeName: typeof(string).FullName!, static b => b
                            .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                                "TestNamespace.SingleInvalidHtmlAttributePrefix",
                                "StringProperty")))),
                    diagnostics: RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                        "TestNamespace.SingleInvalidHtmlAttributePrefix",
                        "StringProperty")),
                Combine(
                    NameAndBoundAttributes("MultipleInvalidHtmlAttributePrefix", static b => b
                        .BoundAttribute<long>(name: "valid-name1", propertyName: "LongProperty")
                        .BoundAttribute(name: "valid-name2", propertyName: "DictionaryOfIntProperty", typeName: GetDictionaryTypeName<Dictionary<int, string>>(), static b => b
                            .AsDictionaryAttribute<string>("valid-prefix2-")
                            .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                                "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                                "DictionaryOfIntProperty")))
                        .BoundAttribute(name: "valid-name3", propertyName: "ReadOnlyDictionaryProperty", typeName: GetDictionaryTypeName<IReadOnlyDictionary<string, object>>(), static b => b
                            .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                                "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                                "ReadOnlyDictionaryProperty")))
                        .BoundAttribute<int>(name: "valid-name4", propertyName: "IntProperty", static b => b
                            .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                                "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                                "IntProperty")))
                        .BoundAttribute(name: "valid-name5", propertyName: "DictionaryOfIntSubclassProperty", typeName: "TestNamespace.DictionaryOfIntSubclass", static b => b
                            .AsDictionaryAttribute<string>("valid-prefix5-")
                            .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                                "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                                "DictionaryOfIntSubclassProperty")))
                        .BoundAttribute(name: string.Empty, propertyName: "GetOnlyDictionaryAttributePrefix", typeName: GetDictionaryTypeName<IDictionary<int, string>>(), static b => b
                            .AsDictionaryAttribute<string>("valid-prefix6")
                            .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                                "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                                "GetOnlyDictionaryAttributePrefix")))
                        .BoundAttribute(name: string.Empty, propertyName: "GetOnlyDictionaryPropertyWithAttributeName", typeName: GetDictionaryTypeName<IDictionary<string, object>>(), static b => b
                            .AsDictionaryAttribute<object>("invalid-name7-")
                            .AddDiagnostic(RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNull(
                                "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                                "GetOnlyDictionaryPropertyWithAttributeName")))),
                    diagnostics: [
                        RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                            "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                            "DictionaryOfIntProperty"),
                        RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                            "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                            "ReadOnlyDictionaryProperty"),
                        RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                            "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                            "IntProperty"),
                        RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                            "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                            "DictionaryOfIntSubclassProperty"),
                        RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(
                            "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                            "GetOnlyDictionaryAttributePrefix"),
                        RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNull(
                            "TestNamespace.MultipleInvalidHtmlAttributePrefix",
                            "GetOnlyDictionaryPropertyWithAttributeName")])
            };

            static (string, ImmutableArray<BoundAttributeDescriptor>, ImmutableArray<RazorDiagnostic>) Combine(
                (string name, ImmutableArray<BoundAttributeDescriptor> boundAttributes) pair, params ImmutableArray<RazorDiagnostic> diagnostics)
            {
                return (pair.name, pair.boundAttributes, diagnostics);
            }

            static string GetDictionaryTypeName<T>()
            {
                var dictionaryType = typeof(T);
                Assert.True(dictionaryType.IsConstructedGenericType);
                Assert.Equal(2, dictionaryType.GenericTypeArguments.Length);
                Assert.Equal("`2", dictionaryType.Name[^2..]);

                var arg1Type = dictionaryType.GenericTypeArguments[0];
                var arg2Type = dictionaryType.GenericTypeArguments[1];

                return $"{dictionaryType.Namespace}.{dictionaryType.Name[..^2]}<{arg1Type.FullName}, {arg2Type.FullName}>";
            }
        }
    }

    [Theory]
    [MemberData(nameof(TagHelperWithPrefixData))]
    public void CreateDescriptor_WithPrefixes_ReturnsExpectedAttributeDescriptors(
        string tagHelperTypeName,
        ImmutableArray<BoundAttributeDescriptor> expectedAttributes,
        ImmutableArray<RazorDiagnostic> expectedDiagnostics)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal<BoundAttributeDescriptor>(expectedAttributes, descriptor.BoundAttributes);
        Assert.Equal<RazorDiagnostic>(expectedDiagnostics, [.. descriptor.GetAllDiagnostics()]);
    }

    // tagHelperType, expectedDescriptor
    public static TheoryData<string, TagHelperDescriptor?> TagOutputHintData
        => new()
        {
            NameAndTagHelper("MultipleDescriptorTagHelperWithOutputElementHint", static b => b
                .TagMatchingRule(tagName: "a")
                .TagMatchingRule(tagName: "p")
                .TagOutputHint("div")),
            NameAndTagHelper("TestNamespace2", "InheritedOutputElementHintTagHelper", static b => b
                .TagMatchingRule(tagName: "inherited-output-element-hint")),
            NameAndTagHelper("TestNamespace2", "OutputElementHintTagHelper", static b => b
                .TagMatchingRule(tagName: "output-element-hint")
                .TagOutputHint("hinted-value")),
            NameAndTagHelper("TestNamespace2", "OverriddenOutputElementHintTagHelper", static b => b
                .TagMatchingRule(tagName: "overridden-output-element-hint")
                .TagOutputHint("overridden"))
        };

    [Theory]
    [MemberData(nameof(TagOutputHintData))]
    public void CreateDescriptor_CreatesDescriptorsWithOutputElementHint(string tagHelperTypeName, TagHelperDescriptor? expectedDescriptor)
    {
        // Arrange
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: false, excludeHidden: false);
        var typeSymbol = Compilation.GetTypeByMetadataName(tagHelperTypeName);
        Assert.NotNull(typeSymbol);

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.Equal(expectedDescriptor, descriptor);
    }

    [Fact]
    public void CreateDescriptor_CapturesDocumentationOnTagHelperClass()
    {
        // Arrange
        var syntaxTree = Parse("""
            using Microsoft.AspNetCore.Razor.TagHelpers;

            /// <summary>
            /// The summary for <see cref="DocumentedTagHelper"/>.
            /// </summary>
            /// <remarks>
            /// Inherits from <see cref="TagHelper"/>.
            /// </remarks>
            public class DocumentedTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }
            """);

        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: true, excludeHidden: false);
        var typeSymbol = compilation.GetTypeByMetadataName("DocumentedTagHelper");

        Assert.NotNull(typeSymbol);

        var expectedDocumentation = """
            <member name="T:DocumentedTagHelper">
                <summary>
                The summary for <see cref="T:DocumentedTagHelper"/>.
                </summary>
                <remarks>
                Inherits from <see cref="T:Microsoft.AspNetCore.Razor.TagHelpers.TagHelper"/>.
                </remarks>
            </member>
            """;

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor.Documentation);
        Assert.Equal(expectedDocumentation, descriptor.Documentation.Trim());
    }

    [Fact]
    public void CreateDescriptor_CapturesDocumentationOnTagHelperProperties()
    {
        // Arrange
        var syntaxTree = Parse("""
            using System.Collections.Generic;

            public class DocumentedTagHelper : Microsoft.spNetCore.Razor.TagHelpers.TagHelper
            {
                /// <summary>
                /// This <see cref="SummaryProperty"/> is of type <see cref="string"/>.
                /// </summary>
                public string SummaryProperty { get; set; }

                /// <remarks>
                /// The <see cref="SummaryProperty"/> may be <c>null</c>.
                /// </remarks>
                public int RemarksProperty { get; set; }

                /// <summary>
                /// This is a complex <see cref="List{bool}"/>.
                /// </summary>
                /// <remarks>
                /// <see cref="SummaryProperty"/><see cref="RemarksProperty"/>
                /// </remarks>
                public List<bool> RemarksAndSummaryProperty { get; set; }
            }
            """);

        var compilation = Compilation.AddSyntaxTrees(syntaxTree);
        var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation: true, excludeHidden: false);
        var typeSymbol = compilation.GetTypeByMetadataName("DocumentedTagHelper");

        Assert.NotNull(typeSymbol);

        var expectedDocumentations = new[]
        {
            """
            <member name="P:DocumentedTagHelper.SummaryProperty">
                <summary>
                This <see cref="P:DocumentedTagHelper.SummaryProperty"/> is of type <see cref="T:System.String"/>.
                </summary>
            </member>
            """,
            """
            <member name="P:DocumentedTagHelper.RemarksProperty">
                <remarks>
                The <see cref="P:DocumentedTagHelper.SummaryProperty"/> may be <c>null</c>.
                </remarks>
            </member>
            """,
            """
            <member name="P:DocumentedTagHelper.RemarksAndSummaryProperty">
                <summary>
                This is a complex <see cref="T:System.Collections.Generic.List`1"/>.
                </summary>
                <remarks>
                <see cref="P:DocumentedTagHelper.SummaryProperty"/><see cref="P:DocumentedTagHelper.RemarksProperty"/>
                </remarks>
            </member>
            """,
        };

        // Act
        var descriptor = factory.CreateDescriptor(typeSymbol);

        // Assert
        Assert.NotNull(descriptor);
        var actuaDocumentations = descriptor.BoundAttributes
            .SelectAsArray(boundAttribute =>
            {
                Assert.NotNull(boundAttribute.Documentation);
                return boundAttribute.Documentation.Trim();
            });

        Assert.Equal(expectedDocumentations, actuaDocumentations);
    }

    private static TheoryData<string, string[]> GetInvalidNameOrPrefixData(
        Func<string, string, string> onNameError,
        string whitespaceErrorString,
        Func<string, string>? onDataError)
    {
        // name, expectedErrorMessages
        var data = new TheoryData<string, string[]>
        {
            ("!", [onNameError("!", "!")]),
            ("hello!", [onNameError("hello!", "!")]),
            ("!hello", [onNameError("!hello", "!")]),
            ("he!lo", [onNameError("he!lo", "!")]),
            ("!he!lo!", [onNameError("!he!lo!", "!")]),
            ("@", [onNameError("@", "@")]),
            ("hello@", [onNameError("hello@", "@")]),
            ("@hello", [onNameError("@hello", "@")]),
            ("he@lo", [onNameError("he@lo", "@")]),
            ("@he@lo@", [onNameError("@he@lo@", "@")]),
            ("/", [onNameError("/", "/")]),
            ("hello/", [onNameError("hello/", "/")]),
            ("/hello", [onNameError("/hello", "/")]),
            ("he/lo", [onNameError("he/lo", "/")]),
            ("/he/lo/", [onNameError("/he/lo/", "/")]),
            ("<", [onNameError("<", "<")]),
            ("hello<", [onNameError("hello<", "<")]),
            ("<hello", [onNameError("<hello", "<")]),
            ("he<lo", [onNameError("he<lo", "<")]),
            ("<he<lo<", [onNameError("<he<lo<", "<")]),
            ("?", [onNameError("?", "?")]),
            ("hello?", [onNameError("hello?", "?")]),
            ("?hello", [onNameError("?hello", "?")]),
            ("he?lo", [onNameError("he?lo", "?")]),
            ("?he?lo?", [onNameError("?he?lo?", "?")]),
            ("[", [onNameError("[", "[")]),
            ("hello[", [onNameError("hello[", "[")]),
            ("[hello", [onNameError("[hello", "[")]),
            ("he[lo", [onNameError("he[lo", "[")]),
            ("[he[lo[", [onNameError("[he[lo[", "[")]),
            (">", [onNameError(">", ">")]),
            ("hello>", [onNameError("hello>", ">")]),
            (">hello", [onNameError(">hello", ">")]),
            ("he>lo", [onNameError("he>lo", ">")]),
            (">he>lo>", [onNameError(">he>lo>", ">")]),
            ("]", [onNameError("]", "]")]),
            ("hello]", [onNameError("hello]", "]")]),
            ("]hello", [onNameError("]hello", "]")]),
            ("he]lo", [onNameError("he]lo", "]")]),
            ("]he]lo]", [onNameError("]he]lo]", "]")]),
            ("=", [onNameError("=", "=")]),
            ("hello=", [onNameError("hello=", "=")]),
            ("=hello", [onNameError("=hello", "=")]),
            ("he=lo", [onNameError("he=lo", "=")]),
            ("=he=lo=", [onNameError("=he=lo=", "=")]),
            ("\"", [onNameError("\"", "\"")] ),
            ("hello\"", [onNameError("hello\"", "\"")] ),
            ("\"hello", [onNameError("\"hello", "\"")] ),
            ("he\"lo", [onNameError("he\"lo", "\"")] ),
            ("\"he\"lo\"", [onNameError("\"he\"lo\"", "\"")] ),
            ("'", [onNameError("'", "'")] ),
            ("hello'", [onNameError("hello'", "'")] ),
            ("'hello", [onNameError("'hello", "'")] ),
            ("he'lo", [onNameError("he'lo", "'")] ),
            ("'he'lo'", [onNameError("'he'lo'", "'")] ),
            ("hello*", [onNameError("hello*", "*")] ),
            ("*hello", [onNameError("*hello", "*")] ),
            ("he*lo", [onNameError("he*lo", "*")] ),
            ("*he*lo*", [onNameError("*he*lo*", "*")] ),
            (Environment.NewLine, [whitespaceErrorString]),
            ("\t", [whitespaceErrorString]),
            (" \t ", [whitespaceErrorString]),
            (" ", [whitespaceErrorString]),
            (Environment.NewLine + " ", [whitespaceErrorString]),
            ("! \t\r\n@/<>?[]=\"'*", [
                onNameError("! \t\r\n@/<>?[]=\"'*", "!"),
                onNameError("! \t\r\n@/<>?[]=\"'*", " "),
                onNameError("! \t\r\n@/<>?[]=\"'*", "\t"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "\r"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "\n"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "@"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "/"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "<"),
                onNameError("! \t\r\n@/<>?[]=\"'*", ">"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "?"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "["),
                onNameError("! \t\r\n@/<>?[]=\"'*", "]"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "="),
                onNameError("! \t\r\n@/<>?[]=\"'*", "\""),
                onNameError("! \t\r\n@/<>?[]=\"'*", "'"),
                onNameError("! \t\r\n@/<>?[]=\"'*", "*")]),
            ("! \tv\ra\nl@i/d<>?[]=\"'*", [
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "!"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", " "),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\t"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\r"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\n"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "@"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "/"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "<"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", ">"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "?"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "["),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "]"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "="),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\""),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "'"),
                onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "*")])
        };

        if (onDataError is not null)
        {
            data.Add("data-", [onDataError("data-")]);
            data.Add("data-something", [onDataError("data-something")]);
            data.Add("Data-Something", [onDataError("Data-Something")]);
            data.Add("DATA-SOMETHING", [onDataError("DATA-SOMETHING")]);
        }

        return data;
    }

    private static (string, bool, TagHelperDescriptor?) NameAndTagHelper(
        string typeName, bool designTime, Action<TagHelperDescriptorBuilder>? configure)
        => NameAndTagHelper(@namespace: "TestNamespace", typeName, designTime, configure);

    private static (string, bool, TagHelperDescriptor?) NameAndTagHelper(
        string @namespace, string typeName, bool designTime, Action<TagHelperDescriptorBuilder>? configure)
    {
        var (fullName, tagHelper) = NameAndTagHelper(@namespace, typeName, configure);

        return (fullName, designTime, tagHelper);
    }

    private static (string, ImmutableArray<BoundAttributeDescriptor>) NameAndBoundAttributes(
        string typeName, Action<TagHelperDescriptorBuilder>? configure)
    {
        var (fullName, tagHelper) = NameAndTagHelper(@namespace: "TestNamespace", typeName, configure);

        return (fullName, tagHelper?.BoundAttributes ?? []);
    }

    private static (string, TagHelperDescriptor?) NameAndTagHelper(
        string typeName, Action<TagHelperDescriptorBuilder>? configure)
        => NameAndTagHelper(@namespace: "TestNamespace", typeName, configure);

    private static (string, TagHelperDescriptor?) NameAndTagHelper(
        string @namespace, string typeName, Action<TagHelperDescriptorBuilder>? configure)
    {
        var tagHelper = configure is not null
            ? CreateTagHelper(@namespace, typeName, configure)
            : null;

        return ($"{@namespace}.{typeName}", tagHelper);
    }

    private static TagHelperDescriptor CreateTagHelper(
        string typeName,
        Action<TagHelperDescriptorBuilder> configure)
        => CreateTagHelper(@namespace: "TestNamespace", typeName, configure);

    private static TagHelperDescriptor CreateTagHelper(
        string @namespace,
        string typeName,
        Action<TagHelperDescriptorBuilder> configure)
        => CreateTagHelper(@namespace, typeName, AssemblyName, configure);

    private static TagHelperDescriptor CreateTagHelper(
        string @namespace,
        string typeName,
        string assemblyName,
        Action<TagHelperDescriptorBuilder>? configure = null)
    {
        var fullName = $"{@namespace}.{typeName}";

        var builder = TagHelperDescriptorBuilder.CreateTagHelper(fullName, assemblyName);
        builder.SetTypeName(fullName, @namespace, typeName);

        configure?.Invoke(builder);

        return builder.Build();
    }

    private const string AdditionalCode =
        """
        namespace TestNamespace2
        {
            [Microsoft.AspNetCore.Razor.TagHelpers.OutputElementHint("hinted-value")]
            public class OutputElementHintTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }

            public class InheritedOutputElementHintTagHelper : OutputElementHintTagHelper
            {
            }

            [Microsoft.AspNetCore.Razor.TagHelpers.OutputElementHint("overridden")]
            public class OverriddenOutputElementHintTagHelper : OutputElementHintTagHelper
            {
            }
        }
        """;
}
