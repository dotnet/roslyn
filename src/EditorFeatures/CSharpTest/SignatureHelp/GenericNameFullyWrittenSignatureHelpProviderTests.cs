// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class GenericNameFullyWrittenSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(GenericNameFullyWrittenSignatureHelpProvider);

    #region "Declaring generic type objects"

    [Fact]
    public async Task NestedGenericTerminated()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<T>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<T> { };

            class C
            {
                void Goo()
                {
                    G<G<int>$$>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWith1ParameterTerminated()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<T>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<T> { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWith2ParametersOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<S, T> { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWith2ParametersOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T>", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
            class G<S, T> { };

            class C
            {
                void Goo()
                {
                    [|G<int, $$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWith2ParametersOn1XmlDoc()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T>",
                "Summary for G",
                "TypeParamS. Also see T",
                currentParameterIndex: 0)
        };

        await TestAsync("""
            /// <summary>
            /// Summary for G
            /// </summary>
            /// <typeparam name="S">TypeParamS. Also see <see cref="T"/></typeparam>
            /// <typeparam name="T">TypeParamT</typeparam>
            class G<S, T> { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWith2ParametersOn2XmlDoc()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T>", "Summary for G", "TypeParamT. Also see S", currentParameterIndex: 1)
        };

        await TestAsync("""
            /// <summary>
            /// Summary for G
            /// </summary>
            /// <typeparam name="S">TypeParamS</typeparam>
            /// <typeparam name="T">TypeParamT. Also see <see cref="S"/></typeparam>
            class G<S, T> { };

            class C
            {
                void Goo()
                {
                    [|G<int, $$|]>
                }
            }
            """, expectedOrderedItems);
    }

    #endregion

    #region "Constraints on generic types"

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsStruct()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : struct", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<S> where S : struct
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsClass()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : class", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<S> where S : class
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsNew()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : new()", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<S> where S : new()
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsBase()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : Base", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class Base { }

            class G<S> where S : Base
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsBaseGenericWithGeneric()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : Base<S>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class Base<T> { }

            class G<S> where S : Base<S>
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsBaseGenericWithNonGeneric()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : Base<int>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class Base<T> { }

            class G<S> where S : Base<int>
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsBaseGenericNested()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : Base<Base<int>>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class Base<T> { }

            class G<S> where S : Base<Base<int>>
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsDeriveFromAnotherGenericParameter()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T> where S : T", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<S, T> where S : T
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsMixed1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T> where S : Base, new()", "Summary1", "SummaryS", currentParameterIndex: 0)
        };

        await TestAsync("""
            /// <summary>
            /// Summary1
            /// </summary>
            /// <typeparam name="S">SummaryS</typeparam>
            /// <typeparam name="T">SummaryT</typeparam>
            class G<S, T>
                where S : Base, new()
                where T : class, S, IGoo, new()
            { };

            internal interface IGoo { }

            internal class Base { }

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsMixed2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T> where T : class, S, IGoo, new()", "Summary1", "SummaryT", currentParameterIndex: 1)
        };

        await TestAsync("""
            /// <summary>
            /// Summary1
            /// </summary>
            /// <typeparam name="S">SummaryS</typeparam>
            /// <typeparam name="T">SummaryT</typeparam>
            class G<S, T>
                where S : Base, new()
                where T : class, S, IGoo, new()
            { };

            internal interface IGoo { }

            internal class Base { }

            class C
            {
                void Goo()
                {
                    [|G<bar, $$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task DeclaringGenericTypeWithConstraintsAllowRefStruct()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S> where S : allows ref struct", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class G<S> where S : allows ref struct
            { };

            class C
            {
                void Goo()
                {
                    [|G<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    #endregion

    #region "Generic member invocation"

    [Fact]
    public async Task InvokingGenericMethodWith1ParameterTerminated()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<T>()", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestAsync("""
            class C
            {
                void Goo<T>() { }

                void Bar()
                {
                    [|Goo<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544091")]
    public async Task InvokingGenericMethodWith2ParametersOn1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<S, T>(S s, T t)",
                "Method summary", "type param S. see T", currentParameterIndex: 0)
        };

        await TestAsync("""
            class C
            {
                /// <summary>
                /// Method summary
                /// </summary>
                /// <typeparam name="S" > type param S. see <see cref="T"/> </typeparam>
                /// <typeparam name="T">type param T. </typeparam>
                /// <param name="s">parameter s</param>
                /// <param name="t">parameter t</param>
                void Goo<S, T>(S s, T t) { }

                void Bar()
                {
                    [|Goo<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544091")]
    public async Task InvokingGenericMethodWith2ParametersOn2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<S, T>(S s, T t)", string.Empty, string.Empty, currentParameterIndex: 1)
        };

        await TestAsync("""
            class C
            {
                void Goo<S, T>(S s, T t) { }

                void Bar()
                {
                    [|Goo<int, $$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544091")]
    public async Task InvokingGenericMethodWith2ParametersOn1XmlDoc()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<S, T>(S s, T t)", "SummaryForGoo", "SummaryForS", currentParameterIndex: 0)
        };

        await TestAsync("""
            class C
            {
                /// <summary>
                /// SummaryForGoo
                /// </summary>
                /// <typeparam name="S">SummaryForS</typeparam>
                /// <typeparam name="T">SummaryForT</typeparam>
                void Goo<S, T>(S s, T t) { }

                void Bar()
                {
                    [|Goo<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544091")]
    public async Task InvokingGenericMethodWith2ParametersOn2XmlDoc()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<S, T>(S s, T t)", "SummaryForGoo", "SummaryForT", currentParameterIndex: 1)
        };

        await TestAsync("""
            class C
            {
                /// <summary>
                /// SummaryForGoo
                /// </summary>
                /// <typeparam name="S">SummaryForS</typeparam>
                /// <typeparam name="T">SummaryForT</typeparam>
                void Goo<S, T>(S s, T t) { }

                void Bar()
                {
                    [|Goo<int, $$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task CallingGenericExtensionMethod()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"({CSharpFeaturesResources.extension}) void G.Goo<T>()", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        // TODO: Enable the script case when we have support for extension methods in scripts
        await TestAsync("""
            class G
            { };

            class C
            {
                void Bar()
                {
                    G g = null;
                    g.[|Goo<$$|]>
                }
            }

            static class GooClass
            {
                public static void Goo<T>(this G g) { }
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false, sourceCodeKind: Microsoft.CodeAnalysis.SourceCodeKind.Regular);
    }

    #endregion

    #region "Constraints on generic methods"

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544091")]
    public async Task InvokingGenericMethodWithConstraintsMixed1()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("S C.Goo<S, T>(S s, T t) where S : Base, new()", "GooSummary", "ParamS", currentParameterIndex: 0)
        };

        await TestAsync("""
            class Base { }
            interface IGoo { }

            class C
            {
                /// <summary>
                /// GooSummary
                /// </summary>
                /// <typeparam name="S">ParamS</typeparam>
                /// <typeparam name="T">ParamT</typeparam>
                S Goo<S, T>(S s, T t)
                    where S : Base, new()
                    where T : class, S, IGoo, new()
                { return null; }

                void Bar()
                {
                    [|Goo<$$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544091")]
    public async Task InvokingGenericMethodWithConstraintsMixed2()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("S C.Goo<S, T>(S s, T t) where T : class, S, IGoo, new()", "GooSummary", "ParamT", currentParameterIndex: 1)
        };

        await TestAsync("""
            class Base { }
            interface IGoo { }

            class C
            {
                /// <summary>
                /// GooSummary
                /// </summary>
                /// <typeparam name="S">ParamS</typeparam>
                /// <typeparam name="T">ParamT</typeparam>
                S Goo<S, T>(S s, T t)
                    where S : Base, new()
                    where T : class, S, IGoo, new()
                { return null; }

                void Bar()
                {
                    [|Goo<Base, $$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public Task TestUnmanagedConstraint()
        => TestAsync("""
            class C
            {
                /// <summary>
                /// summary headline
                /// </summary>
                /// <typeparam name="T">T documentation</typeparam>
                void M<T>(T arg) where T : unmanaged
                {
                }

                void Bar()
                {
                    [|M<$$|]>
                }
            }
            """,
        [
            new SignatureHelpTestItem("void C.M<T>(T arg) where T : unmanaged", "summary headline", "T documentation", currentParameterIndex: 0)
        ]);

    #endregion

    #region "Trigger tests"

    [Fact]
    public void TestTriggerCharacters()
    {
        char[] expectedCharacters = [',', '<'];
        char[] unexpectedCharacters = [' ', '[', '('];

        VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
    }

    [Fact]
    public async Task FieldUnavailableInOneLinkedFile()
    {
        var expectedDescription = new SignatureHelpTestItem($"""
            D<T>

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
                class D<T>
                {
                }
            #endif
                void goo()
                {
                    var x = new D<$$
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
            D<T>

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
                class D<T>
                {
                }
            #endif

            #if BAR
                void goo()
                {
                    var x = new D<$$
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

    #endregion

    #region "EditorBrowsable tests"

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType_BrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var c = new C<$$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public class C<T>
            {
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("C<T>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                   referencedCode: referencedCode,
                                                   expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                   expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                   sourceLanguage: LanguageNames.CSharp,
                                                   referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType_BrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var c = new C<$$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public class C<T>
            {
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("C<T>", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                   referencedCode: referencedCode,
                                                   expectedOrderedItemsMetadataReference: [],
                                                   expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                   sourceLanguage: LanguageNames.CSharp,
                                                   referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType_BrowsableAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var c = new C<$$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public class C<T>
            {
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("C<T>", string.Empty, string.Empty, currentParameterIndex: 0)
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
    #endregion

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1083601")]
    public async Task DeclaringGenericTypeWithBadTypeArgumentList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>();
        await TestAsync("""
            class G<T> { };

            class C
            {
                void Goo()
                {
                    G{$$>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50114")]
    public async Task DeclaringGenericTypeWithDocCommentList()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("G<S, T>", """
            List:

            Item 1.
            """,
            classificationTypeNames: ImmutableArray.Create(
                ClassificationTypeNames.Text,
                ClassificationTypeNames.WhiteSpace,
                ClassificationTypeNames.WhiteSpace,
                ClassificationTypeNames.WhiteSpace,
                ClassificationTypeNames.Text,
                ClassificationTypeNames.WhiteSpace))
        };

        await TestAsync("""
            /// <summary>
            /// List:
            /// <list>
            /// <item>
            /// <description>
            /// Item 1.
            /// </description>
            /// </item>
            /// </list>
            /// </summary>
            class G<S, T> { };

            class C
            {
                void Goo()
                {
                    [|G<int, $$|]>
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80233")]
    public Task TestModernGenericExtensionMethodOnGenericType()
        => TestAsync("""
            public class My<T>
            {
            }

            public static class MyExtensions
            {
                extension<TBase>(My<TBase> self)
                {
                    public void GenericMethod<TDerived>()
                    {
                    }
                }
            }

            class User
            {
                static void Main()
                {
                    My<object> target = new();
                    target.GenericMethod<$$>();
                }
            }
            """,
            [new("void MyExtensions.extension<object>(My<object>).GenericMethod<object, TDerived>()")],
            sourceCodeKind: SourceCodeKind.Regular);
}
