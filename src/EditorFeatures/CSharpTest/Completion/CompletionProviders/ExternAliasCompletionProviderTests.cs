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
public sealed class ExternAliasCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(ExternAliasCompletionProvider);

    [Fact]
    public Task NoAliases()
        => VerifyNoItemsExistAsync("""
            extern alias $$
            class C
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public Task ExternAlias()
        => VerifyItemWithAliasedMetadataReferencesAsync("""
            extern alias $$
            """, "goo", "goo", 1, "C#", "C#");

    [Fact]
    public Task NotAfterExternAlias()
        => VerifyItemWithAliasedMetadataReferencesAsync("""
            extern alias goo $$
            """, "goo", "goo", 0, "C#", "C#");

    [Fact]
    public Task NotGlobal()
        => VerifyItemWithAliasedMetadataReferencesAsync("""
            extern alias $$
            """, "goo", "global", 0, "C#", "C#");

    [Fact]
    public Task NotIfAlreadyUsed()
        => VerifyItemWithAliasedMetadataReferencesAsync("""
            extern alias goo;
            extern alias $$
            """, "goo", "goo", 0, "C#", "C#");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075278")]
    public Task NotInComment()
        => VerifyNoItemsExistAsync("""
            extern alias // $$
            """);
}
