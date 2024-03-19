// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseExplicitTupleName;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExplicitTupleName
{
    using VerifyCS = CSharpCodeFixVerifier<
        UseExplicitTupleNameDiagnosticAnalyzer,
        UseExplicitTupleNameCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
    public class UseExplicitTupleNameTests
    {
        [Fact]
        public async Task TestNamedTuple1()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.[|Item1|];
                    }
                }
                """, """
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.i;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInArgument()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        Goo(v1.[|Item1|]);
                    }

                    void Goo(int i) { }
                }
                """, """
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        Goo(v1.i);
                    }

                    void Goo(int i) { }
                }
                """);
        }

        [Fact]
        public async Task TestNamedTuple2()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.[|Item2|];
                    }
                }
                """, """
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.s;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingOnMatchingName1()
        {
            var code = """
                class C
                {
                    void M()
                    {
                        (int, string s) v1 = default((int, string));
                        var v2 = v1.Item1;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestMissingOnMatchingName2()
        {
            var code = """
                class C
                {
                    void M()
                    {
                        (int Item1, string s) v1 = default((int, string));
                        var v2 = v1.Item1;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestWrongCasing()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                class C
                {
                    void M()
                    {
                        (int item1, string s) v1 = default((int, string));
                        var v2 = v1.[|Item1|];
                    }
                }
                """, """
                class C
                {
                    void M()
                    {
                        (int item1, string s) v1 = default((int, string));
                        var v2 = v1.item1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.[|Item1|];
                        var v3 = v1.[|Item2|];
                    }
                }
                """, """
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.i;
                        var v3 = v1.s;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll2()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                class C
                {
                    void M()
                    {
                        (int i, int s) v1 = default((int, int));
                        v1.[|Item1|] = v1.[|Item2|];
                    }
                }
                """, """
                class C
                {
                    void M()
                    {
                        (int i, int s) v1 = default((int, int));
                        v1.i = v1.s;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFalseOptionImplicitTuple()
        {
            var code = """
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.Item1;
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                Options =
                {
                    { CodeStyleOptions2.PreferExplicitTupleNames, false, NotificationOption2.Warning }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFalseOptionExplicitTuple()
        {
            var code = """
                class C
                {
                    void M()
                    {
                        (int i, string s) v1 = default((int, string));
                        var v2 = v1.i;
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                Options =
                {
                    { CodeStyleOptions2.PreferExplicitTupleNames, false, NotificationOption2.Warning }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOnRestField()
        {
            var valueTuple8 = """
                namespace System
                {
                    public struct ValueTuple<T1>
                    {
                        public T1 Item1;

                        public ValueTuple(T1 item1)
                        {
                        }
                    }
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;

                        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
                        {
                        }
                    }
                }
                """;
            var code = """
                class C
                {
                    void M()
                    {
                        (int, int, int, int, int, int, int, int) x = default;
                        _ = x.Rest;
                    }
                }
                """ + valueTuple8;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp11
            }.RunAsync();
        }
    }
}
