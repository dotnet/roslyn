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
public sealed class BlockSyntaxStructureTests : AbstractCSharpSyntaxNodeStructureTests<BlockSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new BlockSyntaxStructureProvider();

    [Fact]
    public async Task TestTryBlock1()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestUnsafe1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:unsafe{|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestFixed1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:fixed(int* i = &j){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestUsing1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:using (goo){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestLock1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:lock (goo){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestForStatement1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:for (;;){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestForEachStatement1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:foreach (var v in e){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestCompoundForEachStatement1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:foreach ((var v, var x) in e){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestWhileStatement1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:while (true){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestDoStatement1()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfStatement1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:if (true){|textspan:
                        {$$
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfStatement2()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfStatement3()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestElseClause1()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestElseClause2()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfElse1()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfElse2()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestIfElse3()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestNestedBlock()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:{|textspan:{$$

                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestNestedBlockInSwitchSection1()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestNestedBlockInSwitchSection2()
    {
        await VerifyBlockSpansAsync("""
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
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem(52493, "https://github.com/dotnet/roslyn/issues/")]
    public async Task LocalFunctionInTopLevelStatement_AutoCollapse()
    {
        await VerifyBlockSpansAsync("""
                Goo();
                Bar();

                {|hint:static void Goo(){|textspan:
                {$$
                   {|hint2:{|textspan2:// comment|}|}
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", "// comment ...", autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68513")]
    public async Task LocalFunctionInBodyRespectOption1()
    {
        await VerifyBlockSpansAsync("""
            class C
            {
                void M()
                {
                    {|hint1:static void Goo(){|textspan1:
                    {$$
                       {|hint2:{|textspan2:// comment|}|}
                    }|}|}
                }
            }
            """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("textspan2", "hint2", "// comment ...", autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68513")]
    public async Task LocalFunctionInBodyRespectOption2()
    {
        await VerifyBlockSpansAsync("""
            class C
            {
                void M()
                {
                    {|hint:static void Goo(){|textspan:
                    {$$
                       {|hint2:{|textspan2:// comment|}|}
                    }|}|}
                }
            }
            """, GetDefaultOptions() with
        {
            CollapseLocalFunctionsWhenCollapsingToDefinitions = true,
        }, Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
           Region("textspan2", "hint2", "// comment ...", autoCollapse: true));
    }
}
