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

public class ConstructorInitializerPlacementTests
{
    [Fact]
    public async Task TestNotWithOptionOff()
    {
        var code =
            """
            class C
            {
                public C() :
                    base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleCase()
    {
        var code =
            """
            class C
            {
                public C() [|:|]
                    base()
                {
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C()
                    : base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOnSameLine1()
    {
        var code =
            """
            class C
            {
                public C() : base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOnSameLine2()
    {
        var code =
            """
            class C
            {
                public C()
                    : base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithColonTrailingComment()
    {
        var code =
            """
            class C
            {
                public C() : //comment
                    base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCloseParenTrailingComment1()
    {
        var code =
            """
            class C
            {
                public C() /*comment*/ [|:|]
                    base()
                {
                }
            }
            """;
        var fixedCode =
            """
            class C
            {
                public C() /*comment*/ 
                    : base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithColonLeadingComment1()
    {
        var code =
            """
            class C
            {
                public C()
                    // comment
                    [|:|]
                    base()
                {
                }
            }
            """;
        var fixedCode =
            """
            class C
            {
                public C()
                    // comment
                    
                    : base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithLeadingComment()
    {
        var code =
            """
            class C
            {
                public C() [|:|]
                    // comment
                    base()
                {
                }
            }
            """;
        var fixedCode =
            """
            class C
            {
                public C()
                    // comment
                    : base()
                {
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithLeadingDirective()
    {
        var code =
            """
            class C
            {
                public C() :
            #if true
                    base()
                {
                }
            #endif
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }
}
