// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class ConstructorInitializerSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public ConstructorInitializerSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override ISignatureHelpProvider CreateSignatureHelpProvider()
        {
            return new ConstructorInitializerSignatureHelpProvider();
        }

        #region "Regular tests"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParameters()
        {
            var markup = @"
class BaseClass
{
    public BaseClass() { }
}

class Derived : BaseClass
{
    public Derived() [|: base($$|])
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParametersMethodXmlComments()
        {
            var markup = @"
class BaseClass
{
    /// <summary>Summary for BaseClass</summary>
    public BaseClass() { }
}

class Derived : BaseClass
{
    public Derived() [|: base($$|])
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass()", "Summary for BaseClass", null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn1()
        {
            var markup = @"
class BaseClass
{
    public BaseClass(int a, int b) { }
}

class Derived : BaseClass
{
    public Derived() [|: base($$2, 3|])
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlCommentsOn1()
        {
            var markup = @"
class BaseClass
{
    /// <summary>Summary for BaseClass</summary>
    /// <param name=""a"">Param a</param>
    /// <param name=""b"">Param b</param>
    public BaseClass(int a, int b) { }
}

class Derived : BaseClass
{
    public Derived() [|: base($$2, 3|])
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass(int a, int b)", "Summary for BaseClass", "Param a", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn2()
        {
            var markup = @"
class BaseClass
{
    /// <summary>Summary for BaseClass</summary>
    /// <param name=""a"">Param a</param>
    /// <param name=""b"">Param b</param>
    public BaseClass(int a, int b) { }
}


class Derived : BaseClass
{
    public Derived() [|: base(2, $$3|])
    { }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass(int a, int b)", "Summary for BaseClass", "Param b", currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlComentsOn2()
        {
            var markup = @"
class BaseClass
{
    /// <summary>Summary for BaseClass</summary>
    /// <param name=""a"">Param a</param>
    /// <param name=""b"">Param b</param>
    public BaseClass(int a, int b) { }
} 

class Derived : BaseClass
{
    public Derived() [|: base(2, $$3|])
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass(int a, int b)", "Summary for BaseClass", "Param b", currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestThisInvocation()
        {
            var markup = @"
class Goo
{
    public Goo(int a, int b) { }
    public Goo() [|: this(2, $$3|]) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestThisInvocationWithNonEmptyArgumentList()
        {
            var markup = @"
class Foo
{
    public Foo(int a, int b) [|: this($$|]) { }
    public Foo() { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingParen()
        {
            var markup = @"
class Goo
{
    public Goo(int a, int b) { }
    public Goo() [|: this(2, $$
|]}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestThisInvocationWithoutClosingParenWithNonEmptyArgumentList()
        {
            var markup = @"
class Foo
{
    public Foo() { }
    public Foo(int a, int b)  [|: this($$
|]}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_PickInt()
        {
            var markup = @"
class D
{
    D() [|: this(i: 1$$|]) { }

    D(D filtered) => throw null;
    D(string i) => throw null;
    D(int i) => throw null;
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("D(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem("D(string i)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_PickString()
        {
            var markup = @"
class D
{
    D() [|: this(i: null$$|]) { }

    D(D filtered) => throw null;
    D(string i) => throw null;
    D(int i) => throw null;
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("D(int i)", currentParameterIndex: 0),
                new SignatureHelpTestItem("D(string i)", currentParameterIndex: 0, isSelected: true),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        #endregion

        #region "Current Parameter Name"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestCurrentParameterName()
        {
            var markup = @"
class Goo
{
    public Goo(int a, int b) { }
    public Goo() : this(b: 2, a: $$
}";

            await VerifyCurrentParameterNameAsync(markup, "a");
        }

        #endregion

        #region "Trigger tests"

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerParens()
        {
            var markup = @"
class Goo
{
    public Goo(int a) { }
    public Goo() : this($$
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int a)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerParensWithNonEmptyArgumentList()
        {
            var markup = @"
class Foo
{
    public Foo(int a) : this($$
    public Foo() { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerComma()
        {
            var markup = @"
class Goo
{
    public Goo(int a, int b) { }
    public Goo() : this(2,$$
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WorkItem(2579, "https://github.com/dotnet/roslyn/issues/2579")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerCommaWithNonEmptyArgumentList()
        {
            var markup = @"
class Foo
{
    public Foo(int a, int b) : this($$
    public Foo() { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestNoInvocationOnSpace()
        {
            var markup = @"
class Goo
{
    public Goo(int a, int b) { }
    public Goo() : this(2, $$
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestTriggerCharacters()
        {
            char[] expectedCharacters = { ',', '(' };
            char[] unexpectedCharacters = { ' ', '[', '<' };

            VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
        }

        #endregion

        #region "EditorBrowsable tests"
        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateAlways()
        {
            var markup = @"
class DerivedClass : BaseClass
{
    public DerivedClass() : base($$
}";

            var referencedCode = @"
public class BaseClass
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public BaseClass(int x)
    { }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateNever()
        {
            var markup = @"
class DerivedClass : BaseClass
{
    public DerivedClass() : base($$
}";

            var referencedCode = @"
public class BaseClass
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public BaseClass(int x)
    { }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateAdvanced()
        {
            var markup = @"
class DerivedClass : BaseClass
{
    public DerivedClass() : base($$
}";

            var referencedCode = @"
public class BaseClass
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public BaseClass(int x)
    { }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateMixed()
        {
            var markup = @"
class DerivedClass : BaseClass
{
    public DerivedClass() : base($$
}";

            var referencedCode = @"
public class BaseClass
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public BaseClass(int x)
    { }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public BaseClass(int x, int y)
    { }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("BaseClass(int x, int y)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        #endregion

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if GOO
    class Secret
    {
        public Secret(int secret)
        {
        }
    }
#endif
    class SuperSecret : Secret
    {
        public SuperSecret(int secret) : base($$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = new SignatureHelpTestItem($"Secret(int secret)\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO,BAR"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if GOO
    class Secret
    {
        public Secret(int secret)
        {
        }
    }
#endif

#if BAR
    class SuperSecret : Secret
    {
        public SuperSecret(int secret) : base($$
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

            var expectedDescription = new SignatureHelpTestItem($"Secret(int secret)\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [WorkItem(1067933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InvokedWithNoToken()
        {
            var markup = @"
// goo($$";

            await TestAsync(markup);
        }

        [WorkItem(1082601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082601")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithBadParameterList()
        {
            var markup = @"
class BaseClass
{
    public BaseClass() { }
}

class Derived : BaseClass
{
    public Derived() [|: base{$$|])
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss1()
        {
            var markup = @"
class D { public D(object o) {} }
class C : D
{
    public C() [|: base(($$)
    |]{}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("D(object o)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss2()
        {
            var markup = @"
class D { public D(object o) {} }
class C : D
{
    public C() [|: base((1,$$) |]{}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("D(object o)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss3()
        {
            var markup = @"
class D { public D(object o) {} }
class C : D
{
    public C() [|: base((1, ($$)
    |]{}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("D(object o)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss4()
        {
            var markup = @"
class D { public D(object o) {} }
class C : D
{
    public C() [|: base((1, (2,$$) |]{}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("D(object o)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }
    }
}
