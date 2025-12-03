// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class AttributeSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(AttributeSignatureHelpProvider);

    #region "Regular tests"

    [Fact]
    public async Task TestInvocationWithoutParameters()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
            }

            [[|Something($$|]]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithoutParametersMethodXmlComments()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", "Summary For Attribute", null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <summary>Summary For Attribute</summary>
                public SomethingAttribute() { }
            }

            [[|Something($$|]]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_PickInt()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int i)", currentParameterIndex: 0, isSelected: true),
            new("SomethingAttribute(string i)", currentParameterIndex: 0),
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public SomethingAttribute(string i) => throw null;
                public SomethingAttribute(int i) => throw null;
                public SomethingAttribute(byte filtered) => throw null;
            }
            [[|Something(i: 1$$|])]
            class D { }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_PickString()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int i)", currentParameterIndex: 0),
            new("SomethingAttribute(string i)", currentParameterIndex: 0, isSelected: true),
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public SomethingAttribute(string i) => throw null;
                public SomethingAttribute(int i) => throw null;
                public SomethingAttribute(byte filtered) => throw null;
            }
            [[|Something(i: null$$|])]
            class D { }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int someInteger, string someString)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public SomethingAttribute(int someInteger, string someString) { }
            }

            [[|Something($$|]]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlCommentsOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int someInteger, string someString)", "Summary For Attribute", "Param someInteger", currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <summary>
                /// Summary For Attribute
                /// </summary>
                /// <param name="someInteger">Param someInteger</param>
                /// <param name="someString">Param someString</param>
                public SomethingAttribute(int someInteger, string someString) { }
            }

            [[|Something($$
            |]class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int someInteger, string someString)", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public SomethingAttribute(int someInteger, string someString) { }
            }

            [[|Something(22, $$|]]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlComentsOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int someInteger, string someString)", "Summary For Attribute", "Param someString", currentParameterIndex: 1)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <summary>
                /// Summary For Attribute
                /// </summary>
                /// <param name="someInteger">Param someInteger</param>
                /// <param name="someString">Param someString</param>
                public SomethingAttribute(int someInteger, string someString) { }
            }

            [[|Something(22, $$
            |]class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithClosingParen()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            { }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public Task TestInvocationSpan1()
        => TestAsync(
            """
            using System;

            class C
            {
                [[|Obsolete($$|])]
                void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task TestInvocationSpan2()
        => TestAsync(
            """
            using System;

            class C
            {
                [[|Obsolete($$|])]
                void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task TestInvocationSpan3()
        => TestAsync(
            """
            using System;

            class C
            {
                [[|Obsolete(

            $$|]]
                void Goo()
                {
                }
            }
            """);

    #endregion

    #region "Current Parameter Name"

    [Fact]
    public Task TestCurrentParameterName()
        => VerifyCurrentParameterNameAsync("""
            using System;

            class SomethingAttribute : Attribute
            {
                public SomethingAttribute(int someParameter, bool somethingElse) { }
            }

            [[|Something(somethingElse: false, someParameter: $$22|])]
            class C
            {
            }
            """, "someParameter");

    #endregion

    #region "Setting fields in attributes"

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544139")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545425")]
    public async Task TestAttributeWithValidField()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"SomethingAttribute({FeaturesResources.Properties}: [goo = int])", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public int goo;
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    [Fact]
    public async Task TestAttributeWithInvalidFieldReadonly()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public readonly int goo;
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestAttributeWithInvalidFieldStatic()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public static int goo;
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestAttributeWithInvalidFieldConst()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public const int goo = 42;
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    #endregion

    #region "Setting properties in attributes"

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544139")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545425")]
    public async Task TestAttributeWithValidProperty()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"SomethingAttribute({FeaturesResources.Properties}: [goo = int])", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        // TODO: Bug 12319: Enable tests for script when this is fixed.
        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public int goo { get; set; }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    [Fact]
    public async Task TestAttributeWithInvalidPropertyStatic()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public static int goo { get; set; }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestAttributeWithInvalidPropertyNoSetter()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public int goo { get { return 0; } }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestAttributeWithInvalidPropertyNoGetter()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public int goo { set { } }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestAttributeWithInvalidPropertyPrivateGetter()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public int goo { private get; set; }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestAttributeWithInvalidPropertyPrivateSetter()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                public int goo { get; private set; }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23664")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/12544")]
    public async Task TestAttributeWithOverriddenProperty()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"DerivedAttribute({FeaturesResources.Properties}: [Name = string])", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            cusing System;

            class BaseAttribute : Attribute
            {
                public virtual string Name { get; set; }
            }

            class DerivedAttribute : BaseAttribute
            {
                public override string Name { get; set; }
            }

            [[|Derived($$|])]
            class C
            {

            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    #endregion

    #region "Setting fields and arguments"

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544139")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545425")]
    public async Task TestAttributeWithArgumentsAndNamedParameters1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"SomethingAttribute([int goo = 0], [string bar = null], {FeaturesResources.Properties}: [fieldbar = string], [fieldfoo = int])", string.Empty, "GooParameter", currentParameterIndex: 0)
        };

        // TODO: Bug 12319: Enable tests for script when this is fixed.
        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <param name="goo">GooParameter</param>
                /// <param name="bar">BarParameter</param>
                public SomethingAttribute(int goo = 0, string bar = null) { }
                public int fieldfoo { get; set; }
                public string fieldbar { get; set; }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544139")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545425")]
    public async Task TestAttributeWithArgumentsAndNamedParameters2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"SomethingAttribute([int goo = 0], [string bar = null], {FeaturesResources.Properties}: [fieldbar = string], [fieldfoo = int])", string.Empty, "BarParameter", currentParameterIndex: 1)
        };

        // TODO: Bug 12319: Enable tests for script when this is fixed.
        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <param name="goo">GooParameter</param>
                /// <param name="bar">BarParameter</param>
                public SomethingAttribute(int goo = 0, string bar = null) { }
                public int fieldfoo { get; set; }
                public string fieldbar { get; set; }
            }

            [[|Something(22, $$|])]
            class D
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544139")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545425")]
    public async Task TestAttributeWithArgumentsAndNamedParameters3()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"SomethingAttribute([int goo = 0], [string bar = null], {FeaturesResources.Properties}: [fieldbar = string], [fieldfoo = int])", string.Empty, string.Empty, currentParameterIndex: 2)
        };

        // TODO: Bug 12319: Enable tests for script when this is fixed.
        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <param name="goo">GooParameter</param>
                /// <param name="bar">BarParameter</param>
                public SomethingAttribute(int goo = 0, string bar = null) { }
                public int fieldfoo { get; set; }
                public string fieldbar { get; set; }
            }

            [[|Something(22, null, $$|])]
            class D
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544139")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545425")]
    public async Task TestAttributeWithOptionalArgumentAndNamedParameterWithSameName1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"SomethingAttribute([int goo = 0], {FeaturesResources.Properties}: [goo = int])", string.Empty, "GooParameter", currentParameterIndex: 0)
        };

        // TODO: Bug 12319: Enable tests for script when this is fixed.
        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <param name="goo">GooParameter</param>
                public SomethingAttribute(int goo = 0) { }
                public int goo { get; set; }
            }

            [[|Something($$|])]
            class D
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544139")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545425")]
    public async Task TestAttributeWithOptionalArgumentAndNamedParameterWithSameName2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"SomethingAttribute([int goo = 0], {FeaturesResources.Properties}: [goo = int])", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        // TODO: Bug 12319: Enable tests for script when this is fixed.
        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
                /// <param name="goo">GooParameter</param>
                public SomethingAttribute(int goo = 0) { }
                public int goo { get; set; }
            }

            [[|Something(22, $$|])]
            class D
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false);
    }

    #endregion

    #region "Trigger tests"

    [Fact]
    public async Task TestInvocationOnTriggerParens()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int someParameter, bool somethingElse)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            using System;

            class SomethingAttribute : Attribute
            {
                public SomethingAttribute(int someParameter, bool somethingElse) { }
            }

            [[|Something($$|])]
            class C
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestInvocationOnTriggerComma()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("SomethingAttribute(int someParameter, bool somethingElse)", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
            using System;

            class SomethingAttribute : Attribute
            {
                public SomethingAttribute(int someParameter, bool somethingElse) { }
            }

            [[|Something(22,$$|])]
            class C
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestNoInvocationOnSpace()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>();
        await TestAsync("""
            using System;

            class SomethingAttribute : Attribute
            {
                public SomethingAttribute(int someParameter, bool somethingElse) { }
            }

            [[|Something(22, $$|])]
            class C
            {
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public void TestTriggerCharacters()
    {
        char[] expectedCharacters = [',', '('];
        char[] unexpectedCharacters = [' ', '[', '<'];

        VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
    }

    #endregion

    #region "EditorBrowsable tests"
    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Attribute_BrowsableAlways()
    {
        var markup = """
            [MyAttribute($$
            class Program
            {
            }
            """;

        var referencedCode = """
            public class MyAttribute
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public MyAttribute(int x)
                {
                }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Attribute_BrowsableNever()
    {
        var markup = """
            [MyAttribute($$
            class Program
            {
            }
            """;

        var referencedCode = """
            public class MyAttribute
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public MyAttribute(int x)
                {
                }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: [],
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Attribute_BrowsableAdvanced()
    {
        var markup = """
            [MyAttribute($$
            class Program
            {
            }
            """;

        var referencedCode = """
            public class MyAttribute
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
                public MyAttribute(int x)
                {
                }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: [],
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp,
                                            hideAdvancedMembers: true);

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp,
                                            hideAdvancedMembers: false);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Attribute_BrowsableMixed()
    {
        var markup = """
            [MyAttribute($$
            class Program
            {
            }
            """;

        var referencedCode = """
            public class MyAttribute
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public MyAttribute(int x)
                {
                }

                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public MyAttribute(int x, int y)
                {
                }
            }
            """;

        var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>
        {
            new("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>
        {
            new("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0),
            new("MyAttribute(int x, int y)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                            expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    #endregion

    [Fact]
    public async Task FieldUnavailableInOneLinkedFile()
    {
        var expectedDescription = new SignatureHelpTestItem($"""
            Secret()

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0);
        await VerifyItemWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                class Secret : System.Attribute
                {
                }
            #endif
                [Secret($$
                void Goo()
                {
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [expectedDescription], false);
    }

    [Fact]
    public async Task ExcludeFilesWithInactiveRegions()
    {
        var expectedDescription = new SignatureHelpTestItem($"""
            Secret()

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0);
        await VerifyItemWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO,BAR">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                class Secret : System.Attribute
                {
                }
            #endif

            #if BAR
                [Secret($$
                void Goo()
                {
                }
            #endif
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument" />
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj3" PreprocessorSymbols="BAR">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [expectedDescription], false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
    public Task InvokedWithNoToken()
        => TestAsync("""
            // [goo($$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1081535")]
    public async Task TestInvocationWithBadParameterList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>();
        await TestAsync("""
            class SomethingAttribute : System.Attribute
            {
            }

            [Something{$$]
            class D
            {
            }
            """, expectedOrderedItems);
    }
}
