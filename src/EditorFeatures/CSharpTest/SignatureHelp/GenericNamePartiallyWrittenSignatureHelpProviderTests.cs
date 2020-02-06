﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class GenericNamePartiallyWrittenSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public GenericNamePartiallyWrittenSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override ISignatureHelpProvider CreateSignatureHelpProvider()
        {
            return new GenericNamePartiallyWrittenSignatureHelpProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task NestedGenericUnterminated()
        {
            var markup = @"
class G<T> { };

class C
{
    void Goo()
    {
        G<G<int>$$
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("G<T>", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(544088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544088")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task DeclaringGenericTypeWith1ParameterUnterminated()
        {
            var markup = @"
class G<T> { };

class C
{
    void Goo()
    {
        [|G<$$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("G<T>", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task CallingGenericAsyncMethod()
        {
            var markup = @"
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
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.awaitable}) Task<int> Program.Goo<T>()", methodDocumentation: string.Empty, string.Empty, currentParameterIndex: 0));

            // TODO: Enable the script case when we have support for extension methods in scripts
            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: false, sourceCodeKind: Microsoft.CodeAnalysis.SourceCodeKind.Regular);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericMethod_BrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().Goo<$$
    }
}
";

            var referencedCode = @"
public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public void Goo<T>(T x)
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                       referencedCode: referencedCode,
                                                       expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                       sourceLanguage: LanguageNames.CSharp,
                                                       referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericMethod_BrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().Goo<$$
    }
}
";

            var referencedCode = @"
public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo<T>(T x)
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                       referencedCode: referencedCode,
                                                       expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                       expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                       sourceLanguage: LanguageNames.CSharp,
                                                       referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericMethod_BrowsableAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().Goo<$$
    }
}
";

            var referencedCode = @"
public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public void Goo<T>(T x)
    { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                       referencedCode: referencedCode,
                                                       expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                       sourceLanguage: LanguageNames.CSharp,
                                                       referencedLanguage: LanguageNames.CSharp,
                                                       hideAdvancedMembers: false);

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                       referencedCode: referencedCode,
                                                       expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                       expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                       sourceLanguage: LanguageNames.CSharp,
                                                       referencedLanguage: LanguageNames.CSharp,
                                                       hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericMethod_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().Goo<$$
    }
}
";

            var referencedCode = @"
public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public void Goo<T>(T x)
    { }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo<T, U>(T x, U y)
    { }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C.Goo<T>(T x)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C.Goo<T, U>(T x, U y)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                       referencedCode: referencedCode,
                                                       expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                       expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                       sourceLanguage: LanguageNames.CSharp,
                                                       referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task GenericExtensionMethod()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void IGoo.Bar<T>()", currentParameterIndex: 0),
                new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void IGoo.Bar<T1, T2>()", currentParameterIndex: 0),
            };

            // Extension methods are supported in Interactive/Script (yet).
            await TestAsync(markup, expectedOrderedItems, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(544088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544088")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InvokingGenericMethodWith1ParameterUnterminated()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Method Goo
    /// </summary>
    /// <typeparam name=""T"">Method type parameter</typeparam>
    void Goo<T>() { }

    void Bar()
    {
        [|Goo<$$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo<T>()",
                    "Method Goo", "Method type parameter", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerBracket()
        {
            var markup = @"
class G<S, T> { };

class C
{
    void Goo()
    {
        [|G<$$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("G<S, T>", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerComma()
        {
            var markup = @"
class G<S, T> { };

class C
{
    void Goo()
    {
        [|G<int,$$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("G<S, T>", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [WorkItem(1067933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InvokedWithNoToken()
        {
            var markup = @"
// goo<$$";

            await TestAsync(markup);
        }
    }
}
