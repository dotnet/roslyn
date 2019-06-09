// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ExternAliasCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ExternAliasCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new ExternAliasCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoAliases()
        {
            await VerifyNoItemsExistAsync(@"
extern alias $$
class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExternAlias()
        {
            var markup = @"
extern alias $$ ";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "goo", "goo", 1, "C#", "C#", false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterExternAlias()
        {
            var markup = @"
extern alias goo $$ ";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "goo", "goo", 0, "C#", "C#", false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotGlobal()
        {
            var markup = @"
extern alias $$ ";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "goo", "global", 0, "C#", "C#", false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfAlreadyUsed()
        {
            var markup = @"
extern alias goo;
extern alias $$";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "goo", "goo", 0, "C#", "C#", false);
        }

        [WorkItem(1075278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075278")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInComment()
        {
            var markup = @"
extern alias // $$ ";
            await VerifyNoItemsExistAsync(markup);
        }
    }
}
