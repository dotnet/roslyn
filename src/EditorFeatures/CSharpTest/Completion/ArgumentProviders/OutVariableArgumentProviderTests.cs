// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.ArgumentProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class OutVariableArgumentProviderTests : AbstractCSharpArgumentProviderTests
{
    private static readonly OptionsCollection s_useExplicitTypeOptions = new(LanguageNames.CSharp)
    {
        { CSharpCodeStyleOptions.VarForBuiltInTypes, false },
        { CSharpCodeStyleOptions.VarWhenTypeIsApparent, false },
        { CSharpCodeStyleOptions.VarElsewhere, false },
    };

    private static readonly OptionsCollection s_useExplicitMetadataTypeOptions = new(LanguageNames.CSharp)
    {
        { CSharpCodeStyleOptions.VarForBuiltInTypes, false },
        { CSharpCodeStyleOptions.VarWhenTypeIsApparent, false },
        { CSharpCodeStyleOptions.VarElsewhere, false },
        { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false },
    };

    private static readonly OptionsCollection s_useImplicitTypeOptions = new(LanguageNames.CSharp)
    {
        { CSharpCodeStyleOptions.VarForBuiltInTypes, true },
        { CSharpCodeStyleOptions.VarWhenTypeIsApparent, true },
        { CSharpCodeStyleOptions.VarElsewhere, true },
    };

    internal override Type GetArgumentProviderType()
        => typeof(OutVariableArgumentProvider);

    [Theory]
    [InlineData("")]
    [InlineData("ref")]
    [InlineData("in")]
    public Task TestUnsupportedModifiers(string modifier)
        => VerifyDefaultValueAsync($$"""
            class C
            {
                void Method()
                {
                    TryParse($$)
                }

                bool TryParse({{modifier}} int value) => throw null;
            }
            """, expectedDefaultValue: null);

    [Fact]
    public async Task TestDeclareVariable()
    {
        var markup = $$"""
            class C
            {
                void Method()
                {
                    int.TryParse("x", $$)
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "out var result");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/53056")]
    [CombinatorialData]
    public async Task TestDeclareVariableBuiltInType(bool preferVar, bool preferBuiltInType)
    {
        var expected = (preferVar, preferBuiltInType) switch
        {
            (true, _) => "out var result",
            (false, true) => "out int result",
            (false, false) => "out Int32 result",
        };

        var options = (preferVar, preferBuiltInType) switch
        {
            (true, _) => s_useImplicitTypeOptions,
            (false, true) => s_useExplicitTypeOptions,
            (false, false) => s_useExplicitMetadataTypeOptions,
        };

        await VerifyDefaultValueAsync($$"""
            using System;
            class C
            {
                void Method()
                {
                    int.TryParse("x", $$)
                }
            }
            """, expected, options: options);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("bool")]
    [InlineData("int?")]
    public async Task TestDeclareVariableEscapedIdentifier(string type)
    {
        var markup = $$"""
            class C
            {
                void Method()
                {
                    this.Target($$);
                }

                void Target(out {{type}} @void)
                {
                    @void = default;
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "out var @void");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
    }
}
