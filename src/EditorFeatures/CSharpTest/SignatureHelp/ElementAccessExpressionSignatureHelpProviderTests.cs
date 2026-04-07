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
public sealed class ElementAccessExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(ElementAccessExpressionSignatureHelpProvider);

    #region "Regular tests"

    [Fact]
    public async Task TestInvocationWithParametersOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[$$|]];
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24311")]
    public async Task TestInvocationWithParametersOn1_WithRefReturn()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("ref int C[int a]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class C
            {
                public ref int this[int a]
                {
                    get { throw null; }
                }
                void Goo(C c)
                {
                    [|c[$$]|]
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24311")]
    public async Task TestInvocationWithParametersOn1_WithRefReadonlyReturn()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("ref readonly int C[int a]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class C
            {
                public ref readonly int this[int a]
                {
                    get { throw null; }
                }
                void Goo(C c)
                {
                    [|c[$$]|]
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636117")]
    public async Task TestInvocationOnExpression()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
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
                void Goo()
                {
                    C[] c = new C[1];
                    c[0][$$
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlCommentsOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a]", "Summary for this.", "Param a", currentParameterIndex: 0)
        };

        await TestAsync("""
            class C
            {
                /// <summary>
                /// Summary for this.
                /// </summary>
                /// <param name="a">Param a</param>
                public string this[int a]
                {
                    get { return null; }
                    set { }
                }
            }

            class D
            {
                void Goo()
                {
                    var c = new C();
                    var x = [|c[$$|]];
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a, bool b]", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[22, $$|]];
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlComentsOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a, bool b]", "Summary for this.", "Param b", currentParameterIndex: 1)
        };

        await TestAsync("""
            class C
            {
                /// <summary>
                /// Summary for this.
                /// </summary>
                /// <param name="a">Param a</param>
                /// <param name="b">Param b</param>
                public string this[int a, bool b]
                {
                    get { return null; }
                    set { }
                }
            }

            class D
            {
                void Goo()
                {
                    var c = new C();
                    var x = [|c[22, $$|]];
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithoutClosingBracketWithParameters()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[$$
                |]}
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationWithoutClosingBracketWithParametersOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a, bool b]", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[22, $$
                |]}
            }
            """, expectedOrderedItems);
    }

    #endregion

    #region "Current Parameter Name"

    [Fact]
    public Task TestCurrentParameterName()
        => VerifyCurrentParameterNameAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[b: false, a: $$42|]];
                }
            }
            """, "a");

    #endregion

    #region "Trigger tests"

    [Fact]
    public async Task TestInvocationOnTriggerBracket()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[$$|]];
                }
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestInvocationOnTriggerComma()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("string C[int a, bool b]", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[42,$$|]];
                }
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestNoInvocationOnSpace()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>();
        await TestAsync("""
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
                void Goo()
                {
                    var c = new C();
                    var x = [|c[42, $$|]];
                }
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public void TestTriggerCharacters()
    {
        char[] expectedCharacters = [',', '['];
        char[] unexpectedCharacters = [' ', '(', '<'];

        VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
    }

    #endregion

    #region "EditorBrowsable tests"

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Indexer_PropertyAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo()[$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public int this[int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("int Goo[int x]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Indexer_PropertyNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo()[$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public int this[int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """;
        var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>
        {
            new("int Goo[int x]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: [],
                                            expectedOrderedItemsSameSolution: expectedOrderedItemsMetadataReference,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Indexer_PropertyAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo()[$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                public int this[int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("int Goo[int x]", string.Empty, string.Empty, currentParameterIndex: 0)
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
    public async Task EditorBrowsable_Indexer_PropertyNeverOnOneOfTwoOverloads()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo()[$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
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
            }
            """;

        var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>
        {
            new("int Goo[double d]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>
        {
            new("int Goo[double d]", string.Empty, string.Empty, currentParameterIndex: 0),
            new("int Goo[int x]", string.Empty, string.Empty, currentParameterIndex: 0),
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                            expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Indexer_GetBrowsableNeverIgnored()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo()[$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                public int this[int x]
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    get { return 5; }
                    set { }
                }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("int Goo[int x]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Indexer_SetBrowsableNeverIgnored()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo()[$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                public int this[int x]
                {
                    get { return 5; }
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    set { }
                }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("int Goo[int x]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Indexer_GetSetBrowsableNeverIgnored()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo()[$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                public int this[int x]
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    get { return 5; }
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    set { }
                }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("int Goo[int x]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                            referencedCode: referencedCode,
                                            expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                            expectedOrderedItemsSameSolution: expectedOrderedItems,
                                            sourceLanguage: LanguageNames.CSharp,
                                            referencedLanguage: LanguageNames.CSharp);
    }

    #endregion

    #region Indexed Property tests

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530811")]
    public async Task IndexedProperty()
    {
        var markup = """
            class Program
            {
                void M()
                {
                        CCC c = new CCC();
                        c.IndexProp[$$
                }
            }
            """;

        // Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
        var referencedCode = """
            Imports System.Runtime.InteropServices

            <ComImport()>
            <GuidAttribute(CCC.ClassId)>
            Public Class CCC

            #Region "COM GUIDs"
                Public Const ClassId As String = "9d965fd2-1514-44f6-accd-257ce77c46b0"
                Public Const InterfaceId As String = "a9415060-fdf0-47e3-bc80-9c18f7f39cf6"
                Public Const EventsId As String = "c6a866a5-5f97-4b53-a5df-3739dc8ff1bb"
            # End Region

                        ''' <summary>
                ''' An index property from VB
                ''' </summary>
                ''' <param name="p1">p1 is an integer index</param>
                ''' <returns>A string</returns>
                Public Property IndexProp(ByVal p1 As Integer) As String
                    Get
                        Return Nothing
                    End Get
                    Set(ByVal value As String)

                    End Set
                End Property
            End Class
            """;

        var metadataItems = new List<SignatureHelpTestItem>
        {
            new("string CCC.IndexProp[int p1]", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        var projectReferenceItems = new List<SignatureHelpTestItem>
        {
            new("string CCC.IndexProp[int p1]", "An index property from VB", "p1 is an integer index", currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                           referencedCode: referencedCode,
                                           expectedOrderedItemsMetadataReference: metadataItems,
                                           expectedOrderedItemsSameSolution: projectReferenceItems,
                                           sourceLanguage: LanguageNames.CSharp,
                                           referencedLanguage: LanguageNames.VisualBasic);
    }

    #endregion

    [Fact]
    public async Task FieldUnavailableInOneLinkedFile()
    {
        var expectedDescription = new SignatureHelpTestItem($"""
            int C[int z]

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
                public int this[int z]
                {
                    get
                    {
                        return 0;
                    }
                }
            #endif
                void goo()
                {
                    var x = this[$$
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
            int C[int z]

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
                public int this[int z]
                {
                    get
                    {
                        return 0;
                    }
                }
            #endif

            #if BAR
                void goo()
                {
                    var x = this[$$
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

    [Trait(Traits.Feature, Traits.Features.SignatureHelp)]
    public sealed class IncompleteElementAccessExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        internal override Type GetSignatureHelpProviderType()
            => typeof(ElementAccessExpressionSignatureHelpProvider);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636117")]
        public async Task TestInvocation()
        {
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new("string C[int a]", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync("""
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
                    void Goo()
                    {
                        var c = new C();
                        c[$$]
                    }
                }
                """, expectedOrderedItems);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939417")]
        public async Task ConditionalIndexer()
        {
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new("int P[int z]", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync("""
                public class P
                {
                    public int this[int z]
                    {
                        get
                        {
                            return 0;
                        }
                    }

                    public void goo()
                    {
                        P p = null;
                        p?[$$]
                    }
                }
                """, expectedOrderedItems);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32")]
        public async Task NonIdentifierConditionalIndexer()
        {
            var expected = new[] { new SignatureHelpTestItem("char string[int index]") };
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        ""?[$$ }
                }
                """, expected); // inline with a string literal
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        ""?[/**/$$ }
                }
                """, expected); // inline with a string literal and multiline comment
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        ("")?[$$ }
                }
                """, expected); // parenthesized expression
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        new System.String(' ', 1)?[$$ }
                }
                """, expected); // new object expression

            // more complicated parenthesized expression
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        (null as System.Collections.Generic.List<int>)?[$$ }
                }
                """, [new SignatureHelpTestItem("int System.Collections.Generic.List<int>[int index]")]);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
        public Task InvokedWithNoToken()
            => TestAsync("""
                // goo[$$
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2482")]
        public Task WhereExpressionLooksLikeArrayTypeSyntaxOfQualifiedName()
            => TestAsync("""
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
                """, [new SignatureHelpTestItem("int WithIndexer[int index]")], usePreviousCharAsTrigger: true);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20507")]
        public Task InConditionalIndexingFollowedByMemberAccess()
            => TestAsync("""
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
                """, [new SignatureHelpTestItem("Indexable Indexable[int x]")], usePreviousCharAsTrigger: false);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20507")]
        public Task InConditionalIndexingFollowedByConditionalAccess()
            => TestAsync("""
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
                """, [new SignatureHelpTestItem("Indexable Indexable[int x]")], usePreviousCharAsTrigger: false);
    }
}
