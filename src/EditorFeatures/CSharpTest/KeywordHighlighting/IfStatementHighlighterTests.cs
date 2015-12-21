// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndSingleElse1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndSingleElse2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndElseIfAndElse1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndElseIfAndElse2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndElseIfAndElse3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithElseIfOnDifferentLines1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithElseIfOnDifferentLines2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithElseIfOnDifferentLines3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithElseIfOnDifferentLines4()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndElseIfAndElseTouching1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndElseIfAndElseTouching2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestIfStatementWithIfAndElseIfAndElseTouching3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExtraSpacesBetweenElseAndIf1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExtraSpacesBetweenElseAndIf2()
        {
            await TestAsync(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExtraSpacesBetweenElseAndIf3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestCommentBetweenElseIf1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestCommentBetweenElseIf2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestCommentBetweenElseIf3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestCommentBetweenElseIf4()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedIfDoesNotHighlight1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedIfDoesNotHighlight2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedIfDoesNotHighlight3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_1()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_3()
        {
            await TestAsync(
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
