// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpAsAndMemberAccessDiagnosticAnalyzer,
        CSharpAsAndMemberAccessCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternMatchingForAsAndMemberAccess)]
    public partial class CSharpAsAndMemberAccessTests
    {
        [Fact]
        public async Task TestCoreCase()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length == 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: 0 })
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotInCSharp7()
        {
            var test = """
                class C
                {
                    void M(object o)
                    {
                        if ((o as string)?.Length == 0)
                        {
                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = test,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithNonConstant()
        {
            var test = """
                class C
                {
                    void M(object o, int length)
                    {
                        if ((o as string)?.Length == length)
                        {
                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = test,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithoutTest()
        {
            var test = """
                class C
                {
                    void M(object o, int length)
                    {
                        var v = (o as string)?.Length;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = test,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithNonMemberBinding1()
        {
            var test = """
                class C
                {
                    C[] X;
                    int Length;

                    void M(object o, int length)
                    {
                        if ((o as C)?.X[0].Length == 0)
                        {
                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = test,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotEquals_CSharp8()
        {
            var test = """
                class C
                {
                    void M(object o)
                    {
                        if ((o as string)?.Length != 0)
                        {
                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = test,
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotEquals_CSharp9()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length != 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: not 0 })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestGreaterThan()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length > 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: > 0 })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestGreaterThanEquals()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length >= 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: >= 0 })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestLessThan()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        int Goo;

                        void M(object o)
                        {
                            if (([|o as C|])?.Goo < 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        int Goo;

                        void M(object o)
                        {
                            if (o is C { Goo: < 0 })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestLessThanEquals()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length <= 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: <= 0 })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestIsPattern1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length is 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: 0 })
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }

        [Fact]
        public async Task TestIsPattern2()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length is not 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: not 0 })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestMemberAccess1_CSharp9()
        {
            var test = """
                class C
                {
                    C X;
                    int Length;

                    void M(object o)
                    {
                        if ((o as C)?.X.Length == 0)
                        {
                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = test,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestMemberAccess1_CSharp10()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        C X;
                        int Length;

                        void M(object o)
                        {
                            if (([|o as C|])?.X.Length == 0)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        C X;
                        int Length;

                        void M(object o)
                        {
                            if (o is C { X.Length: 0 })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestParenthesizedParent()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if ((([|o as string|])?.Length == 0) || true)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: 0 } || true)
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }

        [Fact]
        public async Task TestBinaryParent()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as string|])?.Length == 0 && true)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        void M(object o)
                        {
                            if (o is string { Length: 0 } && true)
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }
    }
}
