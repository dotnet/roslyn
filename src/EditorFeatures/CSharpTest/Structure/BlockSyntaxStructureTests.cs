// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public class BlockSyntaxStructureTests : AbstractCSharpSyntaxNodeStructureTests<BlockSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new BlockSyntaxStructureProvider();

    [Fact]
    public async Task TestTryBlock1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:try{|textspan:
                        {$$
                        }
                        catch 
                        {
                        }
                        finally
                        {
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestUnsafe1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:unsafe{|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestFixed1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:fixed(int* i = &j){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestUsing1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:using (goo){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestLock1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:lock (goo){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestForStatement1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:for (;;){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestForEachStatement1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:foreach (var v in e){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestCompoundForEachStatement1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:foreach ((var v, var x) in e){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestWhileStatement1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:while (true){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestDoStatement1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:do{|textspan:
                        {$$
                        }
                        while (true);|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfStatement1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:if (true){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfStatement2()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:if (true){|textspan:
                        {$$
                        }|}|}
                        else
                        {
                        }
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfStatement3()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:if (true){|textspan:
                        {$$
                        }|}|}
                        else
                            return;
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestElseClause1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        {|hint:else{|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestElseClause2()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|primaryhint:if (true){|primarytextspan:
                        {
                        }|}|}
                        {|hint:else{|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", "primaryhint", "primarytextspan", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfElse1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        {|hint:else if (false){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfElse2()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        {|hint:else
                            if (false ||
                                true){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfElse3()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        {|hint:else if (false ||
                            true){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestNestedBlock()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        {|hint:{|textspan:{$$

                        }|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestNestedBlockInSwitchSection1()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        switch (e)
                        {
                            case 0:
                                {|hint:{|textspan:{$$

                                }|}|}
                        }
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestNestedBlockInSwitchSection2()
    {
        var code = """
                class C
                {
                    void M()
                    {
                        switch (e)
                        {
                        case 0:
                            int i = 0;
                            {|hint:{|textspan:{$$

                            }|}|}
                        }
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem(52493, "https://github.com/dotnet/roslyn/issues/")]
    public async Task LocalFunctionInTopLevelStatement_AutoCollapse()
    {
        var code = """
                Goo();
                Bar();

                {|hint:static void Goo(){|textspan:
                {$$
                   // ...
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68513")]
    public async Task LocalFunctionInBodyRespectOption1()
    {
        var code = """
            class C
            {
                void M()
                {
                    {|hint:static void Goo(){|textspan:
                    {$$
                       // ...
                    }|}|}
                }
            }
            """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68513")]
    public async Task LocalFunctionInBodyRespectOption2()
    {
        var code = """
            class C
            {
                void M()
                {
                    {|hint:static void Goo(){|textspan:
                    {$$
                       // ...
                    }|}|}
                }
            }
            """;

        await VerifyBlockSpansAsync(code, GetDefaultOptions() with
        {
            CollapseLocalFunctionsWhenCollapsingToDefinitions = true,
        }, Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
