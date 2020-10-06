// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class IndexerCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public IndexerCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override Type GetCompletionProviderType()
            => typeof(IndexerCompletionProvider);

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
", "this", displayTextSuffix: "[]");
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
", "this", @"
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
", "this", @"
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
", "this", @$"
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
        public async Task IndexerDescriptionIncludesDocCommentsAndOverloadsHint()
        {
            await VerifyItemExistsAsync(@"
public class C
{
    /// <summary>
    /// Returns the index <paramref name=""i""/>
    /// </summary>
    /// <param name=""i"">The index</param>
    /// <returns>Returns the index <paramref name=""i""/></returns>
    public int this[int i] => i;

    /// <summary>
    /// Returns 1
    /// </summary>
    /// <param name=""i"">The index</param>
    /// <returns>Returns 1</returns>
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
", "this", displayTextSuffix: "[]", expectedDescriptionOrNull: @$"int C.this[int i] {{ get; }} (+ 1 {FeaturesResources.overload})
Returns the index i");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerOfBaseTypeIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync(@"
public class Base
{
    public int this[int i] => i;
}
public class Derived : Base
{
}

public class Program
{
    public void Main()
    {
        var d = new Derived();
        d.$$
    }
}
", "this", displayTextSuffix: "[]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerOfBaseTypeIsNotSuggestedIfNotAccessible()
        {
            await VerifyNoItemsExistAsync(@"
public class Base
{
    protected int this[int i] => i;
}
public class Derived : Base
{
}

public class Program
{
    public void Main()
    {
        var d = new Derived();
        d.$$
    }
}
");
        }
    }
}
