// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MakeAnonymousFunctionStatic;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeAnonymousFunctionStatic;

using VerifyCS = CSharpCodeFixVerifier<MakeAnonymousFunctionStaticDiagnosticAnalyzer, CSharpMakeAnonymousFunctionStaticCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeAnonymousFunctionStatic)]
public sealed class MakeAnonymousFunctionStaticTests
{
    private static Task TestWithCSharp9Async(string code, string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp9
        }.RunAsync();

    [Theory]
    [InlineData("i => { }")]
    [InlineData("(i) => { }")]
    [InlineData("delegate (int i) { }")]
    public Task TestBelowCSharp9(string anonymousFunctionSyntax)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;

            class C
            {
                void M()
                {
                    N({{anonymousFunctionSyntax}});
                }

                void N(Action<int> a)
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();

    [Theory]
    [InlineData("i => { }")]
    [InlineData("(i) => { }")]
    [InlineData("delegate (int i) { }")]
    public Task TestWithOptionOff(string anonymousFunctionSyntax)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;

            class C
            {
                void M()
                {
                    N({{anonymousFunctionSyntax}});
                }

                void N(Action<int> a)
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.PreferStaticAnonymousFunction, false }
            }
        }.RunAsync();

    [Theory]
    [InlineData("i => { }")]
    [InlineData("(i) => { }")]
    [InlineData("delegate (int i) { }")]
    public async Task TestMissingWhenAlreadyStatic(string anonymousFunctionSyntax)
    {
        var code = $$"""
            using System;
            
            class C
            {
                void M()
                {
                    N(static {{anonymousFunctionSyntax}});
                }
            
                void N(Action<int> a)
                {
                }
            }
            """;

        await TestWithCSharp9Async(code, code);
    }

    [Theory]
    [InlineData("i => { }")]
    [InlineData("(i) => { }")]
    [InlineData("delegate (int i) { }")]
    public Task TestNoCaptures(string anonymousFunctionSyntax)
        => TestWithCSharp9Async($$"""
            using System;
            
            class C
            {
                void M()
                {
                    N([|{{anonymousFunctionSyntax}}|]);
                }
            
                void N(Action<int> a)
                {
                }
            }
            """, $$"""
            using System;
            
            class C
            {
                void M()
                {
                    N(static {{anonymousFunctionSyntax}});
                }
            
                void N(Action<int> a)
                {
                }
            }
            """);

    [Theory]
    [InlineData("i => _field")]
    [InlineData("(i) => _field")]
    [InlineData("delegate (int i) { return _field; }")]
    public async Task TestCapturesThis(string anonymousFunctionSyntax)
    {
        var code = $$"""
            using System;
            
            class C
            {
                private int _field;

                void M()
                {
                    N({{anonymousFunctionSyntax}});
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """;

        await TestWithCSharp9Async(code, code);
    }

    [Theory]
    [InlineData("i => GetValueFromStaticMethod()")]
    [InlineData("(i) => GetValueFromStaticMethod()")]
    [InlineData("delegate (int i) { return GetValueFromStaticMethod(); }")]
    public Task TestNoCaptures_ReferencesStaticMethod(string anonymousFunctionSyntax)
        => TestWithCSharp9Async($$"""
            using System;
            
            class C
            {
                private static int GetValueFromStaticMethod() => 0;

                void M()
                {
                    N([|{{anonymousFunctionSyntax}}|]);
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """, $$"""
            using System;
            
            class C
            {
                private static int GetValueFromStaticMethod() => 0;

                void M()
                {
                    N(static {{anonymousFunctionSyntax}});
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """);

    [Theory]
    [InlineData("i => x")]
    [InlineData("(i) => x")]
    [InlineData("delegate (int i) { return x; }")]
    public async Task TestCapturesParameter(string anonymousFunctionSyntax)
    {
        var code = $$"""
            using System;
            
            class C
            {
                void M(int x)
                {
                    N({{anonymousFunctionSyntax}});
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """;

        await TestWithCSharp9Async(code, code);
    }

    [Theory]
    [InlineData("i => i")]
    [InlineData("(i) => i")]
    [InlineData("delegate (int i) { return i; }")]
    public Task TestNoCaptures_SameFunctionParameterNameAsOuterParameterName(string anonymousFunctionSyntax)
        => TestWithCSharp9Async($$"""
            using System;
            
            class C
            {
                void M(int i)
                {
                    N([|{{anonymousFunctionSyntax}}|]);
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """, $$"""
            using System;
            
            class C
            {
                void M(int i)
                {
                    N(static {{anonymousFunctionSyntax}});
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """);

    [Theory]
    [InlineData("i => x")]
    [InlineData("(i) => x")]
    [InlineData("delegate (int i) { return x; }")]
    public async Task TestCapturesLocal(string anonymousFunctionSyntax)
    {
        var code = $$"""
            using System;
            
            class C
            {
                void M()
                {
                    int x = 0;
                    N({{anonymousFunctionSyntax}});
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """;

        await TestWithCSharp9Async(code, code);
    }

    [Theory]
    [InlineData("i => i")]
    [InlineData("(i) => i")]
    [InlineData("delegate (int i) { return i; }")]
    public Task TestNoCaptures_SameFunctionParameterNameAsOuterLocalName(string anonymousFunctionSyntax)
        => TestWithCSharp9Async($$"""
            using System;
            
            class C
            {
                void M()
                {
                    int i = 0;
                    N([|{{anonymousFunctionSyntax}}|]);
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """, $$"""
            using System;
            
            class C
            {
                void M()
                {
                    int i = 0;
                    N(static {{anonymousFunctionSyntax}});
                }
            
                void N(Func<int, int> f)
                {
                }
            }
            """);

    [Fact]
    public Task TestNestedLambdasWithNoCaptures()
        => TestWithCSharp9Async("""
            using System;
            
            class C
            {
                void M()
                {
                    N([|() =>
                    {
                        Action a = [|() => { }|];
                    }|]);
                }
            
                void N(Action a)
                {
                }
            }
            """, """
            using System;
            
            class C
            {
                void M()
                {
                    N(static () =>
                    {
                        Action a = static () => { };
                    });
                }
            
                void N(Action a)
                {
                }
            }
            """);

    [Fact]
    public Task TestNestedAnonymousMethodsWithNoCaptures()
        => TestWithCSharp9Async("""
            using System;
            
            class C
            {
                void M()
                {
                    N([|delegate ()
                    {
                        Action a = [|delegate () { }|];
                    }|]);
                }
            
                void N(Action a)
                {
                }
            }
            """, """
            using System;
            
            class C
            {
                void M()
                {
                    N(static delegate ()
                    {
                        Action a = static delegate () { };
                    });
                }
            
                void N(Action a)
                {
                }
            }
            """);
}
