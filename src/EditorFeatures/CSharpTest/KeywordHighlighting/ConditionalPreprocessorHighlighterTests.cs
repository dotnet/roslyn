// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class ConditionalPreprocessorHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new ConditionalPreprocessorHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
@"class C {
    void M() {
        #define Debug
#undef Trace
class PurchaseTransaction
{
    void Commit() {
        {|Cursor:[|#if|]|} Debug
            CheckConsistency();
            #if Trace
                WriteToLog(this.ToString());
            #else
                Exit();
            #endif
        [|#endif|]
        CommitHelper();
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
        #define Debug
#undef Trace
class PurchaseTransaction
{
    void Commit() {
        [|#if|] Debug
            CheckConsistency();
            #if Trace
                WriteToLog(this.ToString());
            #else
                Exit();
            #endif
        {|Cursor:[|#endif|]|}
        CommitHelper();
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
        #define Debug
#undef Trace
class PurchaseTransaction
{
    void Commit() {
        #if Debug
            CheckConsistency();
            {|Cursor:[|#if|]|} Trace
                WriteToLog(this.ToString());
            [|#else|]
                Exit();
            [|#endif|]
        #endif
        CommitHelper();
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
        #define Debug
#undef Trace
class PurchaseTransaction
{
    void Commit() {
        #if Debug
            CheckConsistency();
            [|#if|] Trace
                WriteToLog(this.ToString());
            {|Cursor:[|#else|]|}
                Exit();
            [|#endif|]
        #endif
        CommitHelper();
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
        #define Debug
#undef Trace
class PurchaseTransaction
{
    void Commit() {
        #if Debug
            CheckConsistency();
            [|#if|] Trace
                WriteToLog(this.ToString());
            [|#else|]
                Exit();
            {|Cursor:[|#endif|]|}
        #endif
        CommitHelper();
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
        #define Foo1
#define Foo2

{|Cursor:[|#if|]|} Foo1

[|#elif|] Foo2

[|#else|]

[|#endif|]
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
        #define Foo1
#define Foo2

[|#if|] Foo1

{|Cursor:[|#elif|]|} Foo2

[|#else|]

[|#endif|]
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
        #define Foo1
#define Foo2

[|#if|] Foo1

[|#elif|] Foo2

{|Cursor:[|#else|]|}

[|#endif|]
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample4_4()
        {
            Test(
        @"class C {
    void M() {
        #define Foo1
#define Foo2

[|#if|] Foo1

[|#elif|] Foo2

[|#else|]

{|Cursor:[|#endif|]|}
    }
}
");
        }
    }
}
