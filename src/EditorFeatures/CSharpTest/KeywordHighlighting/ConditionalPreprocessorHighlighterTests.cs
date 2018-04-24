// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class ConditionalPreprocessorHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new ConditionalPreprocessorHighlighter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {


        #define Debug
#undef Trace
    class PurchaseTransaction
    {
        void Commit()
        {
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_2()
        {
            await TestAsync(
@"class C
{
    void M()
    {


        #define Debug
#undef Trace
    class PurchaseTransaction
    {
        void Commit()
        {
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {


        #define Debug
#undef Trace
    class PurchaseTransaction
    {
        void Commit()
        {
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_2()
        {
            await TestAsync(
@"class C
{
    void M()
    {


        #define Debug
#undef Trace
    class PurchaseTransaction
    {
        void Commit()
        {
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_3()
        {
            await TestAsync(
@"class C
{
    void M()
    {


        #define Debug
#undef Trace
    class PurchaseTransaction
    {
        void Commit()
        {
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample4_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
#define Goo1
#define Goo2

{|Cursor:[|#if|]|} Goo1

[|#elif|] Goo2

[|#else|]

[|#endif|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample4_2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
#define Goo1
#define Goo2

[|#if|] Goo1

{|Cursor:[|#elif|]|} Goo2

[|#else|]

[|#endif|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample4_3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
#define Goo1
#define Goo2

[|#if|] Goo1

[|#elif|] Goo2

{|Cursor:[|#else|]|}

[|#endif|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample4_4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
#define Goo1
#define Goo2

[|#if|] Goo1

[|#elif|] Goo2

[|#else|]

{|Cursor:[|#endif|]|}
    }
}");
        }
    }
}
