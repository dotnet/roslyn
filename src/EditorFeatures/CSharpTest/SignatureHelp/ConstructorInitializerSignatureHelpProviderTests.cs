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
public sealed class ConstructorInitializerSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(ConstructorInitializerSignatureHelpProvider);

    #region "Regular tests"

    [Fact]
    public async Task TestInvocationWithoutParameters()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass()", string.Empty, null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class BaseClass
            {
                public BaseClass() { }
            }

            class Derived : BaseClass
            {
                public Derived() [|: base($$|])
                { }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithoutParametersMethodXmlComments()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass()", "Summary for BaseClass", null, currentParameterIndex: 0)
        };

        await TestAsync("""
            class BaseClass
            {
                /// <summary>Summary for BaseClass</summary>
                public BaseClass() { }
            }

            class Derived : BaseClass
            {
                public Derived() [|: base($$|])
                { }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class BaseClass
            {
                public BaseClass(int a, int b) { }
            }

            class Derived : BaseClass
            {
                public Derived() [|: base($$2, 3|])
                { }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlCommentsOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int a, int b)", "Summary for BaseClass", "Param a", currentParameterIndex: 0)
        };

        await TestAsync("""
            class BaseClass
            {
                /// <summary>Summary for BaseClass</summary>
                /// <param name="a">Param a</param>
                /// <param name="b">Param b</param>
                public BaseClass(int a, int b) { }
            }

            class Derived : BaseClass
            {
                public Derived() [|: base($$2, 3|])
                { }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int a, int b)", "Summary for BaseClass", "Param b", currentParameterIndex: 1)
        };

        await TestAsync("""
            class BaseClass
            {
                /// <summary>Summary for BaseClass</summary>
                /// <param name="a">Param a</param>
                /// <param name="b">Param b</param>
                public BaseClass(int a, int b) { }
            }


            class Derived : BaseClass
            {
                public Derived() [|: base(2, $$3|])
                { }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlComentsOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int a, int b)", "Summary for BaseClass", "Param b", currentParameterIndex: 1)
        };

        await TestAsync("""
            class BaseClass
            {
                /// <summary>Summary for BaseClass</summary>
                /// <param name="a">Param a</param>
                /// <param name="b">Param b</param>
                public BaseClass(int a, int b) { }
            } 

            class Derived : BaseClass
            {
                public Derived() [|: base(2, $$3|])
                { }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestThisInvocation()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
            class Goo
            {
                public Goo(int a, int b) { }
                public Goo() [|: this(2, $$3|]) { }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestThisInvocationWithNonEmptyArgumentList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Foo()", string.Empty, null, currentParameterIndex: 0),
        };

        await TestAsync("""
            class Foo
            {
                public Foo(int a, int b) [|: this($$|]) { }
                public Foo() { }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestInvocationWithoutClosingParen()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
            class Goo
            {
                public Goo(int a, int b) { }
                public Goo() [|: this(2, $$
            |]}
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestThisInvocationWithoutClosingParenWithNonEmptyArgumentList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Foo()", string.Empty, null, currentParameterIndex: 0),
        };

        await TestAsync("""
            class Foo
            {
                public Foo() { }
                public Foo(int a, int b)  [|: this($$
            |]}
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_PickInt()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("D(int i)", currentParameterIndex: 0, isSelected: true),
            new("D(string i)", currentParameterIndex: 0),
        };

        await TestAsync("""
            class D
            {
                D() [|: this(i: 1$$|]) { }

                D(D filtered) => throw null;
                D(string i) => throw null;
                D(int i) => throw null;
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_PickString()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("D(int i)", currentParameterIndex: 0),
            new("D(string i)", currentParameterIndex: 0, isSelected: true),
        };

        await TestAsync("""
            class D
            {
                D() [|: this(i: null$$|]) { }

                D(D filtered) => throw null;
                D(string i) => throw null;
                D(int i) => throw null;
            }
            """, expectedOrderedItems);
    }

    #endregion

    #region "Current Parameter Name"

    [Fact]
    public Task TestCurrentParameterName()
        => VerifyCurrentParameterNameAsync("""
            class Goo
            {
                public Goo(int a, int b) { }
                public Goo() : this(b: 2, a: $$
            }
            """, "a");

    #endregion

    #region "Trigger tests"

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestInvocationOnTriggerParens()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Goo(int a)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class Goo
            {
                public Goo(int a) { }
                public Goo() : this($$
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestInvocationOnTriggerParensWithNonEmptyArgumentList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Foo()", string.Empty, null, currentParameterIndex: 0),
        };

        await TestAsync("""
            class Foo
            {
                public Foo(int a) : this($$
                public Foo() { }
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestInvocationOnTriggerComma()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
            class Goo
            {
                public Goo(int a, int b) { }
                public Goo() : this(2,$$
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2579")]
    public async Task TestInvocationOnTriggerCommaWithNonEmptyArgumentList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Foo()", string.Empty, null, currentParameterIndex: 0),
        };

        await TestAsync("""
            class Foo
            {
                public Foo(int a, int b) : this($$
                public Foo() { }
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestNoInvocationOnSpace()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>();
        await TestAsync("""
            class Goo
            {
                public Goo(int a, int b) { }
                public Goo() : this(2, $$
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
    public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateAlways()
    {
        var markup = """
            class DerivedClass : BaseClass
            {
                public DerivedClass() : base($$
            }
            """;

        var referencedCode = """
            public class BaseClass
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public BaseClass(int x)
                { }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateNever()
    {
        var markup = """
            class DerivedClass : BaseClass
            {
                public DerivedClass() : base($$
            }
            """;

        var referencedCode = """
            public class BaseClass
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public BaseClass(int x)
                { }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: [],
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateAdvanced()
    {
        var markup = """
            class DerivedClass : BaseClass
            {
                public DerivedClass() : base($$
            }
            """;

        var referencedCode = """
            public class BaseClass
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
                public BaseClass(int x)
                { }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
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
    public async Task EditorBrowsable_ConstructorInitializer_BrowsableStateMixed()
    {
        var markup = """
            class DerivedClass : BaseClass
            {
                public DerivedClass() : base($$
            }
            """;

        var referencedCode = """
            public class BaseClass
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public BaseClass(int x)
                { }

                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public BaseClass(int x, int y)
                { }
            }
            """;
        var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>
        {
            new("BaseClass(int x)", string.Empty, string.Empty, currentParameterIndex: 0),
            new("BaseClass(int x, int y)", string.Empty, string.Empty, currentParameterIndex: 0)
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
            Secret(int secret)

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
            Secret(int secret)

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
            // goo($$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082601")]
    public async Task TestInvocationWithBadParameterList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>();
        await TestAsync("""
            class BaseClass
            {
                public BaseClass() { }
            }

            class Derived : BaseClass
            {
                public Derived() [|: base{$$|])
                { }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("D(object o)", currentParameterIndex: 0)
        };

        await TestAsync("""
            class D { public D(object o) {} }
            class C : D
            {
                public C() [|: base(($$)
                |]{}
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("D(object o)", currentParameterIndex: 0)
        };

        await TestAsync("""
            class D { public D(object o) {} }
            class C : D
            {
                public C() [|: base((1,$$) |]{}
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss3()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("D(object o)", currentParameterIndex: 0)
        };

        await TestAsync("""
            class D { public D(object o) {} }
            class C : D
            {
                public C() [|: base((1, ($$)
                |]{}
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss4()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("D(object o)", currentParameterIndex: 0)
        };

        await TestAsync("""
            class D { public D(object o) {} }
            class C : D
            {
                public C() [|: base((1, (2,$$) |]{}
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Theory]
    [InlineData("1$$", 0)]
    [InlineData(",$$", 1)]
    [InlineData(",$$,", 1)]
    [InlineData(",,$$", 2)]
    [InlineData("i2: 1, $$,", 0)]
    [InlineData("i2: 1, i1: $$,", 0)]
    [InlineData("i2: 1, $$, i1: 2", 2)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_NamesAndEmptyPositions(string arguments, int expectedParameterIndex)
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Program(int i1, int i2, int i3)", currentParameterIndex: expectedParameterIndex, isSelected: true),
        };

        await TestAsync($$"""
            class Program
            {
                Program() [|: this({{arguments}}|])
                {
                }
                Program(int i1, int i2, int i3) { }
            }
            """, expectedOrderedItems);
    }

    [Theory]
    [InlineData("1$$", 0, 0)]
    [InlineData("1$$, ", 0, 0)]
    [InlineData("1, $$", 1, 0)]
    [InlineData("s: $$", 1, 0)]
    [InlineData("s: string.Empty$$", 1, 0)]
    [InlineData("s: string.Empty$$, ", 1, 0)]
    [InlineData("s: string.Empty, $$", 0, 0)]
    [InlineData("string.Empty$$", 0, 1)]
    [InlineData("string.Empty$$, ", 0, 1)]
    [InlineData("string.Empty,$$", 1, 1)]
    [InlineData("$$, ", 0, 0)]
    [InlineData(",$$", 1, 0)]
    [InlineData("$$, s: string.Empty", 0, 0)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_Incomplete(string arguments, int expectedParameterIndex, int expecteSelectedIndex)
    {
        var index = 0;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("Program(int i, string s)", currentParameterIndex: expectedParameterIndex, isSelected: expecteSelectedIndex == index++),
            new("Program(string s, string s2)", currentParameterIndex: expectedParameterIndex, isSelected: expecteSelectedIndex == index++),
        };

        await TestAsync($$"""
            class Program
            {
                Program() [|: this({{arguments}}|])
                {
                }
                Program(int i, string s) { }
                Program(string s, string s2) { }
            }
            """, expectedOrderedItems);
    }
}
