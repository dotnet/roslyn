// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ConstructorInitializerPlacement;

using Verify = CSharpCodeFixVerifier<
    ConstructorInitializerPlacementDiagnosticAnalyzer,
    ConstructorInitializerPlacementCodeFixProvider>;

public sealed class ConstructorInitializerPlacementTests
{
    [Fact]
    public Task TestNotWithOptionOff()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C() :
                    base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestSimpleCase()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C() [|:|]
                    base()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                    : base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNotOnSameLine1()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C() : base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNotOnSameLine2()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                    : base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNotWithColonTrailingComment()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C() : //comment
                    base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestWithCloseParenTrailingComment1()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C() /*comment*/ [|:|]
                    base()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C() /*comment*/ 
                    : base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestWithColonLeadingComment1()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                    // comment
                    [|:|]
                    base()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                    // comment
                    
                    : base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestWithLeadingComment()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C() [|:|]
                    // comment
                    base()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                    // comment
                    : base()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestWithLeadingDirective()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public C() :
            #if true
                    base()
                {
                }
            #endif
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
}
