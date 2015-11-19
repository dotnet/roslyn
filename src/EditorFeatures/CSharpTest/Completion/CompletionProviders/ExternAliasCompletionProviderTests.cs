// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ExternAliasCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ExternAliasCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new ExternAliasCompletionProvider();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoAliases()
        {
            await VerifyNoItemsExistAsync(@"
extern alias $$
class C
{
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExternAlias()
        {
            var markup = @"
extern alias $$ ";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "foo", "foo", 1, "C#", "C#", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterExternAlias()
        {
            var markup = @"
extern alias foo $$ ";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "foo", "foo", 0, "C#", "C#", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotGlobal()
        {
            var markup = @"
extern alias $$ ";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "foo", "global", 0, "C#", "C#", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfAlreadyUsed()
        {
            var markup = @"
extern alias foo;
extern alias $$";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "foo", "foo", 0, "C#", "C#", false);
        }

        [WorkItem(1075278)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInComment()
        {
            var markup = @"
extern alias // $$ ";
            await VerifyNoItemsExistAsync(markup);
        }
    }
}
