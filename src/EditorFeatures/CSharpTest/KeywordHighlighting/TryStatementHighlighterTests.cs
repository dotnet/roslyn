// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class TryStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new TryStatementHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
        @"class C {
    void M() {
        {|Cursor:[|try|]|} {
    try {
    }
    catch (Exception e) {
    }
}
[|finally|] {
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
        try {
    {|Cursor:[|try|]|} {
    }
    [|catch|] (Exception e) {
    }
}
finally {
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
        try {
    [|try|] {
    }
    {|Cursor:[|catch|]|} (Exception e) {
    }
}
finally {
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_4()
        {
            Test(
        @"class C {
    void M() {
        [|try|] {
    try {
    }
    catch (Exception e) {
    }
}
{|Cursor:[|finally|]|} {
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExceptionFilter1()
        {
            Test(
        @"class C {
    void M() {
        try {
    {|Cursor:[|try|]|} {
    }
    [|catch|] (Exception e) [|when|] (e != null) {
    }
}
finally {
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExceptionFilter2()
        {
            Test(
        @"class C {
    void M() {
        try {
    [|try|] {
    }
    {|Cursor:[|catch|]|} (Exception e) [|when|] (e != null) {
    }
}
finally {
}
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExceptionFilter3()
        {
            Test(
        @"class C {
    void M() {
        try {
    [|try|] {
    }
    [|catch|] (Exception e) {|Cursor:[|when|]|} (e != null) {
    }
}
finally {
}
    }
}
");
        }
    }
}
