// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class LoopHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new LoopHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
        @"class C {
    void M() {
        {|Cursor:[|while|]|} (true) {
    if (x) {
        [|break|];
    }
    else {
        [|continue|];
    }
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
        [|while|] (true) {
    if (x) {
        {|Cursor:[|break|]|};
    }
    else {
        [|continue|];
    }
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
        [|while|] (true) {
    if (x) {
        [|break|];
    }
    else {
        {|Cursor:[|continue|]|};
    }
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
        {|Cursor:[|do|]|} {
    if (x) {
        [|break|];
    }
    else {
        [|continue|];
    }
}
[|while|] (true);
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
        [|do|] {
    if (x) {
        {|Cursor:[|break|]|};
    }
    else {
        [|continue|];
    }
}
[|while|] (true);
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_3()
        {
            Test(
        @"class C {
    void M() {
        [|do|] {
    if (x) {
        [|break|];
    }
    else {
        {|Cursor:[|continue|]|};
    }
}
[|while|] (true);
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
        [|do|] {
    if (x) {
        [|break|];
    }
    else {
        [|continue|];
    }
}
{|Cursor:[|while|]|} (true);
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_5()
        {
            Test(
        @"class C {
    void M() {
        do {
    if (x) {
        break;
    }
    else {
        continue;
    }
}
while {|Cursor:(true)|};
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_6()
        {
            Test(
@"class C {
    void M() {
[|do|] {
    if (x) {
        [|break|];
    }
    else {
        [|continue|];
    }
}
[|while|] (true);{|Cursor:|}
    }
}");
        }

        private const string SpecExample3 = @"for (int i = 0; i < 10; i++) {
    if (x) {
        break;
    }
    else {
        continue;
    }
}";

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample3_1()
        {
            Test(
        @"class C {
    void M() {
        {|Cursor:[|for|]|} (int i = 0; i < 10; i++) {
    if (x) {
        [|break|];
    }
    else {
        [|continue|];
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample3_2()
        {
            Test(
        @"class C {
    void M() {
        [|for|] (int i = 0; i < 10; i++) {
    if (x) {
        {|Cursor:[|break|];|}
    }
    else {
        [|continue|];
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample3_3()
        {
            Test(
        @"class C {
    void M() {
        [|for|] (int i = 0; i < 10; i++) {
    if (x) {
        [|break|];
    }
    else {
        {|Cursor:[|continue|];|}
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample4_1()
        {
            Test(
        @"class C {
    void M() {
        {|Cursor:[|foreach|]|} (var a in x) {
    if (x) {
        [|break|];
    }
    else {
        [|continue|];
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample4_2()
        {
            Test(
        @"class C {
    void M() {
        [|foreach|] (var a in x) {
    if (x) {
        {|Cursor:[|break|];|}
    }
    else {
        [|continue|];
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample4_3()
        {
            Test(
        @"class C {
    void M() {
        [|foreach|] (var a in x) {
    if (x) {
        [|break|];
    }
    else {
        {|Cursor:[|continue|];|}
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_1()
        {
            Test(
        @"class C {
    void M() {
        {|Cursor:[|foreach|]|} (var a in x) {
    if (a) {
        [|break|];
    }
    else {
        switch (b) {
            case 0:
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_2()
        {
            Test(
        @"class C {
    void M() {
        [|foreach|] (var a in x) {
    if (a) {
        {|Cursor:[|break|];|}
    }
    else {
        switch (b) {
            case 0:
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_3()
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
            case 0:
                while (true) {
                    {|Cursor:[|do|]|} {
                        [|break|];
                    }
                    [|while|] (false);
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_4()
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
            case 0:
                while (true) {
                    [|do|] {
                        {|Cursor:[|break|];|}
                    }
                    [|while|] (false);
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_5()
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
            case 0:
                while (true) {
                    [|do|] {
                        [|break|];
                    }
                    {|Cursor:[|while|]|} (false);
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_6()
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
                    case 0:
                        while (true) {
                            [|do|] {
                                [|break|];
                            }
                            [|while|] (false);{|Cursor:|}
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
        public void TestNestedExample1_7()
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
            case 0:
                {|Cursor:[|while|]|} (true) {
                    do {
                        break;
                    }
                    while (false);
                    [|break|];
                }
                break;
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
        public void TestNestedExample1_8()
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
            case 0:
                [|while|] (true) {
                    do {
                        break;
                    }
                    while (false);
                    {|Cursor:[|break|];|}
                }
                break;
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

        // TestNestedExample1 9-13 are in SwitchStatementHighlighterTests.cs

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_14()
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
            case 0:
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

    {|Cursor:[|for|]|} (int i = 0; i < 10; i++) {
        [|break|];
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_15()
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
            case 0:
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

    [|for|] (int i = 0; i < 10; i++) {
        {|Cursor:[|break|];|}
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_1()
        {
            Test(
        @"class C {
    void M() {
        {|Cursor:[|foreach|]|} (var a in x) {
    if (a) {
        [|continue|];
    }
    else {
        while (true) {
            do {
                continue;
            }
            while (false);
            continue;
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_2()
        {
            Test(
        @"class C {
    void M() {
        [|foreach|] (var a in x) {
    if (a) {
        {|Cursor:[|continue|];|}
    }
    else {
        while (true) {
            do {
                continue;
            }
            while (false);
            continue;
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_3()
        {
            Test(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        continue;
    }
    else {
        while (true) {
            {|Cursor:[|do|]|} {
                [|continue|];
            }
            [|while|] (false);
            continue;
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_4()
        {
            Test(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        continue;
    }
    else {
        while (true) {
            [|do|] {
                {|Cursor:[|continue|];|}
            }
            [|while|] (false);
            continue;
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_5()
        {
            Test(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        continue;
    }
    else {
        while (true) {
            [|do|] {
                [|continue|];
            }
            {|Cursor:[|while|]|} (false);
            continue;
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_6()
        {
            Test(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        continue;
    }
    else {
        while (true) {
            do {
                continue;
            }
            while {|Cursor:(false)|};
            continue;
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_7()
        {
            Test(
@"class C {
    void M() {
        foreach (var a in x) {
            if (a) {
                continue;
            }
            else {
                while (true) {
                    [|do|] {
                        [|continue|];
                    }
                    [|while|] (false);{|Cursor:|}
                    continue;
                }
            }

            for (int i = 0; i < 10; i++) {
                continue;
            }
        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_8()
        {
            Test(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        continue;
    }
    else {
        {|Cursor:[|while|]|} (true) {
            do {
                continue;
            }
            while (false);
            [|continue|];
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_9()
        {
            Test(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        continue;
    }
    else {
        [|while|] (true) {
            do {
                continue;
            }
            while (false);
            {|Cursor:[|continue|];|}
        }
    }

    for (int i = 0; i < 10; i++) {
        continue;
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_10()
        {
            Test(
        @"class C {
    void M() {
        foreach (var a in x) {
    if (a) {
        continue;
    }
    else {
        while (true) {
            do {
                continue;
            }
            while (false);
            continue;
        }
    }

    {|Cursor:[|for|]|} (int i = 0; i < 10; i++) {
        [|continue|];
    }
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample2_11()
        {
            Test(
@"class C {
    void M() {
        foreach (var a in x) {
            if (a) {
                continue;
            }
            else {
                while (true) {
                    do {
                        continue;
                    }
                    while (false);
                    continue;
                }
            }

            [|for|] (int i = 0; i < 10; i++) {
                {|Cursor:[|continue|];|}
            }
        }
    }
}
");
        }
    }
}
