// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class PropertySubPatternCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public PropertySubPatternCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new PropertySubPatternCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertiesInRecursivePattern()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = this is Program { $$ }
    }
}
";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "P2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertiesInRecursivePattern_MissingType()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = this is { $$ }
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertiesInRecursivePattern_MissingAfterColon()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = this is { P1: $$ }
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertiesInRecursivePattern_SecondProperty()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = this is Program { P2: 1, $$ }
    }
}
";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "P1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertiesInRecursivePattern_NoPropertyLeft()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = this is Program { P2: 1, P1: 2, $$ }
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertiesInRecursivePattern_NotForEditorUnbrowsable()
        {
            var markup =
@"
class Program
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int P1 { get; set; }

    public int P2 { get; set; }

    void M()
    {
        _ = this is Program { $$ }
    }
}
";
            await VerifyItemExistsAsync(markup, "P2");
        }
    }
}
