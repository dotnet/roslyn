﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task PropertiesInRecursivePattern_WithEscapedKeyword()
        {
            var markup =
@"
class Program
{
    public int @new { get; set; }
    public int @struct { get; set; }

    void M()
    {
        _ = this is Program { $$ }
    }
}
";
            await VerifyItemExistsAsync(markup, "@new");
            await VerifyItemExistsAsync(markup, "@struct");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_WriteOnlyProperties()
        {
            var markup =
@"
class Program
{
    public int P1 { set => throw null; }

    void M()
    {
        _ = this is Program { $$ }
    }
}
";
            await VerifyNoItemsExistAsync(markup);
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
        public async Task PropertiesInRecursivePattern_WithOtherType()
        {
            var markup =
@"
class Program
{
    void M(Other other)
    {
        _ = other is Other { $$ }
    }
}
class Other
{
    public int P1 { get; set; }
    public int F2;
}
";
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "F2");
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
        public async Task PropertiesInRecursivePattern_WithDerivedType_WithPrivateMember()
        {
            var markup =
@"
class Program
{
    private int P1 { get; set; }
    void M()
    {
        _ = this is Derived { $$ }
    }
}
class Derived : Program
{
    private int P2 { get; set; }
}
";
            await VerifyItemExistsAsync(markup, "P1");
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

        [Fact(Skip = "PROTOTYPE(patterns2)")]
        public async Task PropertiesInRecursivePattern_InSwitchExpression()
        {
            // PROTOTYPE(patterns2): I think this will be fixed by using GetTypeInfo (once merged)
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = this switch { { $$ } }
    }
}
";
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "P2");
        }

        [Fact(Skip = "PROTOTYPE(patterns2)")]
        public async Task PropertiesInRecursivePattern_WithPositionalPattern()
        {
            // PROTOTYPE(patterns2): I think this will be fixed by using GetTypeInfo (once merged)
            var markup =
@"
class Program
{
    public int P1 { get; set; }
    public int P2 { get; set; }

    void M()
    {
        _ = this is (1, 2) { $$ }
    }
    void Deconstruct(out int x, out int y) => throw null;
}
";
            await VerifyItemExistsAsync(markup, "P1");
            await VerifyItemExistsAsync(markup, "P2");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_NestedInProperty()
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
            await VerifyItemIsAbsentAsync(markup, "P1");
            await VerifyItemIsAbsentAsync(markup, "P2");
            await VerifyItemExistsAsync(markup, "P3");
            await VerifyItemExistsAsync(markup, "P4");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_NestedInField()
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
    public Nested F2;

    void M()
    {
         _ = this is Program { F2: { $$ } }
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "P1");
            await VerifyItemIsAbsentAsync(markup, "F2");
            await VerifyItemExistsAsync(markup, "P3");
            await VerifyItemExistsAsync(markup, "P4");
        }

        [Fact]
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
            await VerifyItemIsAbsentAsync(markup, "P1");
            await VerifyItemIsAbsentAsync(markup, "P2");
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
            await VerifyItemIsAbsentAsync(markup, "P2");
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_PositionalInFirstProperty()
        {
            var markup =
@"
class Program
{
    public D P1 { get; set; }

    void M()
    {
        _ = this is Program { P1: ($$ }
    }
}
class D
{
    public int P2 { get; set; }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_PositionalInFirstProperty_AfterComma()
        {
            var markup =
@"
class Program
{
    public D P1 { get; set; }

    void M()
    {
        _ = this is Program { P1: (1, $$ }
    }
}
class D
{
    public int P2 { get; set; }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task PropertiesInRecursivePattern_PositionalInFirstProperty_AfterCommaAndBeforeParen()
        {
            var markup =
@"
class Program
{
    public D P1 { get; set; }

    void M()
    {
        _ = this is Program { P1: (1, $$) }
    }
}
class D
{
    public int P2 { get; set; }
}
";
            await VerifyNoItemsExistAsync(markup);
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
            await VerifyItemExistsAsync(markup, "P1"); // PROTOTYPE(patterns2): Need to review and confirm
            await VerifyItemExistsAsync(markup, "P2");
        }
    }
}
