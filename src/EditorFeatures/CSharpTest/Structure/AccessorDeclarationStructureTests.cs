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
public class AccessorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<AccessorDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new AccessorDeclarationStructureProvider();

    [Fact]
    public async Task TestPropertyGetter1()
    {
        var code = """
                class C
                {
                    public string Text
                    {
                        $${|hint:get{|textspan:
                        {
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithSingleLineComments1()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetter2()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithSingleLineComments2()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetter3()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithSingleLineComments3()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetter1()
    {
        var code = """
                class C
                {
                    public string Text
                    {
                        $${|hint:set{|textspan:
                        {
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithSingleLineComments1()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetter2()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithSingleLineComments2()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetter3()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithSingleLineComments3()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// My ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
