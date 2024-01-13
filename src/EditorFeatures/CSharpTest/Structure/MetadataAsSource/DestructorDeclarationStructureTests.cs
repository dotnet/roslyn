﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure.MetadataAsSource;

[Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
public class DestructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<DestructorDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new DestructorDeclarationStructureProvider();

    [Fact]
    public async Task NoCommentsOrAttributes()
    {
        var code = """
                class Goo
                {
                    $$~Goo();
                }
                """;

        await VerifyNoBlockSpansAsync(code);
    }

    [Fact]
    public async Task WithAttributes()
    {
        var code = """
                class Goo
                {
                    {|hint:{|textspan:[Bar]
                    |}$$~Goo();|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task WithCommentsAndAttributes()
    {
        var code = """
                class Goo
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Bar]
                    |}$$~Goo();|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
