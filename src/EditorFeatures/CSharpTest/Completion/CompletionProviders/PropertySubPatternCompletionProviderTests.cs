// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class PropertySubpatternCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(PropertySubpatternCompletionProvider);

    [Fact]
    public async Task PropertiesInRecursivePattern()
    {
        var markup =
            """
            class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this is Program { $$ }
                }
            }
            """;
        // VerifyItemExistsAsync also tests with the item typed.
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_WithPositional()
    {
        var markup =
            """
            public class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this is Program (1, 2) { $$ }
                }
                public void Deconstruct(out int x, out int y) => throw null;
            }
            """;
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_WithPositional_UsingStaticType()
    {
        var markup =
            """
            public class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this is (1, 2) { $$ }
                }
                public void Deconstruct(out int x, out int y) => throw null;
            }
            """;
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_WithEscapedKeyword()
    {
        var markup =
            """
            class Program
            {
                public int @new { get; set; }
                public int @struct { get; set; }

                void M()
                {
                    _ = this is Program { $$ }
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "@new", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "@struct", displayTextSuffix: "");
    }

    [Fact]
    public Task PropertiesInRecursivePattern_WriteOnlyProperties()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                public int P1 { set => throw null; }

                void M()
                {
                    _ = this is Program { $$ }
                }
            }
            """);

    [Fact]
    public async Task PropertiesInRecursivePattern_WithDerivedType()
    {
        var markup =
            """
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
            """;
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_WithOtherType()
    {
        var markup =
            """
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
            """;
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "F2", displayTextSuffix: "");
    }

    [Fact]
    public Task PropertiesInRecursivePattern_WithDerivedType_WithInaccessibleMembers()
        => VerifyNoItemsExistAsync("""
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
            """);

    [Fact]
    public Task PropertiesInRecursivePattern_WithDerivedType_WithPrivateMember()
        => VerifyItemExistsAsync("""
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
            """, "P1", displayTextSuffix: "");

    [Fact]
    public async Task PropertiesInRecursivePattern_UseStaticTypeFromIs()
    {
        var markup =
            """
            class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this is { $$ }
                }
            }
            """;
        // VerifyItemExistsAsync also tests with the item typed.
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_InSwitchStatement()
    {
        var markup =
            """
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
            """;
        // VerifyItemExistsAsync also tests with the item typed.
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_UseStaticTypeFromSwitchStatement()
    {
        var markup =
            """
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
            """;
        // VerifyItemExistsAsync also tests with the item typed.
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_InSwitchExpression()
    {
        var markup =
            """
            class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this switch { { $$ } }
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_NestedInProperty()
    {
        var markup =
            """
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
            """;
        await VerifyItemIsAbsentAsync(markup, "P1");
        await VerifyItemIsAbsentAsync(markup, "P2");
        await VerifyItemExistsAsync(markup, "P3", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P4", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_NestedInField()
    {
        var markup =
            """
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
            """;
        await VerifyItemIsAbsentAsync(markup, "P1");
        await VerifyItemIsAbsentAsync(markup, "F2");
        await VerifyItemExistsAsync(markup, "P3", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P4", displayTextSuffix: "");
    }

    [Fact]
    public async Task PropertiesInRecursivePattern_Nested_WithFields()
    {
        var markup =
            """
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
            """;
        await VerifyItemIsAbsentAsync(markup, "P1");
        await VerifyItemIsAbsentAsync(markup, "P2");
        await VerifyItemExistsAsync(markup, "F3", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "F4", displayTextSuffix: "");
    }

    [Fact]
    public Task PropertiesInRecursivePattern_Nested_WithMissingProperty()
        => VerifyNoItemsExistAsync("""
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
            """);

    [Fact]
    public Task PropertiesInRecursivePattern_NoType()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = missing is { $$ }
                }
            }
            """);

    [Fact]
    public Task PropertiesInRecursivePattern_MissingAfterColon()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this is { P1: $$ }
                }
            }
            """);

    [Fact]
    public async Task PropertiesInRecursivePattern_SecondProperty()
    {
        var markup =
            """
            class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this is Program { P2: 1, $$ }
                }
            }
            """;
        // VerifyItemExistsAsync also tests with the item typed.
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemIsAbsentAsync(markup, "P2");
    }

    [Fact]
    public Task PropertiesInRecursivePattern_PositionalInFirstProperty()
        => VerifyNoItemsExistAsync("""
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
            """);

    [Fact]
    public Task PropertiesInRecursivePattern_PositionalInFirstProperty_AfterComma()
        => VerifyNoItemsExistAsync("""
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
            """);

    [Fact]
    public Task PropertiesInRecursivePattern_PositionalInFirstProperty_AfterCommaAndBeforeParen()
        => VerifyNoItemsExistAsync("""
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
            """);

    [Fact]
    public Task PropertiesInRecursivePattern_InPositional_Incomplete()
        => VerifyItemExistsAsync("""
            public class Program
            {
                public int P1 { get; set; }

                void M()
                {
                    _ = this is ({ $$ }) // Can deconstruct into a parenthesized property pattern
                }

                public void Deconstruct(out Program x, out Program y) => throw null;
            }
            """, "P1", displayTextSuffix: "");

    [Fact]
    public Task PropertiesInRecursivePattern_InPositional_Incomplete_WithoutClosingBrace()
        => VerifyItemExistsAsync("""
            public class Program
            {
                public int P1 { get; set; }

                void M()
                {
                    _ = this is ({ $$   // Can deconstruct into a parenthesized property pattern
                }

                public void Deconstruct(out Program x, out Program y) => throw null;
            }
            """, "P1", displayTextSuffix: "");

    [Fact]
    public Task PropertiesInRecursivePattern_InPositional_Incomplete_WithTwoTypes()
        => VerifyNoItemsExistAsync("""
            public class Program
            {
                void M()
                {
                    _ = this is ({ $$ }) // no deconstruction into 1 element
                }

                public void Deconstruct(out D x, out Program y) => throw null;
            }
            public class D
            {
                public int P2 { get; set; }
            }
            """);

    [Fact]
    public Task PropertiesInRecursivePattern_InPositional_Complete_BeforeComma()
        => VerifyItemExistsAsync("""
            public class Program
            {
                public int P1 { get; set; }

                void M()
                {
                    _ = this is ({ $$ }, )
                }

                public void Deconstruct(out Program x, out Program y) => throw null;
            }
            """, "P1", displayTextSuffix: "");

    [Fact]
    public Task PropertiesInRecursivePattern_InPositional_Complete_AfterComma()
        => VerifyItemExistsAsync("""
            public class Program
            {
                public int P1 { get; set; }

                void M()
                {
                    _ = this is ( , { $$ })
                }

                public void Deconstruct(out Program x, out Program y) => throw null;
            }
            """, "P1", displayTextSuffix: "");

    [Fact]
    public Task PropertiesInRecursivePattern_NoPropertyLeft()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                public int P1 { get; set; }
                public int P2 { get; set; }

                void M()
                {
                    _ = this is Program { P2: 1, P1: 2, $$ }
                }
            }
            """);

    [Fact]
    public async Task PropertiesInRecursivePattern_NotForEditorUnbrowsable()
    {
        var markup =
            """
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
            """;
        // Ignore browsability limiting attributes if the symbol is declared in source.
        await VerifyItemExistsAsync(markup, "P1", displayTextSuffix: "");
        await VerifyItemExistsAsync(markup, "P2", displayTextSuffix: "");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33250")]
    public Task StaticProperties_NotSuggested()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                void M()
                {
                    _ = "" is { $$ }
                }
            }
            """, "Empty");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33250")]
    public Task StaticFields_NotSuggested()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                static int x = 42;

                void M()
                {
                    _ = this is { $$ }
                }
            }
            """, "x");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33250")]
    public Task ConstFields_NotSuggested()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                void M()
                {
                    _ = 5 is { $$ }
                }
            }
            """, "MaxValue");
}
