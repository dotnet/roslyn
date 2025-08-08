// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeLocalFunctionStatic;

public sealed partial class MakeLocalFunctionStaticTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new MakeLocalFunctionStaticDiagnosticAnalyzer(), new MakeLocalFunctionStaticCodeFixProvider());

    private static readonly ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);
    private static readonly ParseOptions CSharp8ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestAboveCSharp8()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
    public Task TestWithOptionOff()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
new TestParameters(
parseOptions: CSharp8ParseOptions,
options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.FalseWithSilentEnforcement)));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingIfAlreadyStatic()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    static int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingPriorToCSharp8()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharp72ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingIfCapturesValue()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M(int i)
                {
                    int [||]fibonacci(int n)
                    {
                        return i <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingIfCapturesThis()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    int [||]fibonacci(int n)
                    {
                        M();
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestAsyncFunction()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    async Task<int> [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : await fibonacci(n - 1) + await fibonacci(n - 2);
                    }
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    static async Task<int> fibonacci(int n)
                    {
                        return n <= 1 ? n : await fibonacci(n - 1) + await fibonacci(n - 2);
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [InlineData("")]
    [InlineData("\r\n")]
    [InlineData("\r\n\r\n")]
    public Task TestLeadingTriviaAfterSemicolon(string leadingTrivia)
        => TestInRegularAndScriptAsync(
            $$"""
            using System;

            class C
            {
                void M()
                {
                    int x;{{leadingTrivia}}
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            $$"""
            using System;

            class C
            {
                void M()
                {
                    int x;{{leadingTrivia}}
                    static int fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [InlineData("")]
    [InlineData("\r\n")]
    [InlineData("\r\n\r\n")]
    public Task TestLeadingTriviaAfterOpenBrace(string leadingTrivia)
        => TestInRegularAndScriptAsync(
            $$"""
            using System;

            class C
            {
                void M()
                {{{leadingTrivia}}
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            $$"""
            using System;

            class C
            {
                void M()
                {{{leadingTrivia}}
                    static int fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [InlineData("")]
    [InlineData("\r\n")]
    [InlineData("\r\n\r\n")]
    public Task TestLeadingTriviaAfterLocalFunction(string leadingTrivia)
        => TestInRegularAndScriptAsync(
            $$"""
            using System;

            class C
            {
                void M()
                {
                    bool otherFunction()
                    {
                        return true;
                    }{{leadingTrivia}}
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            $$"""
            using System;

            class C
            {
                void M()
                {
                    bool otherFunction()
                    {
                        return true;
                    }{{leadingTrivia}}
                    static int fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [InlineData("")]
    [InlineData("\r\n")]
    [InlineData("\r\n\r\n")]
    public Task TestLeadingTriviaAfterExpressionBodyLocalFunction(string leadingTrivia)
        => TestInRegularAndScriptAsync(
            $$"""
            using System;

            class C
            {
                void M()
                {
                    bool otherFunction() => true;{{leadingTrivia}}
                    int [||]fibonacci(int n) => n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                }
            }
            """,
            $$"""
            using System;

            class C
            {
                void M()
                {
                    bool otherFunction() => true;{{leadingTrivia}}
                    static int fibonacci(int n) => n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [InlineData("")]
    [InlineData("\r\n")]
    [InlineData("\r\n\r\n")]
    public Task TestLeadingTriviaAfterComment(string leadingTrivia)
        => TestInRegularAndScriptAsync(
            $$"""
            using System;

            class C
            {
                void M()
                {
                    //Local function comment{{leadingTrivia}}
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            $$"""
            using System;

            class C
            {
                void M()
                {
                    //Local function comment{{leadingTrivia}}
                    static int fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [InlineData("\r\n")]
    [InlineData("\r\n\r\n")]
    public Task TestLeadingTriviaBeforeComment(string leadingTrivia)
        => TestInRegularAndScriptAsync(
            $$"""
            using System;

            class C
            {
                void M()
                {{{leadingTrivia}}
                    //Local function comment
                    int [||]fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            $$"""
            using System;

            class C
            {
                void M()
                {{{leadingTrivia}}
                    //Local function comment
                    static int fibonacci(int n)
                    {
                        return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46858")]
    public Task TestMissingIfAnotherLocalFunctionCalled()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    void [||]A()
                    {
                        B();
                    }

                    void B()
                    {
                    }
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestCallingStaticLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    void [||]A()
                    {
                        B();
                    }

                    static void B()
                    {
                    }
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    static void A()
                    {
                        B();
                    }

                    static void B()
                    {
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestCallingNestedLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    void [||]A()
                    {
                        B();

                        void B()
                        {
                        }
                    }
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    static void A()
                    {
                        B();

                        void B()
                        {
                        }
                    }
                }
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/53179")]
    public Task TestLocalFunctionAsTopLevelStatement()
        => TestAsync("""
            void [||]A()
            {
            }
            """, """
            static void A()
            {
            }
            """,
            new(parseOptions: CSharp8ParseOptions));

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/59286")]
    public Task TestUnsafeLocalFunction()
        => TestAsync("""
            unsafe void [||]A()
            {
            }
            """, """
            static unsafe void A()
            {
            }
            """,
            new(parseOptions: CSharp8ParseOptions));
}
