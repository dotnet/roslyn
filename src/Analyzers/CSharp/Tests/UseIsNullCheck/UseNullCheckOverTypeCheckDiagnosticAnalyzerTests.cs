// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIsNullCheck;

using VerifyCS = CSharpCodeFixVerifier<CSharpUseNullCheckOverTypeCheckDiagnosticAnalyzer, CSharpUseNullCheckOverTypeCheckCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
public sealed class CSharpUseNullCheckOverTypeCheckDiagnosticAnalyzerTests
{
    private static Task VerifyAsync(string source, string fixedSource, LanguageVersion languageVersion)
        => new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = languageVersion,
        }.RunAsync();

    private static async Task VerifyCSharp9Async(string source, string fixedSource)
        => await VerifyAsync(source, fixedSource, LanguageVersion.CSharp9);

    private static async Task VerifyCSharp8Async(string source, string fixedSource)
        => await VerifyAsync(source, fixedSource, LanguageVersion.CSharp8);

    [Fact]
    public async Task TestIsObjectCSharp8()
    {
        var source = """
            public class C
            {
                public bool M(string value)
                {
                    return value is object;
                }
            }
            """;
        await VerifyCSharp8Async(source, source);
    }

    [Fact]
    public Task TestIsObject()
        => VerifyCSharp9Async("""
            public class C
            {
                public bool M(string value)
                {
                    return [|value is object|]/*comment*/;
                }
            }
            """, """
            public class C
            {
                public bool M(string value)
                {
                    return value is not null/*comment*/;
                }
            }
            """);

    [Fact]
    public async Task TestIsObject2()
    {
        var source = """
            public class C
            {
                public bool M(string value)
                {
                    return value is object x;
                }
            }
            """;
        await VerifyCSharp9Async(source, source);
    }

    [Fact]
    public Task TestIsNotObject()
        => VerifyCSharp9Async("""
            public class C
            {
                public bool M(string value)
                {
                    return value is [|not object|];
                }
            }
            """, """
            public class C
            {
                public bool M(string value)
                {
                    return value is null;
                }
            }
            """);

    [Fact]
    public async Task TestIsNotObject2()
    {
        var source = """
            public class C
            {
                public bool M(string value)
                {
                    return value is not object o;
                }
            }
            """;
        await VerifyCSharp9Async(source, source);
    }

    [Fact]
    public async Task TestIsStringAgainstObject_NoDiagnostic()
    {
        var source = """
            public class C
            {
                public bool M(object value)
                {
                    return value is string;
                }
            }
            """;
        await VerifyCSharp9Async(source, source);
    }

    [Fact]
    public Task TestIsStringAgainstString()
        => VerifyCSharp9Async("""
            public class C
            {
                public bool M(string value)
                {
                    return [|value is string|];
                }
            }
            """, """
            public class C
            {
                public bool M(string value)
                {
                    return value is not null;
                }
            }
            """);

    [Fact]
    public async Task TestIsNotStringAgainstObject_NoDiagnostic()
    {
        var source = """
            public class C
            {
                public bool M(object value)
                {
                    return value is string;
                }
            }
            """;
        await VerifyCSharp9Async(source, source);
    }

    [Fact]
    public Task TestIsNotStringAgainstString()
        => VerifyCSharp9Async("""
            public class C
            {
                public bool M(string value)
                {
                    return value is [|not string|];
                }
            }
            """, """
            public class C
            {
                public bool M(string value)
                {
                    return value is null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58377")]
    public async Task TestNotInExpressionTree()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            class SomeClass
            {
                void M()
                {
                    Bar(s => s is object ? 0 : 1);
                }

                private void Bar(Expression<Func<object, int>> p)
                {
                }
            }
            """;
        await VerifyCSharp9Async(source, source);
    }
}
