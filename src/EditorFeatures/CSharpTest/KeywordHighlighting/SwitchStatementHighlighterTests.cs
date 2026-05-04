// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting;

[Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
public sealed class SwitchStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(SwitchStatementHighlighter);

    [Fact]
    public Task TestExample1_OnSwitchKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|switch|]|} (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            [|break|];
                        [|case|] 1:
                            CaseOne();
                            [|break|];
                        [|default|]:
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_OnCaseKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        {|Cursor:[|case|]|} 0:
                            CaseZero();
                            [|break|];
                        [|case|] 1:
                            CaseOne();
                            [|break|];
                        [|default|]:
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_AfterCaseColon()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        [|case|] 0:{|Cursor:|}
                            CaseZero();
                            [|break|];
                        [|case|] 1:
                            CaseOne();
                            [|break|];
                        [|default|]:
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_NotOnCaseValue()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    switch (i)
                    {
                        case {|Cursor:0|}:
                            CaseZero();
                            break;
                        case 1:
                            CaseOne();
                            break;
                        default:
                            CaseOthers();
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_OnBreakStatement()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            {|Cursor:[|break|];|}
                        [|case|] 1:
                            CaseOne();
                            [|break|];
                        [|default|]:
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_OnDefaultLabel()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            [|break|];
                        [|case|] 1:
                            CaseOne();
                            [|break|];
                        {|Cursor:[|default|]:|}
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_OnGotoCaseKeywords()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            {|Cursor:[|goto case|]|} 1;
                        [|case|] 1:
                            CaseOne();
                            [|goto default|];
                        [|default|]:
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_AfterGotoCaseSemicolon()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            [|goto case|] 1;{|Cursor:|}
                        [|case|] 1:
                            CaseOne();
                            [|goto default|];
                        [|default|]:
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_NotOnGotoCaseValue()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    switch (i)
                    {
                        case 0:
                            CaseZero();
                            goto case {|Cursor:1|};
                        case 1:
                            CaseOne();
                            goto default;
                        default:
                            CaseOthers();
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_OnGotoDefaultStatement()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            [|goto case|] 1;
                        [|case|] 1:
                            CaseOne();
                            {|Cursor:[|goto default|];|}
                        [|default|]:
                            CaseOthers();
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_OnSwitchKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            {|Cursor:[|switch|]|} (b)
                            {
                                [|case|] 0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    [|break|];
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_OnCaseKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            [|switch|] (b)
                            {
                                {|Cursor:[|case|]|} 0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    [|break|];
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_NotBeforeCaseValue()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            switch (b)
                            {
                                case {|Cursor:|}0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_AfterCaseColon()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            [|switch|] (b)
                            {
                                [|case|] 0:{|Cursor:|}
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    [|break|];
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedExample1_OnBreakStatement()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var a in x)
                    {
                        if (a)
                        {
                            break;
                        }
                        else
                        {
                            [|switch|] (b)
                            {
                                [|case|] 0:
                                    while (true)
                                    {
                                        do
                                        {
                                            break;
                                        }
                                        while (false);
                                        break;
                                    }

                                    {|Cursor:[|break|];|}
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task Bug3483()
        => TestAsync(
            """
            class C
            {
                static void M()
                {
                    [|switch|] (2)
                    {
                        [|case|] 1:
                            {|Cursor:[|goto|]|}
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithUnrelatedGotoStatement_OnGotoCaseGotoKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    label:
                    [|switch|] (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            [|{|Cursor:goto|} case|] 1;
                        [|case|] 1:
                            CaseOne();
                            [|goto default|];
                        [|default|]:
                            CaseOthers();
                            goto label;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithUnrelatedGotoStatement_OnGotoDefaultGotoKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    label:
                    [|switch|] (i)
                    {
                        [|case|] 0:
                            CaseZero();
                            [|goto case|] 1;
                        [|case|] 1:
                            CaseOne();
                            [|{|Cursor:goto|} default|];
                        [|default|]:
                            CaseOthers();
                            goto label;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithUnrelatedGotoStatement_NotOnGotoLabelGotoKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    label:
                    switch (i)
                    {
                        case 0:
                            CaseZero();
                            goto case 1;
                        case 1:
                            CaseOne();
                            goto default;
                        default:
                            CaseOthers();
                            {|Cursor:goto|} label;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithNestedStatements_OnSwitchKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|switch|]|} (i)
                    {
                        [|case|] 0:
                        {
                            CaseZero();
                            [|goto case|] 1;
                        }
                        [|case|] 1:
                        {
                            CaseOne();
                            [|goto default|];
                        }
                        [|default|]:
                        {
                            CaseOthers();
                            [|break|];
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithNestedStatements_OnBreakKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (i)
                    {
                        [|case|] 0:
                        {
                            CaseZero();
                            [|goto case|] 1;
                        }
                        [|case|] 1:
                        {
                            CaseOne();
                            [|goto default|];
                        }
                        [|default|]:
                        {
                            CaseOthers();
                            {|Cursor:[|break|]|};
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithGotoCaseAndBreakInsideLoop_OnSwitchKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|switch|]|} (true)
                    {
                        [|case|] true:
                            while (true)
                            {
                                [|goto case|] true;
                                break;

                                switch (true)
                                {
                                    case true:
                                        goto case true;
                                        break;
                                }
                            }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithGotoCaseAndBreakInsideLoop_OnGotoCaseGotoKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (true)
                    {
                        [|case|] true:
                            while (true)
                            {
                                [|{|Cursor:goto|} case|] true;
                                break;

                                switch (true)
                                {
                                    case true:
                                        goto case true;
                                        break;
                                }
                            }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25039")]
    public Task TestWithGotoCaseAndBreakInsideLoop_NotOnLoopBreakKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case true:
                            while (true)
                            {
                                goto case true;
                                {|Cursor:break|};

                                switch (true)
                                {
                                    case true:
                                        goto case true;
                                        break;
                                }
                            }
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithWhenClauseAndPattern_OnSwitchKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|switch|]|} (true)
                    {
                        [|case|] true when true:
                            [|break|];
                        [|case|] bool b:
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithWhenClauseAndPattern_NotOnWhenKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case true {|Cursor:when|} true:
                            break;
                        case bool b:
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithWhenClauseAndPattern_AfterWhenCaseColon()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (true)
                    {
                        [|case|] true when true:{|Cursor:|}
                            [|break|];
                        [|case|] bool b:
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithWhenClauseAndPattern_AfterPatternCaseColon()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    [|switch|] (true)
                    {
                        [|case|] true when true:
                            [|break|];
                        [|case|] bool b:{|Cursor:|}
                            [|break|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithWhenClauseAndPattern_NotOnWhenValue()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case true when {|Cursor:true|}:
                            break;
                        case bool b:
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithWhenClauseAndPattern_NotOnPattern()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case true when true:
                            break;
                        case {|Cursor:bool b|}:
                            break;
                    }
                }
            }
            """);
}
