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

public class PropertyDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<PropertyDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new PropertyDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task NoCommentsOrAttributes()
    {
        var code = """
                class Goo
                {
                    {|hint:public string $$Prop {|textspan:{ get; set; }|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithAttributes()
    {
        var code = """
                class Goo
                {
                    {|hint1:{|textspan1:[Goo]
                    |}{|hint2:public string $$Prop {|textspan2:{ get; set; }|}|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAndAttributes()
    {
        var code = """
                class Goo
                {
                    {|hint1:{|textspan1:// Summary:
                    //     This is a summary.
                    [Goo]
                    |}{|hint2:string $$Prop {|textspan2:{ get; set; }|}|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAttributesAndModifiers()
    {
        var code = """
                class Goo
                {
                    {|hint1:{|textspan1:// Summary:
                    //     This is a summary.
                    [Goo]
                    |}{|hint2:public string $$Prop {|textspan2:{ get; set; }|}|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public async Task TestProperty3()
    {
        var code = """
                class C
                {
                    $${|#0:public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|#0}
                |}
                    public int Goo2
                    {
                        get { }
                        set { }
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public async Task TestProperty4()
    {
        var code = """
                class C
                {
                    $${|#0:public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|#0}
                |}
                    public int this[int value]
                    {
                        get { }
                        set { }
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
