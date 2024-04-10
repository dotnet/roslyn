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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    [Trait(Traits.Feature, Traits.Features.SignatureHelp)]
    public class ObjectCreationExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        internal override Type GetSignatureHelpProviderType()
            => typeof(ObjectCreationExpressionSignatureHelpProvider);

        #region "Regular tests"

        [Fact]
        public async Task TestInvocationWithoutParameters()
        {
            var markup = """
                class C
                {
                    void goo()
                    {
                        var c = [|new C($$|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C()", string.Empty, null, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestImplicitInvocationWithoutParameters()
        {
            var markup = """
                <Workspace>
                    <Project Language="C#" LanguageVersion="Preview" CommonReferences="true">
                        <Document FilePath="SourceDocument"><![CDATA[
                class C
                {
                    void M()
                    {
                        C c = [|new($$|]);
                    }
                }
                        </Document>
                    </Project>
                </Workspace>
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C()", string.Empty, null, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithoutParametersMethodXmlComments()
        {
            var markup = """
                class C
                {
                    /// <summary>
                    /// Summary for C
                    /// </summary>
                    C() { }

                    void Goo()
                    {
                        C c = [|new C($$|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C()", "Summary for C", null, currentParameterIndex: 0)
            };
            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithParametersOn1()
        {
            var markup = """
                class C
                {
                    C(int a, int b) { }

                    void Goo()
                    {
                        C c = [|new C($$2, 3|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestImplicitInvocationWithParametersOn1()
        {
            var markup = """
                class C
                {
                    C(int a, int b) { }

                    void M()
                    {
                        C c = [|new($$2, 3|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithParametersXmlCommentsOn1()
        {
            var markup = """
                class C
                {
                    /// <summary>
                    /// Summary for C
                    /// </summary>
                    /// <param name="a">Param a</param>
                    /// <param name="b">Param b</param>
                    C(int a, int b) { }

                    void Goo()
                    {
                        C c = [|new C($$2, 3|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, int b)", "Summary for C", "Param a", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithParametersOn2()
        {
            var markup = """
                class C
                {
                    C(int a, int b) { }

                    void Goo()
                    {
                        C c = [|new C(2, $$3|]);
                    }
                }
                """;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithParametersXmlComentsOn2()
        {
            var markup = """
                class C
                {
                    /// <summary>
                    /// Summary for C
                    /// </summary>
                    /// <param name="a">Param a</param>
                    /// <param name="b">Param b</param>
                    C(int a, int b) { }

                    void Goo()
                    {
                        C c = [|new C(2, $$3|]);
                    }
                }
                """;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, int b)", "Summary for C", "Param b", currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_PickFirst()
        {
            var markup = """
                class D
                {
                    void M()
                    {
                        [|new D(i: 1$$|]);
                    }
                    D(D filtered) => throw null;
                    D(string i) => throw null;
                    D(int i) => throw null;
                }
                """;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("D(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem("D(string i)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task PickCorrectOverload_PickFirst_ImplicitObjectCreation()
        {
            var markup = """
                class D
                {
                    void M()
                    {
                        D d = [|new(i: 1$$|]);
                    }
                    D(D filtered) => throw null;
                    D(string i) => throw null;
                    D(int i) => throw null;
                }
                """;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("D(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem("D(string i)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_PickSecond()
        {
            var markup = """
                class D
                {
                    void M()
                    {
                        [|new D(i: null$$|]);
                    }
                    D(D filtered) => throw null;
                    D(string i) => throw null;
                    D(int i) => throw null;
                }
                """;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("D(int i)", currentParameterIndex: 0),
                new SignatureHelpTestItem("D(string i)", currentParameterIndex: 0, isSelected: true),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task PickCorrectOverload_PickSecond_ImplicitObjectCreation()
        {
            var markup = """
                class D
                {
                    void M()
                    {
                        D d = [|new(i: null$$|]);
                    }
                    D(D filtered) => throw null;
                    D(string i) => throw null;
                    D(int i) => throw null;
                }
                """;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("D(int i)", currentParameterIndex: 0),
                new SignatureHelpTestItem("D(string i)", currentParameterIndex: 0, isSelected: true),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithoutClosingParen()
        {
            var markup = """
                class C
                {
                    void goo()
                    {
                        var c = [|new C($$
                    |]}
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C()", string.Empty, null, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithoutClosingParenWithParameters()
        {
            var markup = """
                class C
                {
                    C(int a, int b) { }

                    void Goo()
                    {
                        C c = [|new C($$2, 3
                    |]}
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationWithoutClosingParenWithParametersOn2()
        {
            var markup = """
                class C
                {
                    C(int a, int b) { }

                    void Goo()
                    {
                        C c = [|new C(2, $$3
                    |]}
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task TestInvocationOnLambda()
        {
            var markup = """
                using System;

                class C
                {
                    void goo()
                    {
                        var bar = [|new Action<int, int>($$
                    |]}
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Action<int, int>(void (int, int) target)", string.Empty, string.Empty, currentParameterIndex: 0, isSelected: true)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        #endregion

        #region "Current Parameter Name"

        [Fact]
        public async Task TestCurrentParameterName()
        {
            var markup = """
                class C
                {
                    C(int a, string b)
                    {
                    }

                    void goo()
                    {
                        var c = [|new C(b: string.Empty, $$a: 2|]);
                    }
                }
                """;

            await VerifyCurrentParameterNameAsync(markup, "a");
        }

        #endregion

        #region "Trigger tests"

        [Fact]
        public async Task TestInvocationOnTriggerParens()
        {
            var markup = """
                class C
                {
                    void goo()
                    {
                        var c = [|new C($$|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C()", string.Empty, null, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact]
        public async Task TestInvocationOnTriggerComma()
        {
            var markup = """
                class C
                {
                    C(int a, string b)
                    {
                    }

                    void goo()
                    {
                        var c = [|new C(2,$$string.Empty|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(int a, string b)", string.Empty, string.Empty, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact]
        public async Task TestNoInvocationOnSpace()
        {
            var markup = """
                class C
                {
                    C(int a, string b)
                    {
                    }

                    void goo()
                    {
                        var c = [|new C(2, $$string.Empty|]);
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
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
        public async Task EditorBrowsable_Constructor_BrowsableAlways()
        {
            var markup = """
                class Program
                {
                    void M()
                    {
                        new Goo($$
                    }
                }
                """;

            var referencedCode = """
                public class Goo
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                    public Goo(int x)
                    {
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
        public async Task EditorBrowsable_Constructor_BrowsableNever()
        {
            var markup = """
                class Program
                {
                    void M()
                    {
                        new Goo($$
                    }
                }
                """;

            var referencedCode = """
                public class Goo
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    public Goo(int x)
                    {
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
        public async Task EditorBrowsable_Constructor_BrowsableAdvanced()
        {
            var markup = """
                class Program
                {
                    void M()
                    {
                        new Goo($$
                    }
                }
                """;

            var referencedCode = """
                public class Goo
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                    public Goo()
                    {
                    }
                }
                """;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo()", string.Empty, null, currentParameterIndex: 0)
            };

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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
        public async Task EditorBrowsable_Constructor_BrowsableMixed()
        {
            var markup = """
                class Program
                {
                    void M()
                    {
                        new Goo($$
                    }
                }
                """;

            var referencedCode = """
                public class Goo
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                    public Goo(int x)
                    {
                    }
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    public Goo(long y)
                    {
                    }
                }
                """;
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int x)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Goo(int x)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("Goo(long y)", string.Empty, string.Empty, currentParameterIndex: 0)
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
            var markup = """
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                        <Document FilePath="SourceDocument"><![CDATA[
                class C
                {
                #if GOO
                    class D
                    {
                    }
                #endif
                    void goo()
                    {
                        var x = new D($$
                    }
                }
                ]]>
                        </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                        <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                    </Project>
                </Workspace>
                """;
            var expectedDescription = new SignatureHelpTestItem($"D()\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [Fact]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = """
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO,BAR">
                        <Document FilePath="SourceDocument"><![CDATA[
                class C
                {
                #if GOO
                    class D
                    {
                    }
                #endif

                #if BAR
                    void goo()
                    {
                        var x = new D($$
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
                """;

            var expectedDescription = new SignatureHelpTestItem($"D()\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
        public async Task InvokedWithNoToken()
        {
            var markup = """
                // new goo($$
                """;

            await TestAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078993")]
        public async Task TestSigHelpInIncorrectObjectCreationExpression()
        {
            var markup = """
                class C
                {
                    void goo(C c)
                    {
                        goo([|new C{$$|]
                    }
                }
                """;

            await TestAsync(markup);
        }

        [Fact]
        public async Task TypingTupleDoesNotDismiss1()
        {
            var markup = """
                class C
                {
                    public C(object o) { }
                    public C M()
                    {
                        return [|new C(($$)
                    |]}

                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(object o)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact]
        public async Task TypingTupleDoesNotDismiss2()
        {
            var markup = """
                class C
                {
                    public C(object o) { }
                    public C M()
                    {
                        return [|new C((1,$$)
                    |]}

                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(object o)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact]
        public async Task TypingTupleDoesNotDismiss3()
        {
            var markup = """
                class C
                {
                    public C(object o) { }
                    public C M()
                    {
                        return [|new C((1, ($$)
                    |]}

                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(object o)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact]
        public async Task TypingTupleDoesNotDismiss4()
        {
            var markup = """
                class C
                {
                    public C(object o) { }
                    public C M()
                    {
                        return [|new C((1, (2,$$)
                    |]}

                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("C(object o)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
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
            var markup = """
                class Program
                {
                    static void M()
                    {
                        [|new Program(ARGUMENTS|]);
                    }
                    Program(int i, string s) { }
                    Program(string s, string s2) { }
                }
                """;

            var index = 0;
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("Program(int i, string s)", currentParameterIndex: expectedParameterIndex, isSelected: expecteSelectedIndex == index++),
                new SignatureHelpTestItem("Program(string s, string s2)", currentParameterIndex: expectedParameterIndex, isSelected: expecteSelectedIndex == index++),
            };

            await TestAsync(markup.Replace("ARGUMENTS", arguments), expectedOrderedItems);
        }

        [Theory]
        [InlineData("s2: $$", 1)]
        [InlineData("s2: string.Empty$$", 1)]
        [InlineData("s2: string.Empty$$,", 1)]
        [InlineData("s2: string.Empty,$$", 0)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
        public async Task PickCorrectOverload_Incomplete_WithNames(string arguments, int expectedParameterIndex)
        {
            var markup = """
                class Program
                {
                    void M()
                    {
                        [|new Program(ARGUMENTS|]);
                    }
                    Program(int i, string s) { }
                    Program(string s, string s2) { }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem($"Program(string s, string s2)", currentParameterIndex: expectedParameterIndex, isSelected: true),
            };

            await TestAsync(markup.Replace("ARGUMENTS", arguments), expectedOrderedItems);
        }
    }
}
