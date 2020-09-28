// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Test.Utilities;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Numerics;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class IndexerComplitionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public IndexerComplitionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override Type GetCompletionProviderType()
            => typeof(IndexerCompletionProvider);

        protected override string? ItemPartiallyWritten(string? expectedItemOrNull) =>
            expectedItemOrNull switch
            {
                { Length: >= 2 } s when s.StartsWith("[") => expectedItemOrNull.Substring(1, 1),
                _ => base.ItemPartiallyWritten(expectedItemOrNull)
            };

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync(@"
public class C
{
    public int this[int i] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "this[]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerSuggestionCommitsOpenAndClosingBraces()
        {
            await VerifyCustomCommitProviderAsync(@"
public class C
{
    public int this[int i] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "this[]", @"
public class C
{
    public int this[int i] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c[$$]
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerWithTwoParametersSuggestionCommitsOpenAndClosingBraces()
        {
            await VerifyCustomCommitProviderAsync(@"
public class C
{
    public int this[int x, int y] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "this[]", @"
public class C
{
    public int this[int x, int y] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c[$$]
    }
}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", "c[$$]")]
        [InlineData("c. $$", "c[$$] ")]
        [InlineData("c.$$;", "c[$$];")]
        [InlineData("c.th$$", "c[$$]")]
        [InlineData("c.this$$", "c[$$]")]
        [InlineData("c.th$$;", "c[$$];")]
        [InlineData("var f = c.$$;", "var f = c[$$];")]
        [InlineData("var f = c.th$$;", "var f = c[$$];")]
        [InlineData("c?.$$", "c?[$$]")]
        [InlineData("c?.this$$", "c?[$$]")]
        [InlineData("((C)c).$$", "((C)c)[$$]")]
        [InlineData("(true ? c : c).$$", "(true ? c : c)[$$]")]
        public async Task IndexerCompletionForDifferentExpressions(string expression, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {expression}
    }}
}}
", "this[]", @$"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {fixedCode}
    }}
}}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerOverloadsAreEncodedInSymbolsProperty()
        {
            var completionItems = await GetCompletionItemsAsync(@"
public class C
{
    public int this[int i] => i;
    public int this[string s] => 1;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", SourceCodeKind.Regular);
            Assert.Equal(1, completionItems.Length);
            var indexerCompletionItem = completionItems.Single();
            Assert.Equal("this[]", indexerCompletionItem.DisplayText);
            Assert.True(indexerCompletionItem.Properties.TryGetValue("Symbols", out var symbols));
            var symbolSplitted = symbols!.Split('|');
            Assert.Equal(2, symbolSplitted.Length);
        }
    }
}
