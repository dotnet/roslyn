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

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class AccessorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<AccessorDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new AccessorDeclarationStructureProvider();

    [Fact]
    public Task TestPropertyGetter3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        $${|#0:get{|textspan:
                        {
                        }|#0}
                |}
                        set
                        {
                        }
                    }
                }
                """,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestPropertyGetterWithSingleLineComments3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        {|span1:// My
                        // Getter|}
                        $${|#0:get{|textspan2:
                        {
                        }|#0}
                |}
                        set
                        {
                        }
                    }
                }
                """,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestPropertyGetterWithMultiLineComments3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        /* My
                           Getter */
                        $${|#0:get{|textspan1:
                        {
                        }|#0}
                |}
                        set
                        {
                        }
                    }
                }
                """,
            Region("textspan1", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
