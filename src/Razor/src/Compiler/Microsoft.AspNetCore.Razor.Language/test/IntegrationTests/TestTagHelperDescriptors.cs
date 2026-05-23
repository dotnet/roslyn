// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public static class TestTagHelperDescriptors
{
    public static TagHelperCollection SimpleTagHelperDescriptors
    {
        get
        {
            return
                [
                CreateTagHelperDescriptor(
                    tagName: "span",
                    typeName: "SpanTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "div",
                    typeName: "DivTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                        builder => builder
                            .Name("bound")
                            .PropertyName("BoundProp")
                            .TypeName("System.String"),
                        builder => builder
                            .Name("age")
                            .PropertyName("AgeProp")
                            .TypeName("System.Int32"),
                        builder => builder
                            .Name("alive")
                            .PropertyName("AliveProp")
                            .TypeName("System.Boolean"),
                        builder => builder
                            .Name("tag")
                            .PropertyName("TagProp")
                            .TypeName("System.Object"),
                        builder => builder
                            .Name("tuple-dictionary")
                            .PropertyName("DictionaryOfBoolAndStringTupleProperty")
                            .TypeName(typeof(IDictionary<string, int>).Namespace + ".IDictionary<System.String, (System.Boolean, System.String)>")
                            .AsDictionaryAttribute("tuple-prefix-", typeof((bool, string)).FullName)
                    ])
            ];
        }
    }

    public static TagHelperCollection MinimizedBooleanTagHelperDescriptors
    {
        get
        {
            return
            [
                CreateTagHelperDescriptor(
                    tagName: "span",
                    typeName: "SpanTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "div",
                    typeName: "DivTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                        builder => builder
                            .Name("bound")
                            .PropertyName("BoundProp")
                            .TypeName("System.Boolean"),
                        builder => builder
                            .Name("age")
                            .PropertyName("AgeProp")
                            .TypeName("System.Int32"),
                    ])
            ];
        }
    }

    public static TagHelperCollection CssSelectorTagHelperDescriptors
    {
        get
        {
            var inputTypePropertyInfo = GetTestTypeRuntimeProperty("Type");
            var inputCheckedPropertyInfo = GetTestTypeRuntimeProperty("Checked");

            return
            [
                CreateTagHelperDescriptor(
                    tagName: "a",
                    typeName: "TestNamespace.ATagHelper",
                    assemblyName: "TestAssembly",
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute
                                .Name("href", RequiredAttributeNameComparison.FullMatch)
                                .Value("~/", RequiredAttributeValueComparison.FullMatch)),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "a",
                    typeName: "TestNamespace.ATagHelperMultipleSelectors",
                    assemblyName: "TestAssembly",
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute
                                .Name("href", RequiredAttributeNameComparison.FullMatch)
                                .Value("~/", RequiredAttributeValueComparison.PrefixMatch))
                            .RequireAttributeDescriptor(attribute => attribute
                                .Name("href", RequiredAttributeNameComparison.FullMatch)
                                .Value("?hello=world", RequiredAttributeValueComparison.SuffixMatch)),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute
                                .Name("type", RequiredAttributeNameComparison.FullMatch)
                                .Value("text", RequiredAttributeValueComparison.FullMatch)),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper2",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute
                                .Name("ty", RequiredAttributeNameComparison.PrefixMatch)),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "*",
                    typeName: "TestNamespace.CatchAllTagHelper",
                    assemblyName: "TestAssembly",
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute
                                .Name("href", RequiredAttributeNameComparison.FullMatch)
                                .Value("~/", RequiredAttributeValueComparison.PrefixMatch)),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "*",
                    typeName: "TestNamespace.CatchAllTagHelper2",
                    assemblyName: "TestAssembly",
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute
                                .Name("type", RequiredAttributeNameComparison.FullMatch)),
                    ]),
            ];
        }
    }

    public static TagHelperCollection EnumTagHelperDescriptors
    {
        get
        {
            return
            [
                CreateTagHelperDescriptor(
                    tagName: "*",
                    typeName: "TestNamespace.CatchAllTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("catch-all")
                            .PropertyName("CatchAll")
                            .AsEnum()
                            .TypeName("Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestTagHelperDescriptors.MyEnum"),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("value")
                            .PropertyName("Value")
                            .AsEnum()
                            .TypeName("Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestTagHelperDescriptors.MyEnum"),
                    ]),
            ];
        }
    }

    public static TagHelperCollection SymbolBoundTagHelperDescriptors
    {
        get
        {
            return
            [
                CreateTagHelperDescriptor(
                    tagName: "*",
                    typeName: "TestNamespace.CatchAllTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("[item]")
                            .PropertyName("ListItems")
                            .TypeName("System.Collections.Generic.List<string>"),
                        builder => builder
                            .Name("[(item)]")
                            .PropertyName("ArrayItems")
                            .TypeName(typeof(string[]).FullName),
                        builder => builder
                            .Name("(click)")
                            .PropertyName("Event1")
                            .TypeName(typeof(Action).FullName),
                        builder => builder
                            .Name("(^click)")
                            .PropertyName("Event2")
                            .TypeName(typeof(Action).FullName),
                        builder => builder
                            .Name("*something")
                            .PropertyName("StringProperty1")
                            .TypeName(typeof(string).FullName),
                        builder => builder
                            .Name("#local")
                            .PropertyName("StringProperty2")
                            .TypeName(typeof(string).FullName),
                    ],
                    ruleBuilders:
                    [
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("bound")),
                    ]),
            ];
        }
    }

    public static TagHelperCollection MinimizedTagHelpers_Descriptors
    {
        get
        {
            return
            [
                CreateTagHelperDescriptor(
                    tagName: "*",
                    typeName: "TestNamespace.CatchAllTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("catchall-bound-string")
                            .PropertyName("BoundRequiredString")
                            .TypeName(typeof(string).FullName),
                    ],
                    ruleBuilders:
                    [
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("catchall-unbound-required")),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("input-bound-required-string")
                            .PropertyName("BoundRequiredString")
                            .TypeName(typeof(string).FullName),
                        builder => builder
                            .Name("input-bound-string")
                            .PropertyName("BoundString")
                            .TypeName(typeof(string).FullName),
                    ],
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute.Name("input-bound-required-string"))
                            .RequireAttributeDescriptor(attribute => attribute.Name("input-unbound-required")),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "div",
                    typeName: "DivTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("boundbool")
                            .PropertyName("BoundBoolProp")
                            .TypeName(typeof(bool).FullName),
                        builder => builder
                            .Name("booldict")
                            .PropertyName("BoolDictProp")
                            .TypeName("System.Collections.Generic.IDictionary<string, bool>")
                            .AsDictionaryAttribute("booldict-prefix-", typeof(bool).FullName),
                    ]),
            ];
        }
    }

    public static TagHelperCollection DynamicAttributeTagHelpers_Descriptors
    {
        get
        {
            return
            [
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("bound")
                            .PropertyName("Bound")
                            .TypeName(typeof(string).FullName)
                    ]),
                ];
        }
    }

    public static TagHelperCollection DuplicateTargetTagHelperDescriptors
    {
        get
        {
            var typePropertyInfo = GetTestTypeRuntimeProperty("Type");
            var checkedPropertyInfo = GetTestTypeRuntimeProperty("Checked");

            return
            [
                CreateTagHelperDescriptor(
                    tagName: "*",
                    typeName: "TestNamespace.CatchAllTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", typePropertyInfo),
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "checked", checkedPropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("type")),
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("checked"))
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", typePropertyInfo),
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "checked", checkedPropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("type")),
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("checked"))
                    ])
            ];
        }
    }

    public static TagHelperCollection AttributeTargetingTagHelperDescriptors
    {
        get
        {
            var inputTypePropertyInfo = GetTestTypeRuntimeProperty("Type");
            var inputCheckedPropertyInfo = GetTestTypeRuntimeProperty("Checked");

            return
            [
                CreateTagHelperDescriptor(
                    tagName: "p",
                    typeName: "TestNamespace.PTagHelper",
                    assemblyName: "TestAssembly",
                    ruleBuilders:
                    [
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("class")),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("type")),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper2",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "checked", inputCheckedPropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder
                            .RequireAttributeDescriptor(attribute => attribute.Name("type"))
                            .RequireAttributeDescriptor(attribute => attribute.Name("checked")),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "*",
                    typeName: "TestNamespace.CatchAllTagHelper",
                    assemblyName: "TestAssembly",
                    ruleBuilders:
                    [
                        builder => builder.RequireAttributeDescriptor(attribute => attribute.Name("catchAll")),
                    ]),
            ];
        }
    }

    public static TagHelperCollection PrefixedAttributeTagHelperDescriptors
    {
        get
        {
            return
            [
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper1",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("int-prefix-grabber")
                            .PropertyName("IntProperty")
                            .TypeName(typeof(int).FullName),
                        builder => builder
                            .Name("int-dictionary")
                            .PropertyName("IntDictionaryProperty")
                            .TypeName("System.Collections.Generic.IDictionary<string, int>")
                            .AsDictionaryAttribute("int-prefix-", typeof(int).FullName),
                        builder => builder
                            .Name("string-prefix-grabber")
                            .PropertyName("StringProperty")
                            .TypeName(typeof(string).FullName),
                        builder => builder
                            .Name("string-dictionary")
                            .PropertyName("StringDictionaryProperty")
                            .TypeName("Namespace.DictionaryWithoutParameterlessConstructor<string, string>")
                            .AsDictionaryAttribute("string-prefix-", typeof(string).FullName),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper2",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => builder
                            .Name("int-dictionary")
                            .PropertyName("IntDictionaryProperty")
                            .TypeName(typeof(int).FullName)
                            .AsDictionaryAttribute("int-prefix-", typeof(int).FullName),
                        builder => builder
                            .Name("string-dictionary")
                            .PropertyName("StringDictionaryProperty")
                            .TypeName("Namespace.DictionaryWithoutParameterlessConstructor<string, string>")
                            .AsDictionaryAttribute("string-prefix-", typeof(string).FullName),
                    ]),
            ];
        }
    }

    public static TagHelperCollection TagHelpersInSectionDescriptors
    {
        get
        {
            var propertyInfo = GetTestTypeRuntimeProperty("BoundProperty");

            return
            [
                CreateTagHelperDescriptor(
                    tagName: "MyTagHelper",
                    typeName: "TestNamespace.MyTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "BoundProperty", propertyInfo),
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "NestedTagHelper",
                    typeName: "TestNamespace.NestedTagHelper",
                    assemblyName: "TestAssembly"),
            ];
        }
    }

    public static TagHelperCollection DefaultPAndInputTagHelperDescriptors
    {
        get
        {
            var pAgePropertyInfo = GetTestTypeRuntimeProperty("Age");
            var inputTypePropertyInfo = GetTestTypeRuntimeProperty("Type");
            var checkedPropertyInfo = GetTestTypeRuntimeProperty("Checked");

            return
            [
                CreateTagHelperDescriptor(
                    tagName: "p",
                    typeName: "TestNamespace.PTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "age", pAgePropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder.RequireTagStructure(TagStructure.NormalOrSelfClosing)
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                    ],
                    ruleBuilders:
                    [
                        builder => builder.RequireTagStructure(TagStructure.WithoutEndTag)
                    ]),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestNamespace.InputTagHelper2",
                    assemblyName: "TestAssembly",
                    attributes:
                    [
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                        builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "checked", checkedPropertyInfo),
                    ]),
            ];
        }
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>>? attributes = null,
        IEnumerable<Action<TagMatchingRuleDescriptorBuilder>>? ruleBuilders = null)
    {
        var builder = TagHelperDescriptorBuilder.CreateTagHelper(typeName, assemblyName);
        builder.SetTypeName(typeName, typeNamespace: null, typeNameIdentifier: null);

        if (attributes != null)
        {
            foreach (var attributeBuilder in attributes)
            {
                builder.BoundAttributeDescriptor(attributeBuilder);
            }
        }

        if (ruleBuilders != null)
        {
            foreach (var ruleBuilder in ruleBuilders)
            {
                builder.TagMatchingRuleDescriptor(innerRuleBuilder =>
                {
                    innerRuleBuilder.RequireTagName(tagName);
                    ruleBuilder(innerRuleBuilder);
                });
            }
        }
        else
        {
            builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));
        }

        var descriptor = builder.Build();

        return descriptor;
    }

    private static PropertyInfo GetTestTypeRuntimeProperty(string name)
    {
        var result = typeof(TestType).GetRuntimeProperty(name);
        Assert.NotNull(result);

        return result;
    }

    private static void BuildBoundAttributeDescriptorFromPropertyInfo(
        BoundAttributeDescriptorBuilder builder,
        string name,
        PropertyInfo propertyInfo)
    {
        builder
            .Name(name)
            .PropertyName(propertyInfo.Name)
            .TypeName(propertyInfo.PropertyType.FullName);

        if (propertyInfo.PropertyType.GetTypeInfo().IsEnum)
        {
            builder.AsEnum();
        }
    }

    private class TestType
    {
        public int Age { get; set; }

        public string? Type { get; set; }

        public bool Checked { get; set; }

        public string? BoundProperty { get; set; }
    }

    public static readonly string Code = """
        namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
        {
            public class TestTagHelperDescriptors
            {
                public enum MyEnum
                {
                    MyValue,
                    MySecondValue
                }
            }
        }
        """;
}
