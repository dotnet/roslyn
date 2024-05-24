// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertIfToSwitch;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertIfToSwitchCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
public class ConvertIfToSwitchTests
{
    [Fact]
    public async Task TestUnreachableEndPoint()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 || i == 2 || i == 3)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1:
                        case 2:
                        case 3:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReachableEndPoint()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 || i == 2 || i == 3)
                        M(i);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1:
                        case 2:
                        case 3:
                            M(i);
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingOnSubsequentBlock()
    {
        var code = """
            class C
            {
                int M(int i)
                {
                    $$if (i == 3) return 0;
                    { if (i == 6) return 1; }
                    return 2;
                }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestElseBlock_01()
    {
        var source =
            """
            class C
            {
                int {|#0:M|}(int i)
                {
                    $$if (i == 3) return 0;
                    else { if (i == 6) return 1; }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int {|#0:M|}(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(3,9): error CS0161: 'C.M(int)': not all code paths return a value
                    DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("C.M(int)"),
                },
            },
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(3,9): error CS0161: 'C.M(int)': not all code paths return a value
                    DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("C.M(int)"),
                },
            },
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestElseBlock_02()
    {
        var source =
            """
            class C
            {
                int M(int i)
                {
                    $$if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleCases_01()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 || 2 == i || i == 3) M(0);
                    else if (i == 4 || 5 == i || i == 6) M(1);
                    else M(2);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1:
                        case 2:
                        case 3:
                            M(0);
                            break;
                        case 4:
                        case 5:
                        case 6:
                            M(1);
                            break;
                        default:
                            M(2);
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleCases_02_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string s && s.Length > 0) M(0);
                    else if (o is int i && i > 0) M(1);
                    else return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s when s.Length > 0:
                            M(0);
                            break;
                        case int i when i > 0:
                            M(1);
                            break;
                        default:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleCases_02_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string s && s.Length > 0) M(0);
                    else if (o is int i && i > 0) M(1);
                    else return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s when s.Length > 0:
                            M(0);
                            break;
                        case int i when i > 0:
                            M(1);
                            break;
                        default:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExpressionOrder()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (1 == i || i == 2 || 3 == i)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1:
                        case 2:
                        case 3:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestConstantExpression()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    const int A = 1, B = 2, C = 3;
                    $$if (A == i || B == i || C == i)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    const int A = 1, B = 2, C = 3;
                    switch (i)
                    {
                        case A:
                        case B:
                        case C:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingOnNonConstantExpression()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    int A = 1, B = 2, C = 3;
                    $$if (A == i || B == i || C == i)
                        return;
                }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact]
    public async Task TestMissingOnDifferentOperands()
    {
        var source =
            """
            class C
            {
                void M(int i, int j)
                {
                    $$if (i == 5 || 6 == j) {}
                }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact]
    public async Task TestMissingOnSingleCase()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 5) {}
                }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Theory, CombinatorialData]
    public async Task TestIsExpression(
        [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
    {
        var source =
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is int || o is string || o is C)
                        return;
                }
            }
            """;
        var fixedSource = languageVersion switch
        {
            LanguageVersion.CSharp8 =>
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case int _:
                        case string _:
                        case C _:
                            return;
                    }
                }
            }
            """,
            LanguageVersion.CSharp9 =>
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case int:
                        case string:
                        case C:
                            return;
                    }
                }
            }
            """,
            _ => throw ExceptionUtilities.Unreachable(),
        };

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = languageVersion,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestIsPatternExpression_01()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is int i)
                            return;
                        else if (o is string s)
                            return;
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case int i:
                            return;
                        case string s:
                            return;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestIsPatternExpression_02_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string s && s.Length == 5)
                            return;
                        else if (o is int i)
                            return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s when s.Length == 5:
                            return;
                        case int i:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact]
    public async Task TestIsPatternExpression_02_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string s && s.Length == 5)
                            return;
                        else if (o is int i)
                            return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s when s.Length == 5:
                            return;
                        case int i:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact]
    public async Task TestIsPatternExpression_03()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string s && (s.Length > 5 && s.Length < 10))
                            return;
                        else if (o is int i)
                            return;
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s when s.Length > 5 && s.Length < 10:
                            return;
                        case int i:
                            return;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestIsPatternExpression_04()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string s && s.Length > 5 && s.Length < 10)
                            return;
                        else if (o is int i)
                            return;
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s when s.Length > 5 && s.Length < 10:
                            return;
                        case int i:
                            return;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestComplexExpression_01()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string s && s.Length > 5 &&
                                             s.Length < 10)
                        {
                            M(o:   0);

                        }
                        else if (o is int i)
                        {
                            M(o:   0);
                        }
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s when s.Length > 5 && s.Length < 10:
                            M(o: 0);

                            break;
                        case int i:
                            M(o: 0);
                            break;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingIfCaretDoesntIntersectWithTheIfKeyword()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    if $$(i == 3) {}
                }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact]
    public async Task TestKeepBlockIfThereIsVariableDeclaration()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 3)
                    {
                        var x = i;
                    }
                    else if (i == 4)
                    {
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 3:
                            {
                                var x = i;
                                break;
                            }

                        case 4:
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingOnBreak_01()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    while (true)
                    {
                        $$if (i == 5) break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact]
    public async Task TestMissingOnBreak_02()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    while (true)
                    {
                        $$if (i == 5) M({|#0:b|}, i);
                        else break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(
            source,
            // /0/Test0.cs(7,27): error CS0103: The name 'b' does not exist in the current context
            DiagnosticResult.CompilerError("CS0103").WithLocation(0).WithArguments("b"),
            source);
    }

    [Fact]
    public async Task TestNestedBreak()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1)
                    {
                        while (true)
                        {
                            break;
                        }
                    }
                    else if (i == 2)
                    {
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1:
                            while (true)
                            {
                                break;
                            }
                            break;
                        case 2:
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubsequentIfStatements_01()
    {
        var source =
            """
            class C
            {
                int M(int? i)
                {
                    $$if (i == null) return 5;
                    if (i == 0) return 6;
                    return 7;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int? i)
                {
                    switch (i)
                    {
                        case null:
                            return 5;
                        case 0:
                            return 6;
                        default:
                            return 7;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSwitchExpression_01()
    {
        var source =
            """
            class C
            {
                int M(int? i)
                {
                    $$if (i == null) return 5;
                    if (i == 0) return 6;
                    return 7;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int? i)
                {
                    return i switch
                    {
                        null => 5,
                        0 => 6,
                        _ => 7
                    };
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = "SwitchExpression",
        }.RunAsync();
    }

    [Fact]
    public async Task TestSwitchExpression_02()
    {
        var source =
            """
            class C
            {
                int M(int? i)
                {
                    $$if (i == null) { return 5; }
                    if (i == 0) { return 6; }
                    else { return 7; }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int? i)
                {
                    return i switch
                    {
                        null => 5,
                        0 => 6,
                        _ => 7
                    };
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = "SwitchExpression",
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubsequentIfStatements_02()
    {
        var source =
            """
            class C
            {
                int M(int? i)
                {
                    $$if (i == null) return 5;
                    if (i == 0) {}
                    if (i == 1) return 6;
                    return 7;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int? i)
                {
                    switch (i)
                    {
                        case null:
                            return 5;
                        case 0:
                            break;
                    }
                    if (i == 1) return 6;
                    return 7;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubsequentIfStatements_03()
    {
        var source =
            """
            class C
            {
                int {|#0:M|}(int? i)
                {
                    while (true)
                    {
                        $$if (i == null) return 5; else if (i == 1) return 1;
                        if (i == 0) break;
                        if (i == 1) return 6;
                        return 7;
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int {|#0:M|}(int? i)
                {
                    while (true)
                    {
                        switch (i)
                        {
                            case null:
                                return 5;
                            case 1:
                                return 1;
                        }
                        if (i == 0) break;
                        if (i == 1) return 6;
                        return 7;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(3,9): error CS0161: 'C.M(int?)': not all code paths return a value
                    DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("C.M(int?)"),
                },
            },
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(3,9): error CS0161: 'C.M(int?)': not all code paths return a value
                    DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("C.M(int?)"),
                },
            },
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubsequentIfStatements_04()
    {
        var source =
            """
            class C
            {
                string M(object i)
                {
                    $$if (i == null || i as string == "") return null;
                    if ((string)i == "0") return i as string;
                    else return i.ToString();
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                string M(object i)
                {
                    switch (i)
                    {
                        case null:
                        case "":
                            return null;
                        case "0":
                            return i as string;
                        default:
                            return i.ToString();
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubsequentIfStatements_05()
    {
        var source =
            """
            class C
            {
                int M(int i)
                {
                    $$if (i == 10) return 5;
                    if (i == 20) return 6;
                    if (i == i) return 0;
                    return 7;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 10:
                            return 5;
                        case 20:
                            return 6;
                    }
                    if (i == i) return 0;
                    return 7;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubsequentIfStatements_06()
    {
        var source =
            """
            class C
            {
                int M(int i)
                {
                    $$if (i == 10)
                    {
                        return 5;
                    }
                    else if (i == 20)
                    {
                        return 6;
                    }
                    if (i == i) 
                    {
                        return 0;
                    }
                    return 7;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 10:
                            return 5;
                        case 20:
                            return 6;
                    }
                    if (i == i) 
                    {
                        return 0;
                    }
                    return 7;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubsequentIfStatements_07()
    {
        var source =
            """
            class C
            {
                int M(int i)
                {
                    $$if (i == 5)
                    {
                        return 4;
                    }
                    else if (i == 1)
                    {
                        return 1;
                    }

                    if (i == 10)
                    {
                        return 5;
                    }
                    else if (i == i)
                    {
                        return 6;
                    }
                    else
                    {
                        return 0;
                    }
                    return 7;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 5:
                            return 4;
                        case 1:
                            return 1;
                    }

                    if (i == 10)
                    {
                        return 5;
                    }
                    else if (i == i)
                    {
                        return 6;
                    }
                    else
                    {
                        return 0;
                    }
                    return 7;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21109")]
    public async Task TestTrivia1()
    {
        var source =
            """
            class C
            {
                int {|#0:M|}(int x, int z)
                {
            #if TRUE
                    {|#1:Console|}.WriteLine();
            #endif

                    $$if (x == 1)
                    {
                        {|#2:Console|}.WriteLine(x + z);
                    }
                    else if (x == 2)
                    {
                        {|#3:Console|}.WriteLine(x + z);
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int {|#0:M|}(int x, int z)
                {
            #if TRUE
                    {|#1:Console|}.WriteLine();
            #endif

                    switch (x)
                    {
                        case 1:
                            {|#2:Console|}.WriteLine(x + z);
                            break;
                        case 2:
                            {|#3:Console|}.WriteLine(x + z);
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(3,9): error CS0161: 'C.M(int, int)': not all code paths return a value
                    DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("C.M(int, int)"),
                    // /0/Test0.cs(6,9): error CS0103: The name 'Console' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithLocation(1).WithArguments("Console"),
                    // /0/Test0.cs(11,13): error CS0103: The name 'Console' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithLocation(2).WithArguments("Console"),
                    // /0/Test0.cs(15,13): error CS0103: The name 'Console' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithLocation(3).WithArguments("Console"),
                },
            },
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(3,9): error CS0161: 'C.M(int, int)': not all code paths return a value
                    DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("C.M(int, int)"),
                    // /0/Test0.cs(6,9): error CS0103: The name 'Console' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithLocation(1).WithArguments("Console"),
                    // /0/Test0.cs(11,13): error CS0103: The name 'Console' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithLocation(2).WithArguments("Console"),
                    // /0/Test0.cs(15,13): error CS0103: The name 'Console' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithLocation(3).WithArguments("Console"),
                },
            },
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21101")]
    public async Task TestTrivia2()
    {
        var source =
            """
            class C
            {
                int M(int i, string[] args)
                {
                    $$if (/* t0 */args.Length /* t1*/ == /* t2 */ 2)
                        return /* t3 */ 0 /* t4 */; /* t5 */
                    else /* t6 */
                        return /* t7 */ 3 /* t8 */;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int i, string[] args)
                {
                    switch (/* t0 */args.Length /* t1*/ )
                    {
                        case 2:
                            return /* t3 */ 0 /* t4 */; /* t5 */
                        default:
                            return /* t7 */ 3 /* t8 */;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd1_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && i == 2)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp8,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd1_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && i == 2)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case {|#0:1 and 2|}:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    DiagnosticResult.CompilerError("CS8120").WithLocation(0),
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd2_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && i == 2 && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp8,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd2_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && i == 2 && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case {|#0:1 and 2 and 3|}:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    DiagnosticResult.CompilerError("CS8120").WithLocation(0),
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd3_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && i == 2 && (i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp8,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd3_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && i == 2 && (i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case {|#0:1 and 2 and 3|}:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    DiagnosticResult.CompilerError("CS8120").WithLocation(0),
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd4()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && (i == 2) && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp8,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd4_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && (i == 2) && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case {|#0:1 and 2 and 3|}:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    DiagnosticResult.CompilerError("CS8120").WithLocation(0),
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd5()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && (i == 2) && (i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd6()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1) && i == 2 && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd7()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1) && i == 2 && (i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd8()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1) && (i == 2) && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1) && (i == 2) && (i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd10()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 1 && (i == 2 && i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd11()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1 && i == 2) && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd12()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (((i == 1) && i == 2) && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd13()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1 && (i == 2)) && i == 3)
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd14()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1 && (i == 2)) && (i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd15()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1) && ((i == 2) && i == 3))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21360")]
    public async Task TestCompoundLogicalAnd16()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if ((i == 1) && (i == 2 && (i == 3)))
                        return;
                    else if (i == 10)
                        return;
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1 when i == 2 && i == 3:
                            return;
                        case 10:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37035")]
    public async Task TestComplexExpression_02()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    $$if (o is string text &&
                        int.TryParse(text, out var n) &&
                        n < 5 && n > -5)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string text when int.TryParse(text, out var n) && n < 5 && n > -5:
                            break;
                        default:
                            break;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestRange_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (5 >= i && 1 <= i)
                    {
                        return;
                    }
                    else if (7 >= i && 6 <= i)
                    {
                        return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestRange_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (5 >= i && 1 <= i)
                    {
                        return;
                    }
                    else if (7 >= i && 6 <= i)
                    {
                        return;
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case <= 5 and >= 1:
                            return;
                        case <= 7 and >= 6:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestComparison_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (5 >= i || 1 <= i)
                    {
                        return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestComparison_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (5 >= i || 1 <= i)
                    {
                        return;
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case <= 5:
                        case >= 1:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestComparison_SwitchExpression_CSharp9()
    {
        var source =
            """
            class C
            {
                int M(int i)
                {
                    $$if (5 >= i || 1 <= i)
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                int M(int i)
                {
                    return i switch
                    {
                        <= 5 or >= 1 => 1,
                        {|#0:_|} => 2
                    };
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(8,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                    DiagnosticResult.CompilerError("CS8510").WithLocation(0),
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = "SwitchExpression",
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestComplexIf_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i < 10 || 20 < i || (i >= 30 && 40 >= i) || i == 50)
                    {
                        return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestComplexIf_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i < 10 || 20 < i || (i >= 30 && 40 >= i) || i == 50)
                    {
                        return;
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case < 10:
                        case > 20:
                        case {|#0:>= 30 and <= 40|}:
                        case {|#1:50|}:
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    DiagnosticResult.CompilerError("CS8120").WithLocation(0),
                    // /0/Test0.cs(10,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    DiagnosticResult.CompilerError("CS8120").WithLocation(1),
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestComplexIf_Precedence_CSharp9()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    $$if (i == 0 || i is < 10 or > 20 && i is >= 30 or <= 40)
                    {
                        return;
                    }
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 0:
                        case (< 10 or > 20) and (>= 30 or <= 40):
                            return;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInequality()
    {
        var source =
            """
            class C
            {
                void M(int i)
                {
                    [||]if ((i > 123 && i < 456) && i != 0 || i == 10)
                    {
                        return;
                    }
                }
            }
            """;

        var fixedSource =
"""
 class C
 {
     void M(int i)
     {
         switch (i)
         {
             case > 123 and < 456 when i != 0:
             case 10:
                 return;
         }
     }
 }
 """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44278")]
    public async Task TestTopLevelStatement()
    {
        var source = """
            var e = new ET1();

            [||]if (e == ET1.A)
            {
            }
            else if (e == ET1.C)
            {
            }

            enum ET1
            {
                A,
                B,
                C,
            }
            """;

        var fixedSource = """
            var e = new ET1();

            switch (e)
            {
                case ET1.A:
                    break;
                case ET1.C:
                    break;
            }

            enum ET1
            {
                A,
                B,
                C,
            }
            """;

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        };

        test.ExpectedDiagnostics.Add(
            // /0/Test0.cs(2,1): error CS8805: Program using top-level statements must be an executable.
            DiagnosticResult.CompilerError("CS8805").WithSpan(1, 1, 1, 19));

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46863")]
    public async Task CommentsAtTheEndOfBlocksShouldBePlacedBeforeBreakStatements()
    {
        var source = """
            class C
            {
                void M(int p)
                {
                    [||]if (p == 1)
                    {
                        DoA();
                        // Comment about why A doesn't need something here
                    }
                    else if (p == 2)
                    {
                        DoB();
                        // Comment about why B doesn't need something here
                    }
                }

                void DoA() { }
                void DoB() { }
            }
            """;

        var fixedSource = """
            class C
            {
                void M(int p)
                {
                    switch (p)
                    {
                        case 1:
                            DoA();
                            // Comment about why A doesn't need something here
                            break;
                        case 2:
                            DoB();
                            // Comment about why B doesn't need something here
                            break;
                    }
                }

                void DoA() { }
                void DoB() { }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingOnImplicitCastInRelationalPattern()
    {
        var source =
            """
            class C
            {
                void M(char c)
                {
                    $$if (c >= 128 || c == 'a')
                        System.Console.WriteLine(c);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingExpressionOnImplicitCastInRelationalPattern()
    {
        var source =
            """
            class C
            {
                int M(char c)
                {
                    $$if (c >= 128 || c == 'a')
                        return 1;
                    else
                        return 2;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingOnImplicitCastInRangePattern()
    {
        var source =
            """
            class C
            {
                void M(char c)
                {
                    $$if (7 >= c && 6 <= c || c == 'a')
                        System.Console.WriteLine(c);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingOnImplicitCastInConstantPattern()
    {
        var source =
            """
            class C
            {
                void M(char c)
                {
                    $$if (c == 128 || c == 'a')
                        System.Console.WriteLine(c);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExplicitCastInConstantPattern()
    {
        var source =
            """
            class C
            {
                void M(char c)
                {
                    $$if (c == (char)128 || c == 'a')
                        System.Console.WriteLine(c);
                }
            }
            """;

        var fixedSource =
            """
            class C
            {
                void M(char c)
                {
                    switch (c)
                    {
                        case (char)128:
                        case 'a':
                            System.Console.WriteLine(c);
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41131")]
    public async Task MoveTriviaFromElse1()
    {
        var source =
            """
            using System;

            class C
            {
                void M()
                {
                    bool? abc = true;
                    $$if (abc == true)
                    {
                        Console.WriteLine(3);
                    }
                    // some comment here
                    else if (abc == false)
                    {
                        Console.WriteLine(4);
                    }
                    else if (abc is null)
                    {
                        Console.WriteLine(14);
                    }
                }
            }
            """;
        var fixedSource =
            """
            using System;

            class C
            {
                void M()
                {
                    bool? abc = true;
                    switch (abc)
                    {
                        case true:
                            Console.WriteLine(3);
                            break;
                        // some comment here
                        case false:
                            Console.WriteLine(4);
                            break;
                        case null:
                            Console.WriteLine(14);
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41131")]
    public async Task MoveTriviaFromElse2()
    {
        var source =
            """
            using System;

            class C
            {
                void M()
                {
                    bool? abc = true;
                    $$if (abc == true)
                    {
                        Console.WriteLine(3);
                    }
                    // some comment here
                    else if (abc == false)
                    {
                        Console.WriteLine(4);
                    }
                    // other comment
                    else
                    {
                        Console.WriteLine(14);
                    }
                }
            }
            """;
        var fixedSource =
            """
            using System;

            class C
            {
                void M()
                {
                    bool? abc = true;
                    switch (abc)
                    {
                        case true:
                            Console.WriteLine(3);
                            break;
                        // some comment here
                        case false:
                            Console.WriteLine(4);
                            break;
                        // other comment
                        default:
                            Console.WriteLine(14);
                            break;
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }
}
