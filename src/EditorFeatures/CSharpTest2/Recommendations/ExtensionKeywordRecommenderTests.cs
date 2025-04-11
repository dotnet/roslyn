// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ExtensionKeywordRecommenderTests : KeywordRecommenderTests
{
    private static readonly CSharpParseOptions s_options = CSharpNextParseOptions;

    [Fact]
    public async Task NotInRoot()
    {
        await VerifyAbsenceAsync(@"$$", s_options);
    }

    [Fact]
    public async Task NotInNormalClass()
    {
        await VerifyAbsenceAsync("""
            class C
            {
                $$
            }
            """, s_options);
    }

    [Fact]
    public async Task InStaticClass()
    {
        await VerifyKeywordAsync("""
            static class C
            {
                $$
            }
            """, s_options);
    }

    [Fact]
    public async Task NotAfterAccessibilityInStaticClass()
    {
        await VerifyAbsenceAsync("""
            static class C
            {
                public $$
            }
            """, s_options);
    }

    [Fact]
    public async Task NotAfterModifierInStaticClass()
    {
        await VerifyAbsenceAsync("""
            static class C
            {
                unsafe $$
            }
            """, s_options);
    }

    [Fact]
    public async Task NotAfterPartialInStaticClass()
    {
        await VerifyAbsenceAsync("""
            static class C
            {
                partial $$
            }
            """, s_options);
    }

    [Fact]
    public async Task NotInStaticStructClass()
    {
        await VerifyAbsenceAsync("""
            static struct C
            {
                $$
            }
            """, s_options);
    }

    [Fact]
    public async Task AfterMethodInStaticClass()
    {
        await VerifyKeywordAsync("""
            static class C
            {
                void M() { }
                $$
            }
            """, s_options);
    }

    [Fact]
    public async Task AfterClassInStaticClass()
    {
        await VerifyKeywordAsync("""
            static class C
            {
                class M { }
                $$
            }
            """, s_options);
    }

    [Fact]
    public async Task AfterExtensionInStaticClass()
    {
        await VerifyKeywordAsync("""
            static class C
            {
                extension E() { }
                $$
            }
            """, s_options);
    }

    [Fact]
    public async Task NotInClassInStaticClass()
    {
        await VerifyAbsenceAsync("""
            static class C
            {
                class C
                {
                    $$
                }
            }
            """, s_options);
    }

    [Fact]
    public async Task TestWithinExtension()
    {
        await VerifyAbsenceAsync(
            """
                static class C
                {
                    extension(string s)
                    {
                        $$
                    }
                }
                """, CSharpNextParseOptions);
    }
}
