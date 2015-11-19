// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class AttributeSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public AttributeSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override ISignatureHelpProvider CreateSignatureHelpProvider()
        {
            return new AttributeSignatureHelpProvider();
        }

        #region "Regular tests"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParameters()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
}

[[|Something($$|]]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParametersMethodXmlComments()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <summary>Summary For Attribute</summary>
    public SomethingAttribute() { }
}

[[|Something($$|]]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", "Summary For Attribute", null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn1()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public SomethingAttribute(int someInteger, string someString) { }
}

[[|Something($$|]]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute(int someInteger, string someString)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlCommentsOn1()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <summary>
    /// Summary For Attribute
    /// </summary>
    /// <param name=""someInteger"">Param someInteger</param>
    /// <param name=""someString"">Param someString</param>
    public SomethingAttribute(int someInteger, string someString) { }
}

[[|Something($$
|]class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute(int someInteger, string someString)", "Summary For Attribute", "Param someInteger", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn2()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public SomethingAttribute(int someInteger, string someString) { }
}

[[|Something(22, $$|]]
class D
{
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute(int someInteger, string someString)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlComentsOn2()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <summary>
    /// Summary For Attribute
    /// </summary>
    /// <param name=""someInteger"">Param someInteger</param>
    /// <param name=""someString"">Param someString</param>
    public SomethingAttribute(int someInteger, string someString) { }
}

[[|Something(22, $$
|]class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute(int someInteger, string someString)", "Summary For Attribute", "Param someString", currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithClosingParen()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{ }

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationSpan1()
        {
            await TestAsync(
@"using System;

class C
{
    [[|Obsolete($$|])]
    void Foo()
    {
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationSpan2()
        {
            await TestAsync(
@"using System;

class C
{
    [[|Obsolete(    $$|])]
    void Foo()
    {
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationSpan3()
        {
            await TestAsync(
@"using System;

class C
{
    [[|Obsolete(

$$|]]
    void Foo()
    {
    }
}");
        }

        #endregion

        #region "Current Parameter Name"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestCurrentParameterName()
        {
            var markup = @"
using System;

class SomethingAttribute : Attribute
{
    public SomethingAttribute(int someParameter, bool somethingElse) { }
}

[[|Something(somethingElse: false, someParameter: $$22|])]
class C
{
}";

            await VerifyCurrentParameterNameAsync(markup, "someParameter");
        }

        #endregion

        #region "Setting fields in attributes"

        [WorkItem(545425)]
        [WorkItem(544139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithValidField()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public int foo;
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"SomethingAttribute({CSharpEditorResources.Properties}: [foo = int])", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidFieldReadonly()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public readonly int foo;
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidFieldStatic()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public static int foo;
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidFieldConst()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public const int foo = 42;
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        #endregion

        #region "Setting properties in attributes"

        [WorkItem(545425)]
        [WorkItem(544139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithValidProperty()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public int foo { get; set; }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"SomethingAttribute({CSharpEditorResources.Properties}: [foo = int])", string.Empty, string.Empty, currentParameterIndex: 0));

            // TODO: Bug 12319: Enable tests for script when this is fixed.
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidPropertyStatic()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public static int foo { get; set; }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidPropertyNoSetter()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public int foo { get { return 0; } }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidPropertyNoGetter()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public int foo { set { } }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidPropertyPrivateGetter()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public int foo { private get; set; }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithInvalidPropertyPrivateSetter()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    public int foo { get; private set; }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        #endregion

        #region "Setting fields and arguments"

        [WorkItem(545425)]
        [WorkItem(544139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithArgumentsAndNamedParameters1()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <param name=""foo"">FooParameter</param>
    /// <param name=""bar"">BarParameter</param>
    public SomethingAttribute(int foo = 0, string bar = null) { }
    public int fieldfoo { get; set; }
    public string fieldbar { get; set; }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"SomethingAttribute([int foo = 0], [string bar = null], {CSharpEditorResources.Properties}: [fieldbar = string], [fieldfoo = int])", string.Empty, "FooParameter", currentParameterIndex: 0));

            // TODO: Bug 12319: Enable tests for script when this is fixed.
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false);
        }

        [WorkItem(545425)]
        [WorkItem(544139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithArgumentsAndNamedParameters2()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <param name=""foo"">FooParameter</param>
    /// <param name=""bar"">BarParameter</param>
    public SomethingAttribute(int foo = 0, string bar = null) { }
    public int fieldfoo { get; set; }
    public string fieldbar { get; set; }
}

[[|Something(22, $$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"SomethingAttribute([int foo = 0], [string bar = null], {CSharpEditorResources.Properties}: [fieldbar = string], [fieldfoo = int])", string.Empty, "BarParameter", currentParameterIndex: 1));

            // TODO: Bug 12319: Enable tests for script when this is fixed.
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false);
        }

        [WorkItem(545425)]
        [WorkItem(544139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithArgumentsAndNamedParameters3()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <param name=""foo"">FooParameter</param>
    /// <param name=""bar"">BarParameter</param>
    public SomethingAttribute(int foo = 0, string bar = null) { }
    public int fieldfoo { get; set; }
    public string fieldbar { get; set; }
}

[[|Something(22, null, $$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"SomethingAttribute([int foo = 0], [string bar = null], {CSharpEditorResources.Properties}: [fieldbar = string], [fieldfoo = int])", string.Empty, string.Empty, currentParameterIndex: 2));

            // TODO: Bug 12319: Enable tests for script when this is fixed.
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false);
        }

        [WorkItem(545425)]
        [WorkItem(544139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithOptionalArgumentAndNamedParameterWithSameName1()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <param name=""foo"">FooParameter</param>
    public SomethingAttribute(int foo = 0) { }
    public int foo { get; set; }
}

[[|Something($$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"SomethingAttribute([int foo = 0], {CSharpEditorResources.Properties}: [foo = int])", string.Empty, "FooParameter", currentParameterIndex: 0));

            // TODO: Bug 12319: Enable tests for script when this is fixed.
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false);
        }

        [WorkItem(545425)]
        [WorkItem(544139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestAttributeWithOptionalArgumentAndNamedParameterWithSameName2()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
    /// <param name=""foo"">FooParameter</param>
    public SomethingAttribute(int foo = 0) { }
    public int foo { get; set; }
}

[[|Something(22, $$|])]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"SomethingAttribute([int foo = 0], {CSharpEditorResources.Properties}: [foo = int])", string.Empty, string.Empty, currentParameterIndex: 1));

            // TODO: Bug 12319: Enable tests for script when this is fixed.
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false);
        }

        #endregion

        #region "Trigger tests"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerParens()
        {
            var markup = @"
using System;

class SomethingAttribute : Attribute
{
    public SomethingAttribute(int someParameter, bool somethingElse) { }
}

[[|Something($$|])]
class C
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute(int someParameter, bool somethingElse)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerComma()
        {
            var markup = @"
using System;

class SomethingAttribute : Attribute
{
    public SomethingAttribute(int someParameter, bool somethingElse) { }
}

[[|Something(22,$$|])]
class C
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("SomethingAttribute(int someParameter, bool somethingElse)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestNoInvocationOnSpace()
        {
            var markup = @"
using System;

class SomethingAttribute : Attribute
{
    public SomethingAttribute(int someParameter, bool somethingElse) { }
}

[[|Something(22, $$|])]
class C
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestTriggerCharacters()
        {
            char[] expectedCharacters = { ',', '(' };
            char[] unexpectedCharacters = { ' ', '[', '<' };

            VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
        }

        #endregion

        #region "EditorBrowsable tests"
        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Attribute_BrowsableAlways()
        {
            var markup = @"
[MyAttribute($$
class Program
{
}";

            var referencedCode = @"

public class MyAttribute
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public MyAttribute(int x)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Attribute_BrowsableNever()
        {
            var markup = @"
[MyAttribute($$
class Program
{
}";

            var referencedCode = @"

public class MyAttribute
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public MyAttribute(int x)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Attribute_BrowsableAdvanced()
        {
            var markup = @"
[MyAttribute($$
class Program
{
}";

            var referencedCode = @"

public class MyAttribute
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public MyAttribute(int x)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
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

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Attribute_BrowsableMixed()
        {
            var markup = @"
[MyAttribute($$
class Program
{
}";

            var referencedCode = @"

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
}";

            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("MyAttribute(int x)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("MyAttribute(int x, int y)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    class Secret : System.Attribute
    {
    }
#endif
    [Secret($$
    void Foo()
    {
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = new SignatureHelpTestItem($"Secret()\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO,BAR"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    class Secret : System.Attribute
    {
    }
#endif

#if BAR
    [Secret($$
    void Foo()
    {
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument"" />
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            var expectedDescription = new SignatureHelpTestItem($"Secret()\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [WorkItem(1067933)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InvokedWithNoToken()
        {
            var markup = @"
// [foo($$";

            await TestAsync(markup);
        }

        [WorkItem(1081535)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithBadParameterList()
        {
            var markup = @"
class SomethingAttribute : System.Attribute
{
}

[Something{$$]
class D
{
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems);
        }
    }
}
