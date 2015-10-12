// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutParameters()
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

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutParametersMethodXmlComments()
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

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersOn1()
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

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersXmlCommentsOn1()
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

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersOn2()
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

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersXmlComentsOn2()
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

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestThisInvocation()
        {
            var markup = @"
class Foo
{
    public Foo(int a, int b) { }
    public Foo() [|: this(2, $$3|]) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 1),
                new SignatureHelpTestItem("Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1),
            };

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutClosingParen()
        {
            var markup = @"
class Foo
{
    public Foo(int a, int b) { }
    public Foo() [|: this(2, $$
|]}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 1),
                new SignatureHelpTestItem("Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1),
            };

            Test(markup, expectedOrderedItems);
        }

        #endregion

        #region "Current Parameter Name"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestCurrentParameterName()
        {
            var markup = @"
class Foo
{
    public Foo(int a, int b) { }
    public Foo() : this(b: 2, a: $$
}";

            VerifyCurrentParameterName(markup, "a");
        }

        #endregion

        #region "Trigger tests"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationOnTriggerParens()
        {
            var markup = @"
class Foo
{
    public Foo(int a) { }
    public Foo() : this($$
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 0),
                new SignatureHelpTestItem("Foo(int a)", string.Empty, string.Empty, currentParameterIndex: 0),
            };

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationOnTriggerComma()
        {
            var markup = @"
class Foo
{
    public Foo(int a, int b) { }
    public Foo() : this(2,$$
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 1),
                new SignatureHelpTestItem("Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1),
            };

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestNoInvocationOnSpace()
        {
            var markup = @"
class Foo
{
    public Foo(int a, int b) { }
    public Foo() : this(2, $$
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
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
        public void EditorBrowsable_ConstructorInitializer_BrowsableStateAlways()
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

            TestSignatureHelpInEditorBrowsableContexts(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void EditorBrowsable_ConstructorInitializer_BrowsableStateNever()
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

            TestSignatureHelpInEditorBrowsableContexts(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void EditorBrowsable_ConstructorInitializer_BrowsableStateAdvanced()
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

            TestSignatureHelpInEditorBrowsableContexts(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp,
                                                hideAdvancedMembers: true);

            TestSignatureHelpInEditorBrowsableContexts(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp,
                                                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void EditorBrowsable_ConstructorInitializer_BrowsableStateMixed()
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

            TestSignatureHelpInEditorBrowsableContexts(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
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
            var expectedDescription = new SignatureHelpTestItem($"Secret(int secret)\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
            VerifyItemWithReferenceWorker(markup, new[] { expectedDescription }, false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO,BAR"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
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

            var expectedDescription = new SignatureHelpTestItem($"Secret(int secret)\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
            VerifyItemWithReferenceWorker(markup, new[] { expectedDescription }, false);
        }

        [WorkItem(1067933)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void InvokedWithNoToken()
        {
            var markup = @"
// foo($$";

            Test(markup);
        }

        [WorkItem(1082601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithBadParameterList()
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
            Test(markup, expectedOrderedItems);
        }
    }
}
