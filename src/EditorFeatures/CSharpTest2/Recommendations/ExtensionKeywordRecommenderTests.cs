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
    public Task NotInRoot()
        => VerifyAbsenceAsync(@"$$", s_options);

    [Fact]
    public Task NotInNormalClass()
        => VerifyAbsenceAsync("""
            class C
            {
                $$
            }
            """, s_options);

    [Fact]
    public Task InStaticClass()
        => VerifyKeywordAsync("""
            static class C
            {
                $$
            }
            """, s_options);

    [Fact]
    public Task NotAfterAccessibilityInStaticClass()
        => VerifyAbsenceAsync("""
            static class C
            {
                public $$
            }
            """, s_options);

    [Fact]
    public Task NotAfterModifierInStaticClass()
        => VerifyAbsenceAsync("""
            static class C
            {
                unsafe $$
            }
            """, s_options);

    [Fact]
    public Task NotAfterPartialInStaticClass()
        => VerifyAbsenceAsync("""
            static class C
            {
                partial $$
            }
            """, s_options);

    [Fact]
    public Task NotInStaticStructClass()
        => VerifyAbsenceAsync("""
            static struct C
            {
                $$
            }
            """, s_options);

    [Fact]
    public Task AfterMethodInStaticClass()
        => VerifyKeywordAsync("""
            static class C
            {
                void M() { }
                $$
            }
            """, s_options);

    [Fact]
    public Task AfterClassInStaticClass()
        => VerifyKeywordAsync("""
            static class C
            {
                class M { }
                $$
            }
            """, s_options);

    [Fact]
    public Task AfterExtensionInStaticClass()
        => VerifyKeywordAsync("""
            static class C
            {
                extension E() { }
                $$
            }
            """, s_options);

    [Fact]
    public Task NotInClassInStaticClass()
        => VerifyAbsenceAsync("""
            static class C
            {
                class C
                {
                    $$
                }
            }
            """, s_options);

    [Fact]
    public Task TestWithinExtension()
        => VerifyAbsenceAsync(
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
