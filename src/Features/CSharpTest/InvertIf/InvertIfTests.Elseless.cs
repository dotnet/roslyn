// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertIf;

public sealed partial class InvertIfTests
{
    [Fact]
    public Task IfWithoutElse_MoveIfBodyToElseClause1()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveIfBodyToElseClause2()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveIfBodyToElseClause3()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveIfBodyToElseClause4()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveIfBodyToElseClause5()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveIfBodyToElseClause6()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveIfBodyToElseClause7()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40909")]
    public Task IfWithoutElse_MoveIfBodyToElseClause8()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveSubsequentStatementsToIfBody1()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveSubsequentStatementsToIfBody2()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_MoveSubsequentStatementsToIfBody3()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73917")]
    public Task IfWithoutElse_MoveSubsequentStatementsToIfBody4()
        => TestAsync("""
            public void SomeMethod()
            {
                object something = null;

                [||]if (something == null)
                {
                    return;
                }

                #region A region
                something = new object();
                #endregion
            }
            """, """
            public void SomeMethod()
            {
                object something = null;

                if (something != null)
                {
                    #region A region
                    something = new object();
                    #endregion
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73917")]
    public Task IfWithoutElse_MoveSubsequentStatementsToIfBody5()
        => TestAsync("""
            switch (o)
            {
                case 1:
                    something = new object();
                    
                    [||]if (something == null)
                    {
                        return;
                    }
                    
                    #region A region
                    something = new object();
                    #endregion
                    break;
            }
            """, """
            switch (o)
            {
                case 1:
                    something = new object();

                    if (something != null)
                    {
                        #region A region
                        something = new object();
                        #endregion
                        break;
                    }

                    return;
            }
            """);

    [Fact]
    public Task IfWithoutElse_SwapIfBodyWithSubsequentStatements1()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_SwapIfBodyWithSubsequentStatements2()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithElseClause1()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithNegatedCondition1()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithNearmostJumpStatement1()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithNearmostJumpStatement2()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithNearmostJumpStatement3()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithNearmostJumpStatement4()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithSubsequentExitPointStatement1()
        => TestAsync("""
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

    [Fact]
    public Task IfWithoutElse_WithSubsequentExitPointStatement2()
        => TestAsync("""
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

    [Theory]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("init")]
    public Task IfWithoutElse_InPropertyAccessors(string accessor)
        => TestAsync($$"""
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
