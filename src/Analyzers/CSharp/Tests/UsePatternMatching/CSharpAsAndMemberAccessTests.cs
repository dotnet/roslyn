// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpAsAndMemberAccessDiagnosticAnalyzer,
    CSharpAsAndMemberAccessCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternMatchingForAsAndMemberAccess)]
public sealed partial class CSharpAsAndMemberAccessTests
{
    [Fact]
    public Task TestCoreCase()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotInCSharp7()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length == 0)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact]
    public Task TestNotWithNonConstant()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o, int length)
                {
                    if ((o as string)?.Length == length)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact]
    public Task TestNotWithoutTest()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o, int length)
                {
                    var v = (o as string)?.Length;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact]
    public Task TestNotWithNonMemberBinding1()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsConstant_CSharp8()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length != 0)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsConstant_CSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length != 0)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsNull_ValueType_CSharp8()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length != null)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsNull_ValueType_CSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length != null)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsNull_ValueType2_CSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsNull_ReferenceType_CSharp8()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsNull_ReferenceType_CSharp9()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotEqualsNull_ReferenceType_CSharp10()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotEqualsNull_NullableType_CSharp8()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsNull_NullableType2_CSharp8()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestNotEqualsNull_NullableType_CSharp10()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestGreaterThan()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestGreaterThanEquals()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestLessThan()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestLessThanEquals()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIsConstantPattern1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIsNotConstantPattern()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length is not 0)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestIsNullPattern()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length is null)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestIsNotNullPattern_ValueType()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(object o)
                {
                    if ((o as string)?.Length is not null)
                    {
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestIsNotNullPattern_ValueType2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestIsNotNullPattern_ReferenceType()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIsNotNullPattern_ReferenceType_CSharp10()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIsNotNullPattern_NullableValueType()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIsNotNullPattern_NullableValueType_CSharp10()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMemberAccess1_CSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestMemberAccess1_CSharp10()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestParenthesizedParent()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestBinaryParent()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsTypePattern()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsNotTypePattern()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsVarPattern()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsNotVarPattern()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsRecursivePattern1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsRecursivePattern2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsNotRecursivePattern1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67010")]
    public Task TestIsNotRecursivePattern2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76372")]
    public Task TestPullNotUpwards()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class D
                {
                    void Goo(object metadata)
                    {
                        if (([|metadata as C|])?.P is not E { P: G.A, M: string text })
                        {
                            return;
                        }

                        Console.WriteLine(text);
                    }
                }
                class C
                {
                    public object P { get; set; }
                }

                public class E
                {
                    public G P { get; set; }

                    public object M { get; set; }
                }

                public enum G
                {
                    A
                }
                """,
            FixedCode = """
                using System;
            
                class D
                {
                    void Goo(object metadata)
                    {
                        if (metadata is not C { P: E { P: G.A, M: string text } })
                        {
                            return;
                        }
            
                        Console.WriteLine(text);
                    }
                }
                class C
                {
                    public object P { get; set; }
                }
            
                public class E
                {
                    public G P { get; set; }
            
                    public object M { get; set; }
                }
            
                public enum G
                {
                    A
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
}
