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

public sealed class ConversionOperatorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<ConversionOperatorDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ConversionOperatorDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task NoCommentsOrAttributes()
        => VerifyNoBlockSpansAsync("""
                class C
                {
                    public static explicit operator $$Goo(byte b);
                }
                """);

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:{|textspan:[Blah]
                    |}public static explicit operator $$Goo(byte b);|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Blah]
                    |}public static explicit operator $$Goo(byte b);|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public Task TestOperator3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    $${|#0:public static explicit operator C(byte i){|textspan:
                    {
                    }|#0}
                |}
                    public static explicit operator C(short i)
                    {
                    }
                }
                """,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
