// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    [Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
    public class ConditionalPreprocessorHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override Type GetHighlighterType()
            => typeof(ConditionalPreprocessorHighlighter);

        [Fact]
        public async Task TestExample1_1()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample1_2()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample2_1()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample2_2()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample2_3()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample4_1()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample4_2()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample4_3()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }

        [Fact]
        public async Task TestExample4_4()
        {
            await TestAsync(
                """
                class C
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
                }
                """);
        }
    }
}
