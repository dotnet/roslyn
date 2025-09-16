// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddBraces;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
public sealed partial class AddBracesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public AddBracesTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpAddBracesDiagnosticAnalyzer(), new CSharpAddBracesCodeFixProvider());

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForIfWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|if|] (true)
                    {
                        return;
                    }
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForElseWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                    [|else|]
                    {
                        return;
                    }
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForElseWithChildIf(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true)
                        return;
                    [|else|] if (false)
                        return;
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForForWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|for|] (var i = 0; i < 5; i++)
                    {
                        return;
                    }
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForForEachWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|foreach|] (var c in "test")
                    {
                        return;
                    }
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForWhileWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|while|] (true)
                    {
                        return;
                    }
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForDoWhileWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|do|]
                    {
                        return;
                    }
                    while (true);
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForUsingWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|using|] (var f = new Fizz())
                    {
                        return;
                    }
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForUsingWithChildUsing(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|using|] (var f = new Fizz())
                    using (var b = new Buzz())
                        return;
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            class Buzz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForLockWithBraces(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    var str = "test";
                    [|lock|] (str)
                    {
                        return;
                    }
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForLockWithChildLock(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    var str1 = "test";
                    var str2 = "test";
                    [|lock|] (str1)
                        lock (str2)
                            return;
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    public Task DoNotFireForFixedWithChildFixed(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                unsafe static void Main()
                {
                    [|fixed|] (int* p = null)
                    fixed (int* q = null)
                    {
                    }
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForFixedWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                unsafe static void Main()
                {
                    fixed (int* p = null)
                    [|fixed|] (int* q = null)
                        return;
                }
            }
            """,
            """
            class Program
            {
                unsafe static void Main()
                {
                    fixed (int* p = null)
                    fixed (int* q = null)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|if|] (true) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/57770")]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfWithoutBracesTopLevel(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            [|if|] (true) return;
            """,
            """
            if (true)
            {
                return;
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForElseWithoutBracesButHasContextBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true) { return; }
                    [|else|] return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true) { return; }
                    else
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForElseWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true) return;
                    [|else|] return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true) return;
                    else
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForStandaloneElseWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|else|]
                        return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    else
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfNestedInElseWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true) return;
                    else [|if|] (false) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true) return;
                    else if (false)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfNestedInElseWithoutBracesWithMultilineContext1(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true)
                        if (true)   // This multiline statement does not directly impact the other nested statement
                            return;
                        else
                            return;
                    else [|if|] (false) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true)
                        if (true)   // This multiline statement does not directly impact the other nested statement
                            return;
                        else
                            return;
                    else if (false)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfNestedInElseWithoutBracesWithMultilineContext2(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                            return;
                        else
                            return;
                    }
                    else [|if|] (false) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                            return;
                        else
                            return;
                    }
                    else if (false)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfNestedInElseWithoutBracesWithMultilineContext3(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        [|if|] (true)
                            return;
                        else
                            return;
                    }
                    else if (false) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                        else
                            return;
                    }
                    else if (false) return;
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfNestedInElseWithoutBracesWithMultilineContext4(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                            return;
                        [|else|]
                            return;
                    }
                    else if (false) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                            return;
                        else
                        {
                            return;
                        }
                    }
                    else if (false) return;
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

