// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void TestExample1_1()
        {
            Test(
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
        public void TestExample1_2()
        {
            Test(
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
        public void TestExample1_3()
        {
            Test(
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
        public void TestExample1_4()
        {
            Test(
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
        public void TestExample1_5()
        {
            Test(
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
        public void TestExample1_6()
        {
            Test(
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
        public void TestExample2_1()
        {
            Test(
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
        public void TestExample2_2()
        {
            Test(
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
        public void TestExample2_3()
        {
            Test(
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
        public void TestExample2_4()
        {
            Test(
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
        public void TestNestedExample1_9()
        {
            Test(
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
        public void TestNestedExample1_10()
        {
            Test(
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
        public void TestNestedExample1_11()
        {
            Test(
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
        public void TestNestedExample1_12()
        {
            Test(
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
        public void TestNestedExample1_13()
        {
            Test(
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
        public void Bug3483()
        {
            Test(
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
