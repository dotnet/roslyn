// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class OperatorIndexerCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public OperatorIndexerCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {

        }

        internal override Type GetCompletionProviderType()
            => typeof(OperatorIndexerCompletionProvider);

        protected override string ItemPartiallyWritten(string expectedItemOrNull) => "";

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]

        public async Task ExplicitUserDefinedConversionIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "(float)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]

        public async Task ExplicitUserDefinedConversionIsNotSuggestedIfMemberNameIsPartiallyWritten()
        {
            await VerifyItemIsAbsentAsync(@"
public class C
{
    public void fly() { }
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.fl$$
    }
}
", "(float)");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", true)]
        [InlineData("c.fl$$", false)]
        [InlineData("c.($$", false)]
        [InlineData("c$$", false)]
        [InlineData(@"""c.$$", false)]
        [InlineData("c?.$$", true)]
        [InlineData("((C)c).$$", true)]
        [InlineData("(true ? c : c).$$", true)]
        public async Task ExplicitUserDefinedConversionDifferentInvocations(string invocation, bool shouldSuggestConversion)
        {
            Func<string, string, Task> verifyFunc = shouldSuggestConversion
                ? (markup, expectedItem) => VerifyItemExistsAsync(markup, expectedItem)
                : (markup, expectedItem) => VerifyItemIsAbsentAsync(markup, expectedItem);

            await verifyFunc(@$"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {invocation}
    }}
}}
", "(float)");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("", "(Nested1.C)", "(Nested2.C)")]
        [InlineData("using N1.Nested1;", "(C)", "(Nested2.C)")]
        [InlineData("using N1.Nested2;", "(C)", "(Nested1.C)")]
        [InlineData("using N1.Nested1;using N1.Nested2;", "(Nested1.C)", "(Nested2.C)")]
        public async Task ExplicitUserDefinedConversionTypeDisplayStringIsMinimal(string usingDirective, string displayText1, string displayText2)
        {
            var items = await GetCompletionItemsAsync(@$"
namespace N1.Nested1
{{
    public class C
    {{
    }}
}}

namespace N1.Nested2
{{
    public class C
    {{
    }}
}}
namespace N2
{{
    public class Conversion
    {{
        public static explicit operator N1.Nested1.C(Conversion _) => new N1.Nested1.C();
        public static explicit operator N1.Nested2.C(Conversion _) => new N1.Nested2.C();
    }}
}}
namespace N1
{{
    {usingDirective}
    public class Test
    {{
        public void M()
        {{
            var conversion = new N2.Conversion();
            conversion.$$
        }}
    }}
}}
", SourceCodeKind.Regular);
            Assert.Collection(items,
                i => Assert.Equal(displayText1, i.DisplayText),
                i => Assert.Equal(displayText2, i.DisplayText));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]

        public async Task ExplicitUserDefinedConversionIsSuggestedForAllExplicitConversionsToOtherTypesAndNotForImplicitConversions()
        {
            var items = await GetCompletionItemsAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
    public static explicit operator int(C c) => 0;
    
    public static explicit operator C(float f) => new C();
    public static implicit operator C(string s) => new C();
    public static implicit operator string(C c) => "";
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
            Assert.Collection(items,
                i => Assert.Equal("(float)", i.DisplayText),
                i => Assert.Equal("(int)", i.DisplayText));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]

        public async Task ExplicitUserDefinedConversionFromOtherTypeToTargetIsNotSuggested()
        {
            await VerifyNoItemsExistAsync(@"
public class C
{
    public static explicit operator C(float f) => new C();
}

public class Program
{
    public void Main()
    {
        float f = 1;
        f.$$
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsApplied()
        {
            await VerifyCustomCommitProviderAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "(float)", @"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        ((float)c).$$
    }
}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", "((float)c).$$")]
        [InlineData("c.$$;", "((float)c).$$;")]
        [InlineData("var f = c.$$;", "var f = ((float)c).$$;")]
        [InlineData("c?.$$", "((float)c)?.$$")]
        [InlineData("((C)c).$$", "((float)((C)c)).$$")]
        [InlineData("(true ? c : c).$$", "((float)(true ? c : c)).$$")]
        public async Task ExplicitUserDefinedConversionIsAppliedForDifferentInvcations(string invocation, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {invocation}
    }}
}}
", "(float)", @$"
public class C
{{
    public static explicit operator float(C c) => 0;
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

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("/* Leading */c.$$", "/* Leading */((float)c).$$")]
        [InlineData("c.  $$", "((float)c).  $$")]
        [InlineData("(true ? /* Inline */ c : c).$$", "((float)(true ? /* Inline */ c : c)).$$")]
        public async Task ExplicitUserDefinedConversionTriviaHandling(string invocation, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {invocation}
    }}
}}
", "(float)", @$"
public class C
{{
    public static explicit operator float(C c) => 0;
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

        // 
        // Indexer
        //

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
", "[int]");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("N2", "", "[N1.Nested1.C, N1.Nested2.C]")]
        [InlineData("N2", "using N1.Nested1;", "[C, N1.Nested2.C]")]
        [InlineData("N2", "using N1.Nested2;", "[N1.Nested1.C, C]")]
        [InlineData("N2", "using N1.Nested1; using N1.Nested2;", "[N1.Nested1.C, N1.Nested2.C]")]
        [InlineData("N1", "", "[Nested1.C, Nested2.C]")]
        [InlineData("N1", "using N1.Nested1;", "[C, Nested2.C]")]
        [InlineData("N1", "using N1.Nested2;", "[Nested1.C, C]")]
        [InlineData("N1", "using N1.Nested1; using N1.Nested2;", "[Nested1.C, Nested2.C]")]
        public async Task IndexerDisplayStringContainsMinimalQualifyingTypeNameOfParameters(string namespaceOfIndexer, string usingDirective, string suggestion)
        {
            await VerifyItemExistsAsync(@$"
namespace N1.Nested1
{{
    public class C {{ }}
}}
namespace N1.Nested2
{{
    public class C {{ }}
}}
namespace {namespaceOfIndexer}
{{
    {usingDirective}
    public class Indexer
    {{
        public int this[N1.Nested1.C, N1.Nested2.C] => i;
    }}
    
    public class Program
    {{
        public void Main()
        {{
            var i = new Indexer();
            i.$$
        }}
    }}
}}
", suggestion);
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
", "[int]", @"
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
", "[int, int]", @"
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
        [InlineData("var f = c.$$;", "var f = c[$$];")]
        [InlineData("c?.$$", "c?[$$]")]
        [InlineData("((C)c).$$", "((C)c)[$$]")]
        [InlineData("(true ? c : c).$$", "(true ? c : c)[$$]")]
        public async Task IndexerCompletionForDifferentInvocations(string invocation, string fixedCode)
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
        {invocation}
    }}
}}
", "[int]", @$"
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
    }
}
