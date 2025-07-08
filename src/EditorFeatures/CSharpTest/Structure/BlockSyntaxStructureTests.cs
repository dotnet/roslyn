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
    public Task TestTryBlock1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestUnsafe1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestFixed1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestUsing1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestLock1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestForStatement1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestForEachStatement1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestCompoundForEachStatement1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestWhileStatement1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestDoStatement1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestIfStatement1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestIfStatement2()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestIfStatement3()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestElseClause1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestElseClause2()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestIfElse1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestIfElse2()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestIfElse3()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestNestedBlock()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestNestedBlockInSwitchSection1()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestNestedBlockInSwitchSection2()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem(52493, "https://github.com/dotnet/roslyn/issues/")]
    public Task LocalFunctionInTopLevelStatement_AutoCollapse()
        => VerifyBlockSpansAsync("""
                Goo();
                Bar();

                {|hint:static void Goo(){|textspan:
                {$$
                   {|hint2:{|textspan2:// comment|}|}
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", "// comment ...", autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68513")]
    public Task LocalFunctionInBodyRespectOption1()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68513")]
    public Task LocalFunctionInBodyRespectOption2()
        => VerifyBlockSpansAsync("""
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
