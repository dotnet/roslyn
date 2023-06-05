// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
        public async Task TestNotEqualsConstant_CSharp8()
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
        public async Task TestNotEqualsConstant_CSharp9()
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
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotEqualsNull_ValueType_CSharp8()
        {
            var test = """
                class C
                {
                    void M(object o)
                    {
                        if ((o as string)?.Length != null)
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
        public async Task TestNotEqualsNull_ValueType_CSharp9()
        {
            var test = """
                class C
                {
                    void M(object o)
                    {
                        if ((o as string)?.Length != null)
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
        public async Task TestNotEqualsNull_ValueType2_CSharp9()
        {
            var test = """
                class C
                {
                    C X;
                    int Length;

                    void M(object o)
                    {
                        if ((o as C)?.X.Length != null)
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
        public async Task TestNotEqualsNull_ReferenceType_CSharp8()
        {
            var test = """
                class C
                {
                    string X;

                    void M(object o)
                    {
                        if ((o as C)?.X != null)
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
        public async Task TestNotEqualsNull_ReferenceType_CSharp9()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        string X;
                    
                        void M(object o)
                        {
                            if (([|o as C|])?.X != null)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        string X;

                        void M(object o)
                        {
                            if (o is C { X: not null })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotEqualsNull_ReferenceType_CSharp10()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        C Y;
                        string X;
                    
                        void M(object o)
                        {
                            if (([|o as C|])?.Y.X != null)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        C Y;
                        string X;

                        void M(object o)
                        {
                            if (o is C { Y.X: not null })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotEqualsNull_NullableType_CSharp8()
        {
            var test = """
                class C
                {
                    int? X;

                    void M(object o)
                    {
                        if ((o as C)?.X != null)
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
        public async Task TestNotEqualsNull_NullableType2_CSharp8()
        {
            var test = """
                class C
                {
                    int? X;

                    void M(object o)
                    {
                        if ((o as C)?.X != null)
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
        public async Task TestNotEqualsNull_NullableType_CSharp10()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        C Y;
                        int? X;
                    
                        void M(object o)
                        {
                            if (([|o as C|])?.Y.X != null)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        C Y;
                        int? X;

                        void M(object o)
                        {
                            if (o is C { Y.X: not null })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp10,
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
        public async Task TestIsConstantPattern1()
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
        public async Task TestIsNotConstantPattern()
        {
            var test = """
                class C
                {
                    void M(object o)
                    {
                        if ((o as string)?.Length is not 0)
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
        public async Task TestIsNullPattern()
        {
            var test = """
                class C
                {
                    void M(object o)
                    {
                        if ((o as string)?.Length is null)
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
        public async Task TestIsNotNullPattern_ValueType()
        {
            var test = """
                class C
                {
                    void M(object o)
                    {
                        if ((o as string)?.Length is not null)
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
        public async Task TestIsNotNullPattern_ValueType2()
        {
            var test = """
                class C
                {
                    C X;
                    int Length;

                    void M(object o)
                    {
                        if ((o as C)?.X.Length is not null)
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
        public async Task TestIsNotNullPattern_ReferenceType()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        string X;

                        void M(object o)
                        {
                            if (([|o as C|])?.X is not null)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        string X;

                        void M(object o)
                        {
                            if (o is C { X: not null })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestIsNotNullPattern_ReferenceType_CSharp10()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        C Y;
                        string X;

                        void M(object o)
                        {
                            if (([|o as C|])?.Y.X is not null)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        C Y;
                        string X;

                        void M(object o)
                        {
                            if (o is C { Y.X: not null })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestIsNotNullPattern_NullableValueType()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        int? X;

                        void M(object o)
                        {
                            if (([|o as C|])?.X is not null)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        int? X;

                        void M(object o)
                        {
                            if (o is C { X: not null })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestIsNotNullPattern_NullableValueType_CSharp10()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    class C
                    {
                        int? X;
                        C Y;

                        void M(object o)
                        {
                            if (([|o as C|])?.Y.X is not null)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    class C
                    {
                        int? X;
                        C Y;

                        void M(object o)
                        {
                            if (o is C { Y.X: not null })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp10,
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsTypePattern()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as Type|])?.Name is string s)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (o is Type { Name: string s })
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsNotTypePattern()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if ((o as Type)?.Name is not string s)
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsVarPattern()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as Type|])?.Name is var s)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (o is Type { Name: var s })
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsNotVarPattern()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if ({|CS8518:(o as Type)?.Name is not var s|})
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsRecursivePattern1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as Type|])?.Name is { })
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (o is Type { Name: { } })
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsRecursivePattern2()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as Type|])?.Name is { } s)
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (o is Type { Name: { } s })
                            {
                            }
                        }
                    }
                    """,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsNotRecursivePattern1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (([|o as Type|])?.Name is not { })
                            {
                            }
                        }
                    }
                    """,
                FixedCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if (o is Type { Name: not { } })
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
        public async Task TestIsNotRecursivePattern2()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    class C
                    {
                        void M(object o)
                        {
                            if ((o as Type)?.Name is not { } s)
                            {
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }
    }
}
