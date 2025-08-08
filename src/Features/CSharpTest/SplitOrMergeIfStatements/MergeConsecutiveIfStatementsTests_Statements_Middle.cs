// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements;

public sealed partial class MergeConsecutiveIfStatementsTests
{
    [Fact]
    public async Task MergedIntoStatementOnMiddleIfMergableWithNextOnly()
    {
        const string Initial =
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                        return;
                    else
                        return;
                    [||]if (b)
                        return;
                    if (c)
                        return;
                }
            }
            """;
        await TestActionCountAsync(Initial, 1);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                        return;
                    else
                        return;
                    if (b || c)
                        return;
                }
            }
            """);
    }

    [Fact]
    public async Task MergedIntoStatementOnMiddleIfMergableWithPreviousOnly()
    {
        const string Initial =
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                        return;
                    [||]if (b)
                        return;
                    else
                        return;
                    if (c)
                        return;
                }
            }
            """;
        await TestActionCountAsync(Initial, 1);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a || b)
                        return;
                    else
                        return;
                    if (c)
                        return;
                }
            }
            """);
    }

    [Fact]
    public async Task MergedIntoStatementOnMiddleIfMergableWithBoth()
    {
        const string Initial =
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                        return;
                    [||]if (b)
                        return;
                    if (c)
                        return;
                }
            }
            """;
        await TestActionCountAsync(Initial, 2);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a || b)
                        return;
                    if (c)
                        return;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                        return;
                    if (b || c)
                        return;
                }
            }
            """, index: 1);
    }
}
