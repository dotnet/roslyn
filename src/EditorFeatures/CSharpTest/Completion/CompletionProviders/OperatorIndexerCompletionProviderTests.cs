// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
