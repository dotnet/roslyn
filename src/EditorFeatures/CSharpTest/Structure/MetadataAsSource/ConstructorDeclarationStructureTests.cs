// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure.MetadataAsSource;

public class ConstructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<ConstructorDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ConstructorDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task NoCommentsOrAttributes()
    {
        var code = """
                class C
                {
                    $$C();
                }
                """;

        await VerifyNoBlockSpansAsync(code);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithAttributes()
    {
        var code = """
                class C
                {
                    {|hint:{|textspan:[Bar]
                    |}$$C();|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAndAttributes()
    {
        var code = """
                class C
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Bar]
                    |}$$C();|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAttributesAndModifiers()
    {
        var code = """
                class C
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Bar]
                    |}$$public C();|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public async Task TestConstructor10()
    {
        var code = """
                class C
                {
                    $${|#0:public C(){|textspan:
                    {
                    }|#0}
                |}
                    public C(int x)
                    {
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
