// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.ArgumentProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class ContextVariableArgumentProviderTests : AbstractCSharpArgumentProviderTests
{
    internal override Type GetArgumentProviderType()
        => typeof(ContextVariableArgumentProvider);

    [Theory]
    [InlineData("string")]
    [InlineData("bool")]
    [InlineData("int?")]
    public async Task TestLocalVariable(string type)
    {
        var markup = $$"""
            class C
            {
                void Method()
                {
                    {{type}} arg = default;
                    this.Target($$);
                }

                void Target({{type}} arg)
                {
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "arg");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
    }

    [Theory, CombinatorialData]
    public async Task TestOutVariable(
        [CombinatorialValues("string", "bool", "int?")] string type,
        [CombinatorialValues("out", "ref", "in")] string modifier)
    {
        var markup = $$"""
            class C
            {
                void Method()
                {
                    {{type}} arg;
                    this.Target($$);
                }

                void Target({{modifier}} {{type}} arg)
                {
                }
            }
            """;

        var generatedModifier = modifier switch
        {
            "in" => "",
            _ => $"{modifier} ",
        };

        await VerifyDefaultValueAsync(markup, $"{generatedModifier}arg");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
    }

    [Theory]
    [InlineData("string")]
    [InlineData("bool")]
    [InlineData("int?")]
    public async Task TestParameter(string type)
    {
        var markup = $$"""
            class C
            {
                void Method({{type}} arg)
                {
                    this.Target($$);
                }

                void Target({{type}} arg)
                {
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "arg");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
    }

    [Theory]
    [InlineData("string", "string")]
    [InlineData("string", "IEnumerable<char>")]
    [InlineData("bool", "bool")]
    [InlineData("int?", "int?")]
    [InlineData("int", "int?")]
    public async Task TestInstanceVariable(string fieldType, string parameterType)
    {
        var markup = $$"""
            using System.Collections.Generic;
            class C
            {
                {{fieldType}} arg;

                void Method()
                {
                    this.Target($$);
                }

                void Target({{parameterType}} arg)
                {
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "arg");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
    }

    [Theory]
    [InlineData("C")]
    [InlineData("B")]
    [InlineData("I")]
    public async Task TestThisInstance(string parameterType)
    {
        var markup = $$"""
            using System.Collections.Generic;
            interface I { }

            class B { }

            class C : B, I
            {
                void Method()
                {
                    this.Target($$);
                }

                void Target({{parameterType}} arg)
                {
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "this");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
    }

    [Theory]
    [InlineData("object")]
    [InlineData("string")]
    public Task TestThisInstanceNotProvided1(string parameterType)
        => VerifyDefaultValueAsync($$"""
            using System.Collections.Generic;
            interface I { }

            class B { }

            class C : B, I
            {
                void Method()
                {
                    this.Target($$);
                }

                void Target({{parameterType}} arg)
                {
                }
            }
            """, null);

    [Fact]
    public Task TestThisInstanceNotProvided2()
        => VerifyDefaultValueAsync($$"""
            using System.Collections.Generic;

            class C
            {
                static void Method()
                {
                    Target($$);
                }

                static void Target(C arg)
                {
                }
            }
            """, null);

    // Note: The current implementation checks for exact type match for primitive types. If this changes, some of
    // these tests may need to be updated to account for the new behavior.
    [Theory]
    [InlineData("object", "string")]
    [InlineData("string", "object")]
    [InlineData("bool", "bool?")]
    [InlineData("bool", "int")]
    [InlineData("int", "object")]
    public Task TestMismatchType(string parameterType, string valueType)
        => VerifyDefaultValueAsync($$"""
            class C
            {
                void Method({{valueType}} arg)
                {
                    this.Target($$);
                }

                void Target({{parameterType}} arg)
                {
                }
            }
            """, null);
}
