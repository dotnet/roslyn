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
public sealed class DefaultArgumentProviderTests : AbstractCSharpArgumentProviderTests
{
    internal override Type GetArgumentProviderType()
        => typeof(DefaultArgumentProvider);

    [Theory]
    [InlineData("System.Collections.DictionaryEntry")]
    public async Task TestDefaultValueIsDefaultLiteral(string type)
    {
        var markup = $$"""
            class C
            {
                void Method()
                {
                    this.Target($$);
                }

                void Target({{type}} arg)
                {
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "default");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: "prior", previousDefaultValue: "prior");
    }

    [Theory]
    [InlineData("int?")]
    [InlineData("System.Int32?")]
    [InlineData("object")]
    [InlineData("System.Object")]
    [InlineData("System.Exception")]
    public async Task TestDefaultValueIsNullLiteral(string type)
    {
        var markup = $$"""
            class C
            {
                void Method()
                {
                    this.Target($$);
                }

                void Target({{type}} arg)
                {
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, "null");
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: "prior", previousDefaultValue: "prior");
    }

    [Theory]
    [InlineData("bool", "false")]
    [InlineData("System.Boolean", "false")]
    [InlineData("float", "0.0f")]
    [InlineData("System.Single", "0.0f")]
    [InlineData("double", "0.0")]
    [InlineData("System.Double", "0.0")]
    [InlineData("decimal", "0.0m")]
    [InlineData("System.Decimal", "0.0m")]
    [InlineData("char", @"'\\0'")]
    [InlineData("System.Char", @"'\\0'")]
    [InlineData("byte", "(byte)0")]
    [InlineData("System.Byte", "(byte)0")]
    [InlineData("sbyte", "(sbyte)0")]
    [InlineData("System.SByte", "(sbyte)0")]
    [InlineData("short", "(short)0")]
    [InlineData("System.Int16", "(short)0")]
    [InlineData("ushort", "(ushort)0")]
    [InlineData("System.UInt16", "(ushort)0")]
    [InlineData("int", "0")]
    [InlineData("System.Int32", "0")]
    [InlineData("uint", "0U")]
    [InlineData("System.UInt32", "0U")]
    [InlineData("long", "0L")]
    [InlineData("System.Int64", "0L")]
    [InlineData("ulong", "0UL")]
    [InlineData("System.UInt64", "0UL")]
    public async Task TestDefaultValueIsZero(string type, string literalZero)
    {
        var markup = $$"""
            class C
            {
                void Method()
                {
                    this.Target($$);
                }

                void Target({{type}} arg)
                {
                }
            }
            """;

        await VerifyDefaultValueAsync(markup, literalZero);
        await VerifyDefaultValueAsync(markup, expectedDefaultValue: "prior", previousDefaultValue: "prior");
    }
}
