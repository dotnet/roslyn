// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertIf;

public partial class InvertIfTests
{
    [Fact]
    public async Task IfWithoutElse_MoveIfBodyToElseClause1()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            if (c)
                            {
                                [||]if (c)
                                {
                                    return 1;
                                }
                            }
                            return 2;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            if (c)
                            {
                                [||]if (!c)
                                {
                                }
                                else
                                {
                                    return 1;
                                }
                            }
                            return 2;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveIfBodyToElseClause2()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (c)
                            {
                                f();
                            }
                            g();
                            g();
                            break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (!c)
                            {
                            }
                            else
                            {
                                f();
                            }
                            g();
                            g();
                            break;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveIfBodyToElseClause3()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    [||]if (c)
                    {
                        f();
                    }
                    g();
                    g();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (!c)
                    {
                    }
                    else
                    {
                        f();
                    }
                    g();
                    g();
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveIfBodyToElseClause4()
    {
        await TestAsync("""
            class C
            {
                bool M()
                {
                    if (c)
                    {
                        [||]if (c)
                        {
                            f();
                        }
                        g();
                    }
                    return false;
                }
            }
            """, """
            class C
            {
                bool M()
                {
                    if (c)
                    {
                        if (!c)
                        {
                        }
                        else
                        {
                            f();
                        }
                        g();
                    }
                    return false;
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveIfBodyToElseClause5()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    [||]if (c)
                    {
                        f();
                    }

                    g();
                    g();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (!c)
                    {
                    }
                    else
                    {
                        f();
                    }

                    g();
                    g();
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveIfBodyToElseClause6()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (c)
                            {
                                if (c)
                                {
                                    f();
                                    return 1;
                                }
                            }

                            f();
                            return 2;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (!c)
                            {
                            }
                            else
                            {
                                if (c)
                                {
                                    f();
                                    return 1;
                                }
                            }

                            f();
                            return 2;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveIfBodyToElseClause7()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            if (c)
                            {
                                [||]if (c)
                                {
                                    f();
                                    return 1;
                                }
                            }

                            f();
                            return 2;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            if (c)
                            {
                                if (!c)
                                {
                                }
                                else
                                {
                                    f();
                                    return 1;
                                }
                            }

                            f();
                            return 2;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40909")]
    public async Task IfWithoutElse_MoveIfBodyToElseClause8()
    {
        await TestAsync("""
            using System.Diagnostics;
            class C
            {
                private static bool IsFalse(bool val)
                {
                    {
                        [|if|] (!val)
                        {
                            return true;
                        }
                        Debug.Assert(val);
                    }
                    return false;
                }
            }
            """, """
            using System.Diagnostics;
            class C
            {
                private static bool IsFalse(bool val)
                {
                    {
                        if (val)
                        {
                        }
                        else
                        {
                            return true;
                        }
                        Debug.Assert(val);
                    }
                    return false;
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveSubsequentStatementsToIfBody1()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (!c)
                        {
                            continue;
                        }
                        // comments
                        f();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (c)
                        {
                            // comments
                            f();
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveSubsequentStatementsToIfBody2()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    while (c)
                    {
                        if (c)
                        {
                            [||]if (c)
                            {
                                continue;
                            }
                            if (c())
                                return;
                        }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    while (c)
                    {
                        if (c)
                        {
                            [||]if (!c)
                            {
                                if (c())
                                    return;
                            }
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_MoveSubsequentStatementsToIfBody3()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    while (c)
                    {
                        {
                            [||]if (c)
                            {
                                continue;
                            }
                            if (c())
                                return;
                        }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    while (c)
                    {
                        {
                            [||]if (!c)
                            {
                                if (c())
                                    return;
                            }
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_SwapIfBodyWithSubsequentStatements1()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (c)
                            break;
                        return;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (!c)
                            return;
                        break;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_SwapIfBodyWithSubsequentStatements2()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (!c)
                        {
                            return;
                        }
                        break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (c)
                        {
                            break;
                        }
                        return;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithElseClause1()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (!c)
                            return;
                        f();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        if (c)
                            f();
                        else
                            return;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithNegatedCondition1()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    [||]if (c) { }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (!c) { }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithNearmostJumpStatement1()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (c)
                        {
                            f();
                        }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        [||]if (!c)
                        {
                            continue;
                        }
                        f();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithNearmostJumpStatement2()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        {
                            [||]if (c)
                            {
                                f();
                            }
                        }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    foreach (var item in list)
                    {
                        {
                            [||]if (!c)
                            {
                                continue;
                            }
                            f();
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithNearmostJumpStatement3()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    [||]if (c)
                    {
                        f();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [||]if (!c)
                    {
                        return;
                    }
                    f();
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithNearmostJumpStatement4()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    for (;;)
                    {
                        [||]if (c)
                        {
                            break;
                        }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    for (;;)
                    {
                        [||]if (!c)
                        {
                            continue;
                        }
                        break;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithSubsequentExitPointStatement1()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (c)
                            {
                                f();
                                f();
                            }
                            break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (!c)
                            {
                                break;
                            }
                            f();
                            f();
                            break;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task IfWithoutElse_WithSubsequentExitPointStatement2()
    {
        await TestAsync("""
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (c)
                            {
                                if (c)
                                {
                                    return 1;
                                }
                            }

                            return 2;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        case 1:
                            [||]if (!c)
                            {
                                return 2;
                            }
                            if (c)
                            {
                                return 1;
                            }

                            return 2;
                    }
                }
            }
            """);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("init")]
    public async Task IfWithoutElse_InPropertyAccessors(string accessor)
    {
        await TestAsync($$"""
            class C
            {
                private bool _b;
            
                public string Prop
                {
                    {{accessor}}
                    {
                        [||]if (_b)
                        {
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                        }
                    }
                }
            }
            """, $$"""
            class C
            {
                private bool _b;
            
                public string Prop
                {
                    {{accessor}}
                    {
                        if (!_b)
                        {
                            return;
                        }
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                }
            }
            """);
    }
}
