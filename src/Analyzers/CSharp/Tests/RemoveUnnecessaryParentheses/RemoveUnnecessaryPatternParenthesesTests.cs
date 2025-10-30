// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryParentheses;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
public sealed class RemoveUnnecessaryPatternParenthesesTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpRemoveUnnecessaryPatternParenthesesDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryParenthesesCodeFixProvider());

    private async Task TestAsync(string initial, string expected, bool offeredWhenRequireForClarityIsEnabled, int index = 0)
    {
        await TestInRegularAndScriptAsync(initial, expected, new(options: RemoveAllUnnecessaryParentheses, index: index));

        if (offeredWhenRequireForClarityIsEnabled)
        {
            await TestInRegularAndScriptAsync(initial, expected, new(options: RequireAllParenthesesForClarity, index: index));
        }
        else
        {
            await TestMissingAsync(initial, parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }
    }

    internal override bool ShouldSkipMessageDescriptionVerification(DiagnosticDescriptor descriptor)
        => descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary) && descriptor.DefaultSeverity == DiagnosticSeverity.Hidden;

    [Fact]
    public Task TestArithmeticRequiredForClarity2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or $$(b and c);
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or b and c;
                }
            }
            """, parameters: new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));

    [Fact]
    public Task TestLogicalRequiredForClarity1()
        => TestMissingAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or $$(b and c);
                }
            }
            """, new TestParameters(options: RequireOtherBinaryParenthesesForClarity));

    [Fact]
    public Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame1()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or $$(b or c);
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or b or c;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame2()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is $$(a or b) or c;
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or b or c;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestAlwaysUnnecessaryForIsPattern()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is $$(a or b);
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or b;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestAlwaysUnnecessaryForCasePattern()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case $$(a or b):
                            return;
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
                        case a or b:
                            return;
                    }
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestAlwaysUnnecessaryForSwitchArmPattern()
        => TestAsync(
            """
            class C
            {
                int M(object o)
                {
                    return o switch
                    {
                        $$(a or b) => 0,
                    };
                }
            }
            """,
            """
            class C
            {
                int M(object o)
                {
                    return o switch
                    {
                        a or b => 0,
                    };
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestAlwaysUnnecessaryForSubPattern()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is { X: $$(a or b) };
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is { X: a or b };
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestNotAlwaysUnnecessaryForUnaryPattern1()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or $$(not b);
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is a or not b;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: false);

    [Fact]
    public Task TestNotAlwaysUnnecessaryForUnaryPattern2()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is $$(not a) or b;
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    bool x = o is not a or b;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: false);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52589")]
    public Task TestAlwaysNecessaryForDiscard()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(object o)
                {
                    if (o is $$(_))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestUnnecessaryForDiscardInSubpattern()
        => TestAsync(
            """
            class C
            {
                void M(object o)
                {
                    if (o is string { Length: $$(_) })
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
                    if (o is string { Length: _ })
                    {
                    }
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);
}
