// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.PreferNullLiteral,
    Roslyn.Diagnostics.CSharp.Analyzers.PreferNullLiteralCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class PreferNullLiteralTests
    {
        [Theory]
        [InlineData("default")]
        [InlineData("default(object)")]
        public Task PreferNullLiteral_ClassAsync(string defaultValueExpression)
            => VerifyCS.VerifyCodeFixAsync($$"""
                class Type
                {
                    object Method()
                    {
                        return [|{{defaultValueExpression}}|];
                    }
                }
                """, """
                class Type
                {
                    object Method()
                    {
                        return null;
                    }
                }
                """);

        [Fact]
        public async Task UnresolvedTypeAsync()
        {
            var source = """
                class Type
                {
                    void Method()
                    {
                        {|CS0411:Method2|}(default);
                    }

                    void Method2<T>(T value)
                    {
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Theory]
        [InlineData("default", "null")]
        [InlineData("default(object)", "null")]
        [InlineData("default(object?)", "null")]
        public Task ReturnFromNullableContextAsync(string defaultValueExpression, string fixedExpression)
            => new VerifyCS.Test
            {
                TestCode = $$"""
                #nullable enable

                class Type
                {
                    object Method()
                    {
                        return [|{{defaultValueExpression}}|]!;
                    }
                }
                """,
                FixedCode = $$"""
                #nullable enable

                class Type
                {
                    object Method()
                    {
                        return {{fixedExpression}}!;
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();

        [Theory]
        [InlineData("[|default(object)|]!", "((object?)null)!")]
        [InlineData("[|default(object?)|]!", "((object?)null)!")]
        [InlineData("[|default(object)|]", "(object?)null")]
        [InlineData("[|default(object?)|]", "(object?)null")]
        public Task InvocationInNullableContextAsync(string defaultValueExpression, string fixedExpression)
            => new VerifyCS.Test
            {
                TestCode = $$"""
                #nullable enable

                class Type
                {
                    void Method()
                    {
                        Method2({{defaultValueExpression}});
                    }

                    void Method2<T>(T value)
                    {
                    }
                }
                """,
                FixedCode = $$"""
                #nullable enable

                class Type
                {
                    void Method()
                    {
                        Method2({{fixedExpression}});
                    }

                    void Method2<T>(T value)
                    {
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task NullPointerAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                unsafe class Type
                {
                    void Method()
                    {
                        Method2([|default(int*)|]);
                    }

                    void Method2(int* value) { }
                    void Method2(byte* value) { }
                }
                """, """
                unsafe class Type
                {
                    void Method()
                    {
                        Method2((int*)null);
                    }

                    void Method2(int* value) { }
                    void Method2(byte* value) { }
                }
                """);

        [Fact]
        public Task PointerInNullableContextAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                #nullable enable

                unsafe class Type
                {
                    void Method()
                    {
                        Method2([|default(int*)|]);
                    }

                    void Method2(int* value) { }
                    void Method2(byte* value) { }
                }
                """,
                FixedCode = """
                #nullable enable

                unsafe class Type
                {
                    void Method()
                    {
                        Method2((int*)null);
                    }

                    void Method2(int* value) { }
                    void Method2(byte* value) { }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();

        [Theory]
        [InlineData("default")]
        [InlineData("default(object)")]
        public Task PreferNullLiteral_DefaultParameterValueAsync(string defaultValueExpression)
            => VerifyCS.VerifyCodeFixAsync($$"""
                class Type
                {
                    void Method(object value = [|{{defaultValueExpression}}|])
                    {
                    }
                }
                """, """
                class Type
                {
                    void Method(object value = null)
                    {
                    }
                }
                """);

        [Fact]
        public Task PreferNullLiteral_ArgumentFormattingAsync()
            => VerifyCS.VerifyCodeFixAsync($$"""
                class Type
                {
                    void Method()
                    {
                        Method2(
                            0,
                            [|default|],
                            /*1*/ [|default|] /*2*/,
                            [|default(object)|],
                            /*1*/ [|default /*2*/ ( /*3*/ object /*4*/ )|] /*5*/,
                            "");
                    }

                    void Method2(params object[] values)
                    {
                    }
                }
                """, """
                class Type
                {
                    void Method()
                    {
                        Method2(
                            0,
                            null,
                            /*1*/ null /*2*/,
                            null,
                            /*1*/  /*3*/  /*4*/ null /*2*/  /*5*/,
                            "");
                    }

                    void Method2(params object[] values)
                    {
                    }
                }
                """);

        [Fact]
        public Task PreferNullLiteral_OverloadResolutionAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                using System;

                class Type
                {
                    void Method()
                    {
                        Method2([|default(object)|]);
                        Method2([|default(string)|]);
                        Method2([|default(IComparable)|]);
                        Method2([|default(int?)|]);
                        Method2(default(int));
                    }

                    void Method2<T>(T value)
                    {
                    }
                }
                """, """
                using System;

                class Type
                {
                    void Method()
                    {
                        Method2((object)null);
                        Method2((string)null);
                        Method2((IComparable)null);
                        Method2((int?)null);
                        Method2(default(int));
                    }

                    void Method2<T>(T value)
                    {
                    }
                }
                """);

        [Fact]
        public Task PreferNullLiteral_ParenthesizeWhereNecessaryAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                using System;

                class Type
                {
                    void Method()
                    {
                        Method2([|default(object)|]?.ToString());
                        Method2([|default(string)|]?.ToString());
                        Method2([|default(IComparable)|]?.ToString());
                        Method2([|default(int?)|]?.ToString());
                        Method2(default(int).ToString());
                    }

                    void Method2(string value)
                    {
                    }
                }
                """, """
                using System;

                class Type
                {
                    void Method()
                    {
                        Method2(((object)null)?.ToString());
                        Method2(((string)null)?.ToString());
                        Method2(((IComparable)null)?.ToString());
                        Method2(((int?)null)?.ToString());
                        Method2(default(int).ToString());
                    }

                    void Method2(string value)
                    {
                    }
                }
                """);

        [Fact]
        public async Task PreferNullLiteral_StructAsync()
        {
            var source = """
                class Type
                {
                    int Method()
                    {
                        return default;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task PreferNullLiteral_StructConvertedToReferenceTypeAsync()
        {
            var source = """
                class Type
                {
                    object Method()
                    {
                        return default(int);
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task PreferNullLiteral_UnconstrainedGenericAsync()
        {
            var source = """
                class Type
                {
                    T Method<T>()
                    {
                        return default;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public Task PreferNullLiteral_GenericConstrainedToReferenceTypeAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                class Type
                {
                    T Method<T>()
                        where T : class
                    {
                        return [|default|];
                    }
                }
                """, """
                class Type
                {
                    T Method<T>()
                        where T : class
                    {
                        return null;
                    }
                }
                """);

        [Fact]
        public async Task PreferNullLiteral_GenericConstrainedToInterfaceAsync()
        {
            var source = """
                class Type
                {
                    T Method<T>()
                        where T : System.IComparable
                    {
                        return default;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task PreferNullLiteral_GenericConstrainedToValueTypeAsync()
        {
            var source = """
                class Type
                {
                    T Method<T>()
                        where T : struct
                    {
                        return default;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Theory]
        [InlineData("object")]
        [InlineData("int?")]
        public async Task IgnoreDefaultParametersAsync(string defaultParameterType)
        {
            var source = $$"""
                class Type
                {
                    void Method1()
                    {
                        Method2(0);
                    }

                    void Method2(int first, {{defaultParameterType}} value = null)
                    {
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }
    }
}
