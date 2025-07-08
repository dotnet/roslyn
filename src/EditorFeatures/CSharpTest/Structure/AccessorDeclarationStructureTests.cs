// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class AccessorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<AccessorDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new AccessorDeclarationStructureProvider();

    [Fact]
    public async Task TestPropertyGetter1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        $${|hint:get{|textspan:
                        {
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithSingleLineComments1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        {|span1:// My
                        // Getter|}
                        $${|hint2:get{|textspan2:
                        {
                        }|}|}
                    }
                }
                """,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetter2()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        $${|hint:get{|textspan:
                        {
                        }|}|}
                        set
                        {
                        }
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithSingleLineComments2()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        {|span1:// My
                        // Getter|}
                        $${|hint2:get{|textspan2:
                        {
                        }|}|}
                        set
                        {
                        }
                    }
                }
                """,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetter3()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        $${|hint:get{|textspan:
                        {
                        }|}|}

                        set
                        {
                        }
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithSingleLineComments3()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        {|span1:// My
                        // Getter|}
                        $${|hint2:get{|textspan2:
                        {
                        }|}|}

                        set
                        {
                        }
                    }
                }
                """,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetter1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        $${|hint:set{|textspan:
                        {
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithSingleLineComments1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        {|span1:// My
                        // Setter|}
                        $${|hint2:set{|textspan2:
                        {
                        }|}|}
                    }
                }
                """,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetter2()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        get
                        {
                        }
                        $${|hint:set{|textspan:
                        {
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithSingleLineComments2()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        get
                        {
                        }
                        {|span1:// My
                        // Setter|}
                        $${|hint2:set{|textspan2:
                        {
                        }|}|}
                    }
                }
                """,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetter3()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        get
                        {
                        }

                        $${|hint:set{|textspan:
                        {
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithSingleLineComments3()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    public string Text
                    {
                        get
                        {
                        }

                        {|span1:// My
                        // Setter|}
                        $${|hint2:set{|textspan2:
                        {
                        }|}|}
                    }
                }
                """,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
