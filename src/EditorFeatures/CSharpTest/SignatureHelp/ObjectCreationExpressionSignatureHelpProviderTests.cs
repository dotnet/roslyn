// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class ObjectCreationExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public ObjectCreationExpressionSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override ISignatureHelpProvider CreateSignatureHelpProvider()
        {
            return new ObjectCreationExpressionSignatureHelpProvider();
        }

        #region "Regular tests"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutParameters()
        {
            var markup = @"
class C
{
    void foo()
    {
        var c = [|new C($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C()", string.Empty, null, currentParameterIndex: 0));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutParametersMethodXmlComments()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for C
    /// </summary>
    C() { }

    void Foo()
    {
        C c = [|new C($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C()", "Summary for C", null, currentParameterIndex: 0));
            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersOn1()
        {
            var markup = @"
class C
{
    C(int a, int b) { }

    void Foo()
    {
        C c = [|new C($$2, 3|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersXmlCommentsOn1()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for C
    /// </summary>
    /// <param name=""a"">Param a</param>
    /// <param name=""b"">Param b</param>
    C(int a, int b) { }

    void Foo()
    {
        C c = [|new C($$2, 3|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C(int a, int b)", "Summary for C", "Param a", currentParameterIndex: 0));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersOn2()
        {
            var markup = @"
class C
{
    C(int a, int b) { }

    void Foo()
    {
        C c = [|new C(2, $$3|]);
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithParametersXmlComentsOn2()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for C
    /// </summary>
    /// <param name=""a"">Param a</param>
    /// <param name=""b"">Param b</param>
    C(int a, int b) { }

    void Foo()
    {
        C c = [|new C(2, $$3|]);
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C(int a, int b)", "Summary for C", "Param b", currentParameterIndex: 1));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutClosingParen()
        {
            var markup = @"
class C
{
    void foo()
    {
        var c = [|new C($$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C()", string.Empty, null, currentParameterIndex: 0));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutClosingParenWithParameters()
        {
            var markup = @"
class C
{
    C(int a, int b) { }

    void Foo()
    {
        C c = [|new C($$2, 3
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationWithoutClosingParenWithParametersOn2()
        {
            var markup = @"
class C
{
    C(int a, int b) { }

    void Foo()
    {
        C c = [|new C(2, $$3
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            Test(markup, expectedOrderedItems);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationOnLambda()
        {
            var markup = @"
using System;

class C
{
    void foo()
    {
        var bar = [|new Action<int, int>($$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Action<int, int>(void (int, int) target)", string.Empty, string.Empty, currentParameterIndex: 0));

            Test(markup, expectedOrderedItems);
        }

        #endregion

        #region "Current Parameter Name"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestCurrentParameterName()
        {
            var markup = @"
class C
{
    C(int a, string b)
    {
    }

    void foo()
    {
        var c = [|new C(b: string.Empty, $$a: 2|]);
    }
}";

            VerifyCurrentParameterName(markup, "a");
        }

        #endregion

        #region "Trigger tests"

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationOnTriggerParens()
        {
            var markup = @"
class C
{
    void foo()
    {
        var c = [|new C($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C()", string.Empty, null, currentParameterIndex: 0));

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestInvocationOnTriggerComma()
        {
            var markup = @"
class C
{
    C(int a, string b)
    {
    }

    void foo()
    {
        var c = [|new C(2,$$string.Empty|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("C(int a, string b)", string.Empty, string.Empty, currentParameterIndex: 1));

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestNoInvocationOnSpace()
        {
            var markup = @"
class C
{
    C(int a, string b)
    {
    }

    void foo()
    {
        var c = [|new C(2, $$string.Empty|]);
    }
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
        public void EditorBrowsable_Constructor_BrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public Foo(int x)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Foo(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            TestSignatureHelpInEditorBrowsableContexts(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void EditorBrowsable_Constructor_BrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Foo(int x)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Foo(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            TestSignatureHelpInEditorBrowsableContexts(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void EditorBrowsable_Constructor_BrowsableAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public Foo()
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Foo()", string.Empty, null, currentParameterIndex: 0));

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
        public void EditorBrowsable_Constructor_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public Foo(int x)
    {
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Foo(long y)
    {
    }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("Foo(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("Foo(int x)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("Foo(long y)", string.Empty, string.Empty, currentParameterIndex: 0));

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
    class D
    {
    }
#endif
    void foo()
    {
        var x = new D($$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = new SignatureHelpTestItem($"D()\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
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
    class D
    {
    }
#endif

#if BAR
    void foo()
    {
        var x = new D($$
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

            var expectedDescription = new SignatureHelpTestItem($"D()\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
            VerifyItemWithReferenceWorker(markup, new[] { expectedDescription }, false);
        }

        [WorkItem(1067933)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void InvokedWithNoToken()
        {
            var markup = @"
// new foo($$";

            Test(markup);
        }

        [WorkItem(1078993)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestSigHelpInIncorrectObjectCreationExpression()
        {
            var markup = @"
class C
{
    void foo(C c)
    {
        foo([|new C{$$|]
    }
}";

            Test(markup);
        }
    }
}
