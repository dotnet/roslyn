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
public class MakeAnonymousFunctionStaticTests
{
    private static async Task TestWithCSharp9Async(string code, string fixedCode)
    {
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp9
        }.RunAsync();
    }

    [Theory]
    [InlineData("i => { }")]
    [InlineData("(i) => { }")]
    [InlineData("delegate (int i) { }")]
    public async Task TestBelowCSharp9(string anonymousFunctionSyntax)
    {
        var code = $$"""
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
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();
    }

    [Theory]
    [InlineData("i => { }")]
    [InlineData("(i) => { }")]
    [InlineData("delegate (int i) { }")]
    public async Task TestWithOptionOff(string anonymousFunctionSyntax)
    {
        var code = $$"""
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
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.PreferStaticAnonymousFunction, false }
            }
        }.RunAsync();
    }

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
    public async Task TestNoCaptures(string anonymousFunctionSyntax)
    {
        var code = $$"""
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
            """;

        var fixedCode = $$"""
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

        await TestWithCSharp9Async(code, fixedCode);
    }

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
    public async Task TestNoCaptures_ReferencesStaticMethod(string anonymousFunctionSyntax)
    {
        var code = $$"""
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
            """;

        var fixedCode = $$"""
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
            """;

        await TestWithCSharp9Async(code, fixedCode);
    }

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
    public async Task TestNoCaptures_SameFunctionParameterNameAsOuterParameterName(string anonymousFunctionSyntax)
    {
        var code = $$"""
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
            """;

        var fixedCode = $$"""
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
            """;

        await TestWithCSharp9Async(code, fixedCode);
    }

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
    public async Task TestNoCaptures_SameFunctionParameterNameAsOuterLocalName(string anonymousFunctionSyntax)
    {
        var code = $$"""
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
            """;

        var fixedCode = $$"""
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
            """;

        await TestWithCSharp9Async(code, fixedCode);
    }

    [Fact]
    public async Task TestNestedLambdasWithNoCaptures()
    {
        var code = """
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
            """;

        var fixedCode = """
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
            """;

        await TestWithCSharp9Async(code, fixedCode);
    }

    [Fact]
    public async Task TestNestedAnonymousMethodsWithNoCaptures()
    {
        var code = """
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
            """;

        var fixedCode = """
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
            """;

        await TestWithCSharp9Async(code, fixedCode);
    }
}
