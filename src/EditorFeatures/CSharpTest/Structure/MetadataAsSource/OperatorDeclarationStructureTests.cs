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

public class OperatorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<OperatorDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new OperatorDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task NoCommentsOrAttributes()
    {
        var code = """
                class Goo
                {
                    public static bool operator $$==(Goo a, Goo b);
                }
                """;

        await VerifyNoBlockSpansAsync(code);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithAttributes()
    {
        var code = """
                class Goo
                {
                    {|hint:{|textspan:[Blah]
                    |}public static bool operator $$==(Goo a, Goo b);|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAndAttributes()
    {
        var code = """
                class Goo
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Blah]
                    |}bool operator $$==(Goo a, Goo b);|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAttributesAndModifiers()
    {
        var code = """
                class Goo
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Blah]
                    |}public static bool operator $$==(Goo a, Goo b);|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public async Task TestOperator3()
    {
        var code = """
                class C
                {
                    $${|#0:public static int operator +(int i){|textspan:
                    {
                    }|#0}
                |}
                    public static int operator -(int i)
                    {
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
