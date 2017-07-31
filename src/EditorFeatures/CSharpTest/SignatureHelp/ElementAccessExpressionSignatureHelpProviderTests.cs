﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class ElementAccessExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public ElementAccessExpressionSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override ISignatureHelpProvider CreateSignatureHelpProvider()
        {
            return new ElementAccessExpressionSignatureHelpProvider();
        }

        #region "Regular tests"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn1()
        {
            var markup = @"
class C
{
    public string this[int a]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[$$|]];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(636117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636117")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnExpression()
        {
            var markup = @"
class C
{
    public string this[int a]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        C[] c = new C[1];
        c[0][$$
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlCommentsOn1()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for this.
    /// </summary>
    /// <param name=""a"">Param a</param>
    public string this[int a]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[$$|]];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a]", "Summary for this.", "Param a", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn2()
        {
            var markup = @"
class C
{
    public string this[int a, bool b]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[22, $$|]];
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a, bool b]", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlComentsOn2()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for this.
    /// </summary>
    /// <param name=""a"">Param a</param>
    /// <param name=""b"">Param b</param>
    public string this[int a, bool b]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[22, $$|]];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a, bool b]", "Summary for this.", "Param b", currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingBracketWithParameters()
        {
            var markup =
@"class C
{
    public string this[int a]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[$$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingBracketWithParametersOn2()
        {
            var markup = @"
class C
{
    public string this[int a, bool b]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[22, $$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a, bool b]", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        #endregion

        #region "Current Parameter Name"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestCurrentParameterName()
        {
            var markup = @"
class C
{
    public string this[int a, bool b]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[b: false, a: $$42|]];
    }
}";

            await VerifyCurrentParameterNameAsync(markup, "a");
        }

        #endregion

        #region "Trigger tests"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerBracket()
        {
            var markup = @"
class C
{
    public string this[int a]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[$$|]];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerComma()
        {
            var markup = @"
class C
{
    public string this[int a, bool b]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[42,$$|]];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a, bool b]", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestNoInvocationOnSpace()
        {
            var markup = @"
class C
{
    public string this[int a, bool b]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        var x = [|c[42, $$|]];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestTriggerCharacters()
        {
            char[] expectedCharacters = { ',', '[' };
            char[] unexpectedCharacters = { ' ', '(', '<' };

            VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
        }

        #endregion

        #region "EditorBrowsable tests"

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Indexer_PropertyAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo()[$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public int this[int x]
    {
        get { return 5; }
        set { }
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int Foo[int x]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Indexer_PropertyNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo()[$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public int this[int x]
    {
        get { return 5; }
        set { }
    }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("int Foo[int x]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsMetadataReference,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Indexer_PropertyAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo()[$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public int this[int x]
    {
        get { return 5; }
        set { }
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int Foo[int x]", string.Empty, string.Empty, currentParameterIndex: 0));

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
        public async Task EditorBrowsable_Indexer_PropertyNeverOnOneOfTwoOverloads()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo()[$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public int this[int x]
    {
        get { return 5; }
        set { }
    }

    public int this[double d]
    {
        get { return 5; }
        set { }
    }
}";

            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("int Foo[double d]", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("int Foo[double d]", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("int Foo[int x]", string.Empty, string.Empty, currentParameterIndex: 0),
            };

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Indexer_GetBrowsableNeverIgnored()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo()[$$
    }
}";

            var referencedCode = @"
public class Foo
{
    public int this[int x]
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        get { return 5; }
        set { }
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int Foo[int x]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Indexer_SetBrowsableNeverIgnored()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo()[$$
    }
}";

            var referencedCode = @"
public class Foo
{
    public int this[int x]
    {
        get { return 5; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        set { }
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int Foo[int x]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Indexer_GetSetBrowsableNeverIgnored()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo()[$$
    }
}";

            var referencedCode = @"
public class Foo
{
    public int this[int x]
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        get { return 5; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        set { }
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int Foo[int x]", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        #endregion

        #region Indexed Property tests

        [WorkItem(530811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530811")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task IndexedProperty()
        {
            var markup = @"class Program
{
    void M()
    {
            CCC c = new CCC();
            c.IndexProp[$$
    }
}";

            // Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
            var referencedCode = @"Imports System.Runtime.InteropServices

<ComImport()>
<GuidAttribute(CCC.ClassId)>
Public Class CCC

#Region ""COM GUIDs""
    Public Const ClassId As String = ""9d965fd2-1514-44f6-accd-257ce77c46b0""
    Public Const InterfaceId As String = ""a9415060-fdf0-47e3-bc80-9c18f7f39cf6""
    Public Const EventsId As String = ""c6a866a5-5f97-4b53-a5df-3739dc8ff1bb""
# End Region

            ''' <summary>
    ''' An index property from VB
    ''' </summary>
    ''' <param name=""p1"">p1 is an integer index</param>
    ''' <returns>A string</returns>
    Public Property IndexProp(ByVal p1 As Integer) As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class";

            var metadataItems = new List<SignatureHelpTestItem>();
            metadataItems.Add(new SignatureHelpTestItem("string CCC.IndexProp[int p1]", string.Empty, string.Empty, currentParameterIndex: 0));

            var projectReferenceItems = new List<SignatureHelpTestItem>();
            projectReferenceItems.Add(new SignatureHelpTestItem("string CCC.IndexProp[int p1]", "An index property from VB", "p1 is an integer index", currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                               referencedCode: referencedCode,
                                               expectedOrderedItemsMetadataReference: metadataItems,
                                               expectedOrderedItemsSameSolution: projectReferenceItems,
                                               sourceLanguage: LanguageNames.CSharp,
                                               referencedLanguage: LanguageNames.VisualBasic);
        }

        #endregion

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    public int this[int z]
    {
        get
        {
            return 0;
        }
    }
#endif
    void foo()
    {
        var x = this[$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = new SignatureHelpTestItem($"int C[int z]\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO,BAR"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    public int this[int z]
    {
        get
        {
            return 0;
        }
    }
#endif

#if BAR
    void foo()
    {
        var x = this[$$
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

            var expectedDescription = new SignatureHelpTestItem($"int C[int z]\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        public class IncompleteElementAccessExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
        {
            public IncompleteElementAccessExpressionSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
            {
            }

            internal override ISignatureHelpProvider CreateSignatureHelpProvider()
            {
                return new ElementAccessExpressionSignatureHelpProvider();
            }

            [WorkItem(636117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636117")]
            [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
            public async Task TestInvocation()
            {
                var markup = @"
class C
{
    public string this[int a]
    {
        get { return null; }
        set { }
    }
}

class D
{
    void Foo()
    {
        var c = new C();
        c[$$]
    }
}";

                var expectedOrderedItems = new List<SignatureHelpTestItem>();
                expectedOrderedItems.Add(new SignatureHelpTestItem("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0));

                await TestAsync(markup, expectedOrderedItems);
            }

            [WorkItem(939417, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939417")]
            [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
            public async Task ConditionalIndexer()
            {
                var markup = @"
public class P
{
    public int this[int z]
    {
        get
        {
            return 0;
        }
    }
 
    public void foo()
    {
        P p = null;
        p?[$$]
    }
}
";

                var expectedOrderedItems = new List<SignatureHelpTestItem>();
                expectedOrderedItems.Add(new SignatureHelpTestItem("int P[int z]", string.Empty, string.Empty, currentParameterIndex: 0));

                await TestAsync(markup, expectedOrderedItems);
            }

            [WorkItem(32, "https://github.com/dotnet/roslyn/issues/32")]
            [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
            public async Task NonIdentifierConditionalIndexer()
            {
                var expected = new[] { new SignatureHelpTestItem("char string[int index]") };
                await TestAsync(
@"class C
{
    void M()
    {
        """"?[$$ }
}", expected); // inline with a string literal
                await TestAsync(
@"class C
{
    void M()
    {
        """"?[/**/$$ }
}", expected); // inline with a string literal and multiline comment
                await TestAsync(
@"class C
{
    void M()
    {
        ("""")?[$$ }
}", expected); // parenthesized expression
                await TestAsync(
@"class C
{
    void M()
    {
        new System.String(' ', 1)?[$$ }
}", expected); // new object expression

                // more complicated parenthesized expression
                await TestAsync(
@"class C
{
    void M()
    {
        (null as System.Collections.Generic.List<int>)?[$$ }
}", new[] { new SignatureHelpTestItem("int System.Collections.Generic.List<int>[int index]") });
            }

            [WorkItem(1067933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
            [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
            public async Task InvokedWithNoToken()
            {
                var markup = @"
// foo[$$";

                await TestAsync(markup);
            }

            [WorkItem(2482, "https://github.com/dotnet/roslyn/issues/2482")]
            [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
            public async Task WhereExpressionLooksLikeArrayTypeSyntaxOfQualifiedName()
            {
                var markup = @"
class WithIndexer
{
    public int this[int index] { get { return 0; } }
}

class TestClass
{
    public WithIndexer Item { get; set; }

    public void Method(TestClass tc)
    {
        // `tc.Item[]` parses as ArrayTypeSyntax with an ElementType of QualifiedNameSyntax
        tc.Item[$$]
    }
}
";
                await TestAsync(markup, new[] { new SignatureHelpTestItem("int WithIndexer[int index]") }, usePreviousCharAsTrigger: true);
            }

            [WorkItem(20507, "https://github.com/dotnet/roslyn/issues/20507")]
            [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
            public async Task InConditionalIndexingFollowedByMemberAccess()
            {
                var markup = @"
class Indexable
{
    public Indexable this[int x] { get => null; }

    Indexable Count;

    static void Main(string[] args)
    {
        Indexable x;
        x?[$$].Count;
    }
}
";
                await TestAsync(markup, new[] { new SignatureHelpTestItem("Indexable Indexable[int x]") }, usePreviousCharAsTrigger: false);
            }

            [WorkItem(20507, "https://github.com/dotnet/roslyn/issues/20507")]
            [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
            public async Task InConditionalIndexingFollowedByConditionalAcesss()
            {
                var markup = @"
class Indexable
{
    public Indexable this[int x] { get => null; }

    Indexable Count;

    static void Main(string[] args)
    {
        Indexable x;
        x?[$$].Count?.Count;
    }
}
";
                await TestAsync(markup, new[] { new SignatureHelpTestItem("Indexable Indexable[int x]") }, usePreviousCharAsTrigger: false);
            }
        }
    }
}
