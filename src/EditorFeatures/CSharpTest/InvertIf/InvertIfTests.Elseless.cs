// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertIf
{
    public partial class InvertIfTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveIfBodyToElseClause1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveIfBodyToElseClause2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveIfBodyToElseClause3()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveIfBodyToElseClause4()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveIfBodyToElseClause5()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveIfBodyToElseClause6()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveIfBodyToElseClause7()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveSubsequentStatementsToIfBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveSubsequentStatementsToIfBody2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_MoveSubsequentStatementsToIfBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_SwapIfBodyWithSubsequentStatements1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_SwapIfBodyWithSubsequentStatements2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithElseClause1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithNegatedCondition1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [||]if (c) { }
    }
}",
@"class C
{
    void M()
    {
        if (!c) { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithNearmostJumpStatement1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithNearmostJumpStatement2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithNearmostJumpStatement3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [||]if (c)
        {
            f();
        }
    }
}",
@"class C
{
    void M()
    {
        [||]if (!c)
        {
            return;
        }
        f();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithNearmostJumpStatement4()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithSubsequentExitPointStatement1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task IfWithoutElse_WithSubsequentExitPointStatement2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }
    }
}
