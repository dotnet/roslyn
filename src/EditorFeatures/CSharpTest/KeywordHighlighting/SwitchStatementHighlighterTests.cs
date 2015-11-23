// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class SwitchStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new SwitchStatementHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
        @"class C {
    void M() {
        {|Cursor:[|switch|]|} (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_2()
        {
            await TestAsync(
        @"class C {
    void M() {
        [|switch|] (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_3()
        {
            await TestAsync(
@"class C {
    void M() {
        [|switch|] (i) {
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
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_4()
        {
            await TestAsync(
        @"class C {
    void M() {
        switch (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_5()
        {
            await TestAsync(
        @"class C {
    void M() {
        [|switch|] (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_6()
        {
            await TestAsync(
        @"class C {
    void M() {
        [|switch|] (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_1()
        {
            await TestAsync(
        @"class C {
    void M() {
        [|switch|] (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_2()
        {
            await TestAsync(
@"class C {
    void M() {
        [|switch|] (i) {
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
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_3()
        {
            await TestAsync(
        @"class C {
    void M() {
        switch (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_4()
        {
            await TestAsync(
        @"class C {
    void M() {
        [|switch|] (i) {
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_9()
        {
            await TestAsync(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        break;
    }
    else {
        {|Cursor:[|switch|]|} (b) {
            [|case|] 0:
                while (true) {
                    do {
                        break;
                    }
                    while (false);
                    break;
                }
                [|break|];
        }
    }

    for (int i = 0; i < 10; i++) {
        break;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_10()
        {
            await TestAsync(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        break;
    }
    else {
        [|switch|] (b) {
            {|Cursor:[|case|]|} 0:
                while (true) {
                    do {
                        break;
                    }
                    while (false);
                    break;
                }
                [|break|];
        }
    }

    for (int i = 0; i < 10; i++) {
        break;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_11()
        {
            await TestAsync(
@"class C {
    void M() {
        foreach (var a in x) {
            if (a) {
                break;
            }
            else {
                switch (b) {
                    case {|Cursor:|}0:
                        while (true) {
                            do {
                                break;
                            }
                            while (false);
                            break;
                        }
                        break;
                }
            }

            for (int i = 0; i < 10; i++) {
                break;
            }
        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_12()
        {
            await TestAsync(
@"class C {
    void M() {
        foreach (var a in x) {
            if (a) {
                break;
            }
            else {
                [|switch|] (b) {
                    [|case|] 0:{|Cursor:|}
                        while (true) {
                            do {
                                break;
                            }
                            while (false);
                            break;
                        }
                        [|break|];
                }
            }

            for (int i = 0; i < 10; i++) {
                break;
            }
        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_13()
        {
            await TestAsync(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        break;
    }
    else {
        [|switch|] (b) {
            [|case|] 0:
                while (true) {
                    do {
                        break;
                    }
                    while (false);
                    break;
                }
                {|Cursor:[|break|];|}
        }
    }

    for (int i = 0; i < 10; i++) {
        break;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task Bug3483()
        {
            await TestAsync(
        @"class C
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
");
        }
    }
}
