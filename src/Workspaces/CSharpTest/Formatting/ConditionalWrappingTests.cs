// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions2;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class ConditionalWrappingTests : CSharpFormattingTestBase
{
    private readonly OptionsCollection WrapConditionalExpressionsEnabled = new(LanguageNames.CSharp)
    {
        { WrapConditionalExpressions, true }
    };

    private readonly OptionsCollection WrapConditionalExpressionsDisabled = new(LanguageNames.CSharp)
    {
        { WrapConditionalExpressions, false }
    };

    private readonly OptionsCollection WrapAndIndentConditionalExpressionsEnabled = new(LanguageNames.CSharp)
    {
        { WrapConditionalExpressions, true },
        { IndentWrappedConditionalExpressions, true }
    };

    private readonly OptionsCollection IndentWrappedConditionalExpressionsOnly = new(LanguageNames.CSharp)
    {
        { WrapConditionalExpressions, false },
        { IndentWrappedConditionalExpressions, true }
    };

    [Fact]
    public async Task TestSimpleIfCondition_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                        && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (condition1 && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestSimpleIfCondition_WrapDisabled()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                void M()
                {
                    if (condition1 && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsDisabled);
    }

    [Fact]
    public async Task TestComplexIfCondition_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                        && condition2
                        && (condition3
                            || condition4)
                        || condition5)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (condition1 && condition2 && (condition3 || condition4) || condition5)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestComplexIfCondition_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                            && condition2
                            && (condition3
                                    || condition4)
                            || condition5)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (condition1 && condition2 && (condition3 || condition4) || condition5)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestIndentOnly_NoWrapping()
    {
        // When wrapping is disabled, indentation should have no effect
        await AssertNoFormattingChangesAsync("""
            class C
            {
                void M()
                {
                    if (condition1 && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, IndentWrappedConditionalExpressionsOnly);
    }

    [Fact]
    public async Task TestWhileCondition_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    while (condition1
                        && condition2
                        || condition3)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    while (condition1 && condition2 || condition3)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestWhileCondition_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    while (condition1
                            && condition2
                            || condition3)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    while (condition1 && condition2 || condition3)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestForCondition_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    for (int i = 0; i < 10
                        && condition1
                        && condition2; i++)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    for (int i = 0; i < 10 && condition1 && condition2; i++)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestForCondition_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    for (int i = 0; i < 10
                            && condition1
                            && condition2; i++)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    for (int i = 0; i < 10 && condition1 && condition2; i++)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestDoWhileCondition_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    do
                    {
                        DoSomething();
                    } while (condition1
                        && condition2
                        || condition3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    do
                    {
                        DoSomething();
                    } while (condition1 && condition2 || condition3);
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestDoWhileCondition_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    do
                    {
                        DoSomething();
                    } while (condition1
                            && condition2
                            || condition3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    do
                    {
                        DoSomething();
                    } while (condition1 && condition2 || condition3);
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestTernaryCondition_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = condition1
                        && condition2
                        || condition3 ? value1 : value2;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = condition1 && condition2 || condition3 ? value1 : value2;
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestTernaryCondition_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = condition1
                            && condition2
                            || condition3 ? value1 : value2;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = condition1 && condition2 || condition3 ? value1 : value2;
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestNestedConditions_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (outerCondition1
                        && outerCondition2)
                    {
                        if (innerCondition1
                            || innerCondition2
                            && innerCondition3)
                        {
                            DoSomething();
                        }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (outerCondition1 && outerCondition2)
                    {
                        if (innerCondition1 || innerCondition2 && innerCondition3)
                        {
                            DoSomething();
                        }
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestNestedConditions_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (outerCondition1
                            && outerCondition2)
                    {
                        if (innerCondition1
                                || innerCondition2
                                && innerCondition3)
                        {
                            DoSomething();
                        }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (outerCondition1 && outerCondition2)
                    {
                        if (innerCondition1 || innerCondition2 && innerCondition3)
                        {
                            DoSomething();
                        }
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestMixedBinaryOperators_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                        && condition2
                        || condition3
                        && condition4)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (condition1 && condition2 || condition3 && condition4)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestMixedBinaryOperators_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                            && condition2
                            || condition3
                            && condition4)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (condition1 && condition2 || condition3 && condition4)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestNonConditionalBinaryExpression_NoChange()
    {
        // Binary expressions not in conditionals should not be affected
        await AssertNoFormattingChangesAsync("""
            class C
            {
                void M()
                {
                    var result = value1 + value2 * value3;
                    var comparison = x > y && z < w;
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestComplexNestedParentheses_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if ((condition1
                        && condition2)
                        || (condition3
                            && (condition4
                                || condition5)))
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if ((condition1 && condition2) || (condition3 && (condition4 || condition5)))
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestComplexNestedParentheses_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if ((condition1
                            && condition2)
                            || (condition3
                                && (condition4
                                        || condition5)))
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if ((condition1 && condition2) || (condition3 && (condition4 || condition5)))
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapOnly()
    {
        // Test that the EditorConfig option works properly
        var editorConfig = """
            [*.cs]
            csharp_wrap_conditional_expressions = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                        && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (condition1 && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapAndIndent()
    {
        // Test that both EditorConfig options work properly
        var editorConfig = """
            [*.cs]
            csharp_wrap_conditional_expressions = true
            csharp_indent_wrapped_conditional_expressions = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                            && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (condition1 && condition2)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestLongConditionLines()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (veryLongConditionNameThatExceedsReasonableLength1
                        && veryLongConditionNameThatExceedsReasonableLength2
                        && veryLongConditionNameThatExceedsReasonableLength3)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (veryLongConditionNameThatExceedsReasonableLength1 && veryLongConditionNameThatExceedsReasonableLength2 && veryLongConditionNameThatExceedsReasonableLength3)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestLongConditionLines_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (veryLongConditionNameThatExceedsReasonableLength1
                            && veryLongConditionNameThatExceedsReasonableLength2
                            && veryLongConditionNameThatExceedsReasonableLength3)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (veryLongConditionNameThatExceedsReasonableLength1 && veryLongConditionNameThatExceedsReasonableLength2 && veryLongConditionNameThatExceedsReasonableLength3)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestMethodCallsInConditions_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (SomeMethod()
                        && AnotherMethod()
                        || ThirdMethod())
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (SomeMethod() && AnotherMethod() || ThirdMethod())
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestMethodCallsInConditions_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (SomeMethod()
                            && AnotherMethod()
                            || ThirdMethod())
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (SomeMethod() && AnotherMethod() || ThirdMethod())
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestPropertyAccessInConditions_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (someObject.Property1
                        && someObject.Property2
                        || someObject.Property3)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (someObject.Property1 && someObject.Property2 || someObject.Property3)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestPropertyAccessInConditions_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (someObject.Property1
                            && someObject.Property2
                            || someObject.Property3)
                    {
                        DoSomething();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (someObject.Property1 && someObject.Property2 || someObject.Property3)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestExistingIndentationPreserved()
    {
        // Test that existing proper indentation is preserved when wrapping is disabled
        await AssertNoFormattingChangesAsync("""
            class C
            {
                void M()
                {
                    if (condition1
                        && condition2
                        && condition3)
                    {
                        DoSomething();
                    }
                }
            }
            """, WrapConditionalExpressionsDisabled);
    }

    [Fact]
    public async Task TestRealWorldExample_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (string.IsNullOrEmpty(userName)
                        || string.IsNullOrEmpty(password)
                        || (!isAdmin
                            && !hasPermission))
                    {
                        throw new UnauthorizedAccessException();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password) || (!isAdmin && !hasPermission))
                    {
                        throw new UnauthorizedAccessException();
                    }
                }
            }
            """, WrapConditionalExpressionsEnabled);
    }

    [Fact]
    public async Task TestRealWorldExample_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (string.IsNullOrEmpty(userName)
                            || string.IsNullOrEmpty(password)
                            || (!isAdmin
                                && !hasPermission))
                    {
                        throw new UnauthorizedAccessException();
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password) || (!isAdmin && !hasPermission))
                    {
                        throw new UnauthorizedAccessException();
                    }
                }
            }
            """, WrapAndIndentConditionalExpressionsEnabled);
    }
} 