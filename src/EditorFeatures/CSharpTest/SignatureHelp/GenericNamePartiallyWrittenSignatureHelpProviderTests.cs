// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class GenericNamePartiallyWrittenSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(GenericNamePartiallyWrittenSignatureHelpProvider);

    [Fact]
    public async Task NestedGenericUnterminated()
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
                    G<G<int>$$
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task NestedGenericUnterminatedWithAmbiguousShift()
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
                    var x = G<G<G<int>>$$>

                    x = x;
                }
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task NestedGenericUnterminatedWithAmbiguousUnsignedShift()
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
                    var x = G<G<G<G<int>>>$$>

                    x = x;
                }
            }
            """, expectedOrderedItems);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544088")]
    public async Task DeclaringGenericTypeWith1ParameterUnterminated()
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
                    [|G<$$
                |]}
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task CallingGenericAsyncMethod()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new($"({CSharpFeaturesResources.awaitable}) Task<int> Program.Goo<T>()", methodDocumentation: string.Empty, string.Empty, currentParameterIndex: 0)
        };

        // TODO: Enable the script case when we have support for extension methods in scripts
        await TestAsync("""
            using System.Threading.Tasks;
            class Program
            {
                void Main(string[] args)
                {
                    Goo<$$
                }
                Task<int> Goo<T>()
                {
                    return Goo<T>();
                }
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: false, sourceCodeKind: Microsoft.CodeAnalysis.SourceCodeKind.Regular);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericMethod_BrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().Goo<$$
                }
            }
            """;

        var referencedCode = """
            public class C
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public void Goo<T>(T x)
                { }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                   referencedCode: referencedCode,
                                                   expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                   expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                   sourceLanguage: LanguageNames.CSharp,
                                                   referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericMethod_BrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().Goo<$$
                }
            }
            """;

        var referencedCode = """
            public class C
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo<T>(T x)
                { }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                   referencedCode: referencedCode,
                                                   expectedOrderedItemsMetadataReference: [],
                                                   expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                   sourceLanguage: LanguageNames.CSharp,
                                                   referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericMethod_BrowsableAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().Goo<$$
                }
            }
            """;

        var referencedCode = """
            public class C
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
                public void Goo<T>(T x)
                { }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                   referencedCode: referencedCode,
                                                   expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                   expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                   sourceLanguage: LanguageNames.CSharp,
                                                   referencedLanguage: LanguageNames.CSharp,
                                                   hideAdvancedMembers: false);

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                   referencedCode: referencedCode,
                                                   expectedOrderedItemsMetadataReference: [],
                                                   expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                   sourceLanguage: LanguageNames.CSharp,
                                                   referencedLanguage: LanguageNames.CSharp,
                                                   hideAdvancedMembers: true);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericMethod_BrowsableMixed()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().Goo<$$
                }
            }
            """;

        var referencedCode = """
            public class C
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public void Goo<T>(T x)
                { }

                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo<T, U>(T x, U y)
                { }
            }
            """;
        var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0),
            new("void C.Goo<T, U>(T x, U y)", string.Empty, string.Empty, currentParameterIndex: 0)
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                   referencedCode: referencedCode,
                                                   expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                   expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                   sourceLanguage: LanguageNames.CSharp,
                                                   referencedLanguage: LanguageNames.CSharp);
    }

    [Fact]
    public async Task GenericExtensionMethod()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void IGoo.Bar<T>()", currentParameterIndex: 0),
            new($"({CSharpFeaturesResources.extension}) void IGoo.Bar<T1, T2>()", currentParameterIndex: 0),
        };

        // Extension methods are supported in Interactive/Script (yet).
        await TestAsync("""
            interface IGoo
            {
                void Bar<T>();
            }

            static class GooExtensions
            {
                public static void Bar<T1, T2>(this IGoo goo) { }
            }

            class Program
            {
                static void Main()
                {
                    IGoo f = null;
                    f.[|Bar<$$
                |]}
            }
            """, expectedOrderedItems, sourceCodeKind: SourceCodeKind.Regular);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544088")]
    public async Task InvokingGenericMethodWith1ParameterUnterminated()
    {
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new("void C.Goo<T>()",
                "Method Goo", "Method type parameter", currentParameterIndex: 0)
        };

        await TestAsync("""
            class C
            {
                /// <summary>
                /// Method Goo
                /// </summary>
                /// <typeparam name="T">Method type parameter</typeparam>
                void Goo<T>() { }

                void Bar()
                {
                    [|Goo<$$
                |]}
            }
            """, expectedOrderedItems);
    }

    [Fact]
    public async Task TestInvocationOnTriggerBracket()
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
                    [|G<$$
                |]}
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestInvocationOnTriggerComma()
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
                    [|G<int,$$
                |]}
            }
            """, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
    public Task InvokedWithNoToken()
        => TestAsync("""
            // goo<$$
            """);
}
