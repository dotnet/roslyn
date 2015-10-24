// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class IfStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new IfStatementHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndSingleElse1()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        {|Cursor:[|if|]|} (a < 5)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndSingleElse2()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        {|Cursor:[|else|]|}
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndElseIfAndElse1()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        {|Cursor:[|if|]|} (a < 5)
        {
            // blah
        }
        [|else if|] (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndElseIfAndElse2()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        {|Cursor:[|else if|]|} (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndElseIfAndElse3()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        [|else if|] (a == 10)
        {
            // blah
        }
        {|Cursor:[|else|]|}
        {
            // blah
        }
    }
}
");
        }

        private const string Code3 = @"
public class C
{
    public void Foo()
    {
        int a = 10;
        if (a < 5)
        {
            // blah
        }
        else 
        if (a == 10)
        {
            // blah
        }
        else
        {
            // blah
        }
    }
}";

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithElseIfOnDifferentLines1()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        {|Cursor:[|if|]|} (a < 5)
        {
            // blah
        }
        [|else|] 
        [|if|] (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithElseIfOnDifferentLines2()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        {|Cursor:[|else|]|} 
        [|if|] (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithElseIfOnDifferentLines3()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        [|else|] 
        {|Cursor:[|if|]|} (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithElseIfOnDifferentLines4()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        [|else|] 
        [|if|] (a == 10)
        {
            // blah
        }
        {|Cursor:[|else|]|}
        {
            // blah
        }
    }
}
");
        }

        private const string Code4 = @"
public class C
{
    public void Foo()
    {
        int a = 10;
        if(a < 5) {
            // blah
        }
        else if(a == 10) {
            // blah
        }
        else{
            // blah
        }
    }
}";

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndElseIfAndElseTouching1()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        {|Cursor:[|if|]|}(a < 5) {
            // blah
        }
        [|else if|](a == 10) {
            // blah
        }
        [|else|]{
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndElseIfAndElseTouching2()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|](a < 5) {
            // blah
        }
        {|Cursor:[|else if|]|}(a == 10) {
            // blah
        }
        [|else|]{
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestIfStatementWithIfAndElseIfAndElseTouching3()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|](a < 5) {
            // blah
        }
        [|else if|](a == 10) {
            // blah
        }
        {|Cursor:[|else|]|}{
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExtraSpacesBetweenElseAndIf1()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        {|Cursor:[|if|]|} (a < 5) {
            // blah
        }
        [|else      if|] (a == 10) {
            // blah
        }
        [|else|] {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExtraSpacesBetweenElseAndIf2()
        {
            Test(@"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5) {
            // blah
        }
        {|Cursor:[|else      if|]|} (a == 10) {
            // blah
        }
        [|else|] {
            // blah
        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExtraSpacesBetweenElseAndIf3()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5) {
            // blah
        }
        [|else      if|] (a == 10) {
            // blah
        }
        {|Cursor:[|else|]|} {
            // blah
        }
    }
}
");
        }

        private const string Code6 = @"
public class C
{
    public void Foo()
    {
        int a = 10;
        if (a < 5)
        {
            // blah
        }
        else /* test */ if (a == 10)
        {
            // blah
        }
        else
        {
            // blah
        }
    }
}";

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestCommentBetweenElseIf1()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        {|Cursor:[|if|]|} (a < 5)
        {
            // blah
        }
        [|else|] /* test */ [|if|] (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestCommentBetweenElseIf2()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        {|Cursor:[|else|]|} /* test */ [|if|] (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestCommentBetweenElseIf3()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        [|else|] /* test */ {|Cursor:[|if|]|} (a == 10)
        {
            // blah
        }
        [|else|]
        {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestCommentBetweenElseIf4()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        [|if|] (a < 5)
        {
            // blah
        }
        [|else|] /* test */ [|if|] (a == 10)
        {
            // blah
        }
        {|Cursor:[|else|]|}
        {
            // blah
        }
    }
}
");
        }

        private const string Code7 = @"
public class C
{
    public void Foo()
    {
        int a = 10;
        int b = 15;
        if (a < 5) {
            // blah
            if (b < 15)
                b = 15;
            else
                b = 14;
        }
        else if (a == 10) {
            // blah
        }
        else {
            // blah
        }
    }
}";

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedIfDoesNotHighlight1()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        int b = 15;
        {|Cursor:[|if|]|} (a < 5) {
            // blah
            if (b < 15)
                b = 15;
            else
                b = 14;
        }
        [|else if|] (a == 10) {
            // blah
        }
        [|else|] {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedIfDoesNotHighlight2()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        int b = 15;
        [|if|] (a < 5) {
            // blah
            if (b < 15)
                b = 15;
            else
                b = 14;
        }
        {|Cursor:[|else if|]|} (a == 10) {
            // blah
        }
        [|else|] {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedIfDoesNotHighlight3()
        {
            Test(
        @"
public class C
{
    public void Foo()
    {
        int a = 10;
        int b = 15;
        [|if|] (a < 5) {
            // blah
            if (b < 15)
                b = 15;
            else
                b = 14;
        }
        [|else if|] (a == 10) {
            // blah
        }
        {|Cursor:[|else|]|} {
            // blah
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
        @"class C {
    void M() {
        {|Cursor:[|if|]|} (x) {
    if (y) {
        F();
    }
    else if (z) {
        G();
    }
    else {
        H();
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
        if (x) {
    {|Cursor:[|if|]|} (y) {
        F();
    }
    [|else if|] (z) {
        G();
    }
    [|else|] {
        H();
    }
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
        if (x) {
    [|if|] (y) {
        F();
    }
    {|Cursor:[|else if|]|} (z) {
        G();
    }
    [|else|] {
        H();
    }
}
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
        if (x) {
    [|if|] (y) {
        F();
    }
    [|else if|] (z) {
        G();
    }
    {|Cursor:[|else|]|} {
        H();
    }
}
    }
}
");
        }
    }
}
