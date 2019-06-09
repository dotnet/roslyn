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
    public class ExplicitInterfaceTypeCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ExplicitInterfaceTypeCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture)
            : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
            => new ExplicitInterfaceTypeCompletionProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestAtStartOfClass()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    int $$
}
";
            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(459044, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=459044")]
        public async Task TestInMisplacedUsing()
        {
            var markup = @"
class C
{
    using ($$)
}
";
            await VerifyNoItemsExistAsync(markup); // no crash
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestAtStartOfStruct()
        {
            var markup = @"
using System.Collections;

struct C : IList
{
    int $$
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestAfterField()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    int i;
    int $$
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestAfterMethod_01()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    void Goo() { }
    int $$
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestAfterMethod_02()
        {
            var markup = @"
using System.Collections;

interface C : IList
{
    void Goo() { }
    int $$
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestAfterExpressionBody()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    int Goo() => 0;
    int $$
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestWithAttributeFollowing()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    int Goo() => 0;
    int $$

    [Attr]
    int Bar();
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestWithModifierFollowing()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    int Goo() => 0;
    int $$

    public int Bar();
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestWithTypeFollowing()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    int Goo() => 0;
    int $$

    int Bar();
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestWithTypeFollowing2()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    int Goo() => 0;
    int $$

    X Bar();
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInMember()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    void Goo()
    {
        int $$
    }
}
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotWithAccessibility()
        {
            var markup = @"
using System.Collections;

class C : IList
{
    public int $$
}
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInInterface()
        {
            var markup = @"
using System.Collections;

interface I : IList
{
    int $$
}
";

            await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
            await VerifyItemExistsAsync(markup, "IEnumerable");
            await VerifyItemExistsAsync(markup, "ICollection");
            await VerifyItemExistsAsync(markup, "IList");
        }
    }
}