#pragma warning disable CA1200 // Avoid using cref tags with a prefix - Remove the suppression when https://github.com/dotnet/roslyn/issues/42611 is fixed.
    /// <summary>
    /// Verifies that the use of braces in a construct nested within the true portion of an <c>if</c> statement does
    /// not trigger the multiline behavior the <c>else</c> clause of a containing stetemnet for
    /// <see cref="F:Microsoft.CodeAnalysis.CodeStyle.PreferBracesPreference.WhenMultiline"/>. The <c>else</c> clause would only need braces if the true
    /// portion also used braces (which would be required if the true portion was considered multiline.
    /// </summary>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForIfNestedInElseWithoutBracesWithMultilineContext5(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                            if (true) { return; } else { return; }
                        [|else|]
                            return;
                    }
                    else if (false) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                            if (true) { return; } else { return; }
                        else
                        {
                            return;
                        }
                    }
                    else if (false) return;
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForForWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|for|] (var i = 0; i < 5; i++) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    for (var i = 0; i < 5; i++)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForMultilineForWithoutBraces1(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|for|] (var i = 0;
                        i < 5;
                        i++) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    for (var i = 0;
                        i < 5;
                        i++)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForMultilineForWithoutBraces2(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|for|] (var i = 0; i < 5; i++) if (true)
                        return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    for (var i = 0; i < 5; i++)
                    {
                        if (true)
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForMultilineForWithoutBraces3(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|for|] (var i = 0; i < 5; i++)
                        if (true)
                            return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    for (var i = 0; i < 5; i++)
                    {
                        if (true)
                            return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForForEachWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|foreach|] (var c in "test") return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    foreach (var c in "test")
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForWhileWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|while|] (true) return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    while (true)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForDoWhileWithoutBraces1(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|do|] return; while (true);
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    do
                    {
                        return;
                    }
                    while (true);
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForDoWhileWithoutBraces2(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|do|]
                        return;
                    while (true);
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    do
                    {
                        return;
                    }
                    while (true);
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForMultilineDoWhileWithoutBraces1(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|do|] return; while (true ||
                        true);
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    do
                    {
                        return;
                    }
                    while (true ||
                        true);
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForUsingWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|using|] (var f = new Fizz())
                        return;
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    using (var f = new Fizz())
                    {
                        return;
                    }
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForUsingWithoutBracesNestedInUsing(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    using (var f = new Fizz())
                    [|using|] (var b = new Buzz())
                        return;
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            class Buzz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    using (var f = new Fizz())
                    using (var b = new Buzz())
                    {
                        return;
                    }
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            class Buzz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForMultilineUsingWithoutBracesNestedInUsing1(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    using (var f        // <-- This multiline condition doesn't trigger a multiline braces requirement when it's the outer 'using' statement
                        = new Fizz())
                    [|using|] (var b = new Buzz())
                        return;
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            class Buzz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    using (var f        // <-- This multiline condition doesn't trigger a multiline braces requirement when it's the outer 'using' statement
                        = new Fizz())
                    using (var b = new Buzz())
                    {
                        return;
                    }
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            class Buzz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForMultilineUsingWithoutBracesNestedInUsing2(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    using (var f = new Fizz())
                    [|using|] (var b        // <-- This multiline condition triggers a multiline braces requirement because it's the inner 'using' statement
                        = new Buzz())
                        return;
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            class Buzz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    using (var f = new Fizz())
                    using (var b        // <-- This multiline condition triggers a multiline braces requirement because it's the inner 'using' statement
                        = new Buzz())
                    {
                        return;
                    }
                }
            }

            class Fizz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            class Buzz : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForLockWithoutBraces(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    var str = "test";
                    [|lock|] (str)
                        return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    var str = "test";
                    lock (str)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    public Task FireForLockWithoutBracesNestedInLock(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    var str1 = "test";
                    var str2 = "test";

                    lock (str1)
                    [|lock|] (str2) // VS thinks this should be indented one more level
                        return;
                }
            }
            """,

            """
            class Program
            {
                static void Main()
                {
                    var str1 = "test";
                    var str2 = "test";

                    lock (str1)
                    lock (str2) // VS thinks this should be indented one more level
                        {
                            return;
                        }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task DoNotFireForIfWhenIntercedingDirectiveBefore(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
                    [|if (true)|]
            #endif
                        return;
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task DoNotFireForIfWithIntercedingDirectiveAfter(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|if (true)|]
            #if test
                        return;
            #endif
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None)]
    [InlineData((int)PreferBracesPreference.WhenMultiline)]
    [InlineData((int)PreferBracesPreference.Always)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task DoNotFireForIfElseWithIntercedingDirectiveInBoth(int bracesPreference)
        => TestMissingInRegularAndScriptAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|if (true)
            #if test
                        return;
                    else|]
            #endif
                        return;
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferBraces, (PreferBracesPreference)bracesPreference, NotificationOption2.Silent)));

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task OnlyFireForIfWithIntercedingDirectiveInElseAroundIf(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
                    [|if (true)
                        return;
                    else|]
            #endif
                        return;
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
                    if (true)
                    {
                        return;
                    }
                    else
            #endif
                        return;
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task OnlyFireForElseWithIntercedingDirectiveInIfAroundElse(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|if (true)
            #if test
                        return;
                    else|]
                        return;
            #endif
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
                    if (true)
            #if test
                        return;
                    else
                    {
                        return;
                    }
            #endif
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task OnlyFireForElseWithIntercedingDirectiveInIf(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
                    [|if (true)
            #endif
                        return;
                    else|]
                        return;
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
                    if (true)
            #endif
                        return;
                    else
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task OnlyFireForIfWithIntercedingDirectiveInElse(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|if (true)
                        return;
                    else|]
            #if test
                        return;
            #endif
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                    else
            #if test
                        return;
            #endif
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task FireForIfElseWithDirectiveAroundIf(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
                    [|if (true)
                        return;
            #endif
                    else|]
                    {
                        return;
                    }
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
                    if (true)
                    {
                        return;
                    }
            #endif
                    else
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task FireForIfElseWithDirectiveAroundElse(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|if (true)
                    {
                        return;
                    }
            #if test
                    else|]
                        return;
            #endif
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
            #if test
                    else
                    {
                        return;
                    }
            #endif
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task FireForIfWithoutIntercedingDirective(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
            #endif
                    [|if (true)|]
                        return;
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
            #if test
            #endif
                    if (true)
                    {
                        return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task FireForIfWithDirectiveAfterEmbeddedStatement(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|if (true)|]
                        return;
            #if test
            #endif
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
            #if test
            #endif
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, false)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task FireForInnerNestedStatementWhenDirectiveEntirelyInside(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|while (true)
            #if test
                        if (true)|]
                            return;
            #endif
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
                    while (true)
            #if test
                        if (true)
                        {
                            return;
                        }
            #endif
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    [Theory]
    [InlineData((int)PreferBracesPreference.None, false)]
    [InlineData((int)PreferBracesPreference.WhenMultiline, true)]
    [InlineData((int)PreferBracesPreference.Always, true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32480")]
    public Task FireForOuterNestedStatementWhenDirectiveEntirelyInside(int bracesPreference, bool expectDiagnostic)
        => TestAsync(
            """
            #define test
            class Program
            {
                static void Main()
                {
                    [|while (true)
            #if test
                        if (true)|]
            #endif
                            return;
                }
            }
            """,

            """
            #define test
            class Program
            {
                static void Main()
                {
                    while (true)
                    {
            #if test
                        if (true)
            #endif
                            return;
                    }
                }
            }
            """,
            (PreferBracesPreference)bracesPreference,
            expectDiagnostic);

    private async Task TestAsync(string initialMarkup, string expectedMarkup, PreferBracesPreference bracesPreference, bool expectDiagnostic)
    {
        if (expectDiagnostic)
        {
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: Option(CSharpCodeStyleOptions.PreferBraces, bracesPreference, NotificationOption2.Silent)));
        }
        else
        {
            await TestMissingInRegularAndScriptAsync(initialMarkup, new(options: Option(CSharpCodeStyleOptions.PreferBraces, bracesPreference, NotificationOption2.Silent)));
        }
    }
}
