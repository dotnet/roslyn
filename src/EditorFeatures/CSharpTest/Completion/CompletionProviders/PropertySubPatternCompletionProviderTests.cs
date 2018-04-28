// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class PropertySubPatternCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public PropertySubPatternCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new PropertySubPatternCompletionProvider();
        }

        [Fact]
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

        [Fact]
        public async Task PropertiesInRecursivePattern_WithDerivedType()
        {
            var markup =
@"
class Program
{
    void M()
    {
        _ = this is Derived { $$ }
    }
}
class Derived
{
    public int P1 { get; set; }
    public int P2 { get; set; }
}
";
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "P2");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_WithDerivedType_WithInaccessibleMembers()
        {
            var markup =
@"
class Program
{
    void M()
    {
        _ = this is Derived { $$ }
    }
}
class Derived : Program
{
    private int P1 { get; set; }
    private int P2 { get; set; }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_UseStaticTypeFromIs()
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
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "P2");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_InSwitchStatement()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        switch (this)
        {
            case Program { $$ }
        }
    }
}
";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "P2");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_UseStaticTypeFromSwitchStatement()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        switch (this)
        {
            case { $$ }
        }
    }
}
";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "P2");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_Nested()
        {
            var markup =
@"
public class Nested
{
    public int P3 { get; set; }
    public int P4 { get; set; }
}
class Program
{
    public int P1 { get; set; }
    public Nested P2 { get; set; }

    void M()
    {
         _ = this is Program { P2: { $$ } }
    }
}
";
            await VerifyItemExistsAsync(markup, "P3");
            await VerifyItemExistsAsync(markup, "P4");
        }

        [Fact,]
        public async Task PropertiesInRecursivePattern_Nested_WithFields()
        {
            var markup =
@"
public class Nested
{
    public int F3;
    public int F4;
}
class Program
{
    public int P1 { get; set; }
    public Nested P2 { get; set; }

    void M()
    {
         _ = this is Program { P2: { $$ } }
    }
}
";
            await VerifyItemExistsAsync(markup, "F3");
            await VerifyItemExistsAsync(markup, "F4");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_Nested_WithMissingProperty()
        {
            var markup =
@"
public class Nested
{
    public int P3 { get; set; }
    public int P4 { get; set; }
}
class Program
{
    public int P1 { get; set; }
    public Nested P2 { get; set; }

    void M()
    {
         _ = this is Program { : { $$ } }
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_NoType()
        {
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = missing is { $$ }
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
