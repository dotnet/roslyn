// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;
using Microsoft.CodeAnalysis.CSharp.UseInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseObjectInitializer;

// Pass 3b of the IDE0017+IDE0028 unification: a single C# diagnostic analyzer
// (`CSharpUseCollectionInitializerDiagnosticAnalyzer`) registers all three IDs (IDE0017,
// IDE0028, IDE0400) and a single C# fix provider (`CSharpUseInitializerCodeFixProvider`)
// implements all three fixes. The legacy `CSharpUseObjectInitializerDiagnosticAnalyzer`
// alias used here pre-Pass-3b was deleted in this pass.
using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionInitializerDiagnosticAnalyzer,
    CSharpUseInitializerCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
public sealed partial class UseObjectInitializerTests
{
    // Pass 3b of the IDE0017+IDE0028 unification: the shared analyzer registers IDE0017,
    // IDE0028, and IDE0400 descriptors with IDE0028 first. The test framework's default
    // markup-driven expectations resolve to the first descriptor, so every test in this
    // file (all targeting IDE0017 / IDE0400) needs an explicit override. The two
    // <see cref="Test"/> / <see cref="MixedTest"/> wrappers pin the right ID.

    private static DiagnosticDescriptor RequireDescriptor(DiagnosticAnalyzer[] analyzers, string expectedId)
    {
        // If a future refactor drops the expected descriptor from the analyzer's supported
        // list, fail loudly rather than silently letting tests resolve to the first remaining
        // descriptor. Without this, removing the expected ID would appear to keep tests
        // green while quietly changing what they assert.
        var descriptor = analyzers
            .SelectMany(a => a.SupportedDiagnostics)
            .FirstOrDefault(d => d.Id == expectedId);

        return descriptor ?? throw new InvalidOperationException(
            $"{nameof(CSharpUseCollectionInitializerDiagnosticAnalyzer)} did not register the {expectedId} " +
            $"descriptor expected by this test wrapper.");
    }

    private class Test : VerifyCS.Test
    {
        protected override DiagnosticDescriptor? GetDefaultDiagnostic(DiagnosticAnalyzer[] analyzers)
            => RequireDescriptor(analyzers, IDEDiagnosticIds.UseObjectInitializerDiagnosticId);
    }

    private sealed class MixedTest : Test
    {
        protected override DiagnosticDescriptor? GetDefaultDiagnostic(DiagnosticAnalyzer[] analyzers)
            => RequireDescriptor(analyzers, IDEDiagnosticIds.UseMixedObjectAndCollectionInitializerDiagnosticId);
    }

    private static async Task TestMissingInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        LanguageVersion? languageVersion = null)
    {
        var test = new Test
        {
            TestCode = testCode,
        };

        if (languageVersion != null)
            test.LanguageVersion = languageVersion.Value;

        await test.RunAsync();
    }

    [Fact]
    public Task TestOnVariableDeclarator()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestNotForField1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C c = new C();
            }
            """);

    [Fact]
    public Task TestNotForField2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C c = new C() { };
            }
            """);

    [Fact]
    public Task TestNotForField3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C c = new C { };
            }
            """);

    [Fact]
    public Task TestNotForField4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int P;
                C c = new C() { P = 1 };
            }
            """);

    [Fact]
    public Task TestNotForField5()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int P;
                C c = new C { P = 1 };
            }
            """);

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue1Async()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                    c.i = c.i + 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                    c.i = c.i + 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue2Async()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int i;

                void M()
                {
                    var c = new C();
                    c.i = c.i + 1;
                }
            }
            """);

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue3Async()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    C c;
                    c = [|new|] C();
                    [|c.|]i = 1;
                    c.i = c.i + 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    C c;
                    c = new C
                    {
                        i = 1
                    };
                    c.i = c.i + 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue4Async()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int i;

                void M()
                {
                    C c;
                    c = new C();
                    c.i = c.i + 1;
                }
            }
            """);

    [Fact]
    public Task TestOnAssignmentExpression()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = null;
                    c = [|new|] C();
                    [|c.|]i = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = null;
                    c = new C
                    {
                        i = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestStopOnDuplicateMember()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                    c.i = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                    c.i = 2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestComplexInitializer()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = [|new|] C();
                    [|array[0].|]i = 1;
                    [|array[0].|]j = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = new C
                    {
                        i = 1,
                        j = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestNotOnCompoundAssignment()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                    c.j += 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                    c.j += 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializer()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C() { i = 1 };
                    [|c.|]j = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        j = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializerComma()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C()
                    {
                        i = 1,
                    };
                    [|c.|]j = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        j = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializerNotIfAlreadyInitialized()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C()
                    {
                        i = 1,
                    };
                    [|c.|]j = 1;
                    c.i = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        j = 1
                    };
                    c.i = 2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestWithExistingInitializerNotIfAlreadyInitialized_Compound()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C()
                    {
                        i += 1,
                    };
                    [|c.|]j = 1;
                    c.i = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i += 1,
                        j = 1
                    };
                    c.i = 2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentCompoundStatement_FoldsIntoInitializer()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C { i = 1 };
                    [|c.|]j += 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        j += 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentCompoundStatement_PreservesOperatorKind()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;
                int k;
                string s;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i += 1;
                    [|c.|]j -= 2;
                    [|c.|]k *= 3;
                    [|c.|]s ??= "x";
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;
                int k;
                string s;

                void M()
                {
                    var c = new C
                    {
                        i += 1,
                        j -= 2,
                        k *= 3,
                        s ??= "x"
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentCompoundStatement_Event_StackedSubscriptions()
        => new Test
        {
            TestCode = """
            using System;

            class C
            {
                public event EventHandler Click;

                void M(EventHandler h1, EventHandler h2)
                {
                    var c = [|new|] C();
                    [|c.|]Click += h1;
                    [|c.|]Click += h2;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                public event EventHandler Click;

                void M(EventHandler h1, EventHandler h2)
                {
                    var c = new C
                    {
                        Click += h1,
                        Click += h2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentCompoundStatement_NotOfferedBeforePreview()
        => new Test
        {
            // The feature is gated to LanguageVersion.Preview. On C# 14 the resulting initializer
            // would itself be a binder error, so the analyzer must not offer to fold compound-form
            // subsequent statements. The diagnostic still fires on the `new` for the simple `i = 1`
            // case (so the pre-existing simple-fold path stays intact), but `c.j += 1;` is left
            // alone — no `[|c.|]` marker on it.
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                    c.j += 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                    c.j += 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    #region Mixed object/collection initializer (dotnet/csharplang#10185)

    [Fact]
    public Task TestSubsequentAddInvocation_FoldsIntoMixedInitializer()
        // Under the mixed initializer feature, a subsequent `c.Add(value)` expression statement
        // folds into the new object initializer as a bare-element initializer. The synthesized
        // initializer mixes member-shape and element-shape children, so the analyzer routes the
        // diagnostic to IDE0400 — `MixedTest` re-points markup defaults from IDE0017 to IDE0400.
        => new MixedTest
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    [|c.|]Add(10);
                    [|c.|]Add(20);
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1,
                        10,
                        20
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_InterleavedWithMembers_FoldsInLexicalOrder()
        // Ordering matters: an `Add` call between two member writes lands between them inside the
        // synthesized initializer body, preserving the source-textual order of the statements.
        // The synthesis is mixed, so this routes to IDE0400 via `MixedTest`.
        => new MixedTest
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public int Y { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    [|c.|]Add(10);
                    [|c.|]Y = 2;
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public int Y { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1,
                        10,
                        Y = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_StacksOntoExistingMixedInitializer()
        // Same fold extension applies when the original `new C { ... }` already has a member
        // initializer; the subsequent `Add` is appended alongside it. Existing member init plus
        // new Add-shape matches → mixed synthesis → IDE0400 (`MixedTest`).
        => new MixedTest
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C { X = 1 };
                    [|c.|]Add(10);
                    [|c.|]Add(20);
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1,
                        10,
                        20
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_NotOfferedBeforePreview()
        // Pre-Preview: keep the existing IDE0017 behavior; the `c.Add(10);` statement stays as a
        // statement, the simple member-init fold still happens.
        => new Test
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    c.Add(10);
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1
                    };
                    c.Add(10);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_NoIEnumerable_NotFolded()
        // The mixed initializer's element-shape children inherit the collection-initializer
        // precondition: target type must implement IEnumerable. Without it, the synthesized
        // `new C { X = 1, 10 }` would not bind. The analyzer must therefore decline the Add-fold
        // and leave the trailing `c.Add(...)` as a plain statement.
        => new Test
        {
            TestCode = """
            class C
            {
                public int X { get; set; }
                public void Add(int item) { }
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    c.Add(10);
                }
            }
            """,
            FixedCode = """
            class C
            {
                public int X { get; set; }
                public void Add(int item) { }
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1
                    };
                    c.Add(10);
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_NamedArgument_NotFolded()
        // `Add(item: value)` rejected by `IsSimpleArgument` in the underlying helper; the fold
        // stops at the named-arg call. The pre-existing simple member-init fold still happens.
        => new Test
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    c.Add(item: 10);
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1
                    };
                    c.Add(item: 10);
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_StaticHelperCall_NotFolded()
        // `Helper.Add(c, 10)` doesn't have `c` as the syntactic receiver of `.Add`, so the
        // value-pattern check rejects it. The static call stays as-is.
        => new Test
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            static class Helper
            {
                public static void Add(C target, int item) { }
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    Helper.Add(c, 10);
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            static class Helper
            {
                public static void Add(C target, int item) { }
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1
                    };
                    Helper.Add(c, 10);
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_LeadingAddThenMember_FoldsAsMixedInitializer()
        // The Add-shape statement may appear before any member assignment. The wrapper kind is
        // still ObjectInitializerExpression because at least one folded element is assignment-
        // shape, matching the parser's classification of `{ 10, X = 2 }`. Mixed synthesis →
        // IDE0400 (`MixedTest`).
        => new MixedTest
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]Add(10);
                    [|c.|]X = 2;
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        10,
                        X = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_ExtensionAdd_FoldsAsMixedInitializer()
        // C# 6+ recognizes extension `Add` for collection-initializer folding, and IDE0017's
        // analyzer reuses the same shape recognition. The mixed fold must therefore work when
        // the only `Add` is an extension on the receiver. Mixed synthesis → IDE0400
        // (`MixedTest`).
        => new MixedTest
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            static class Extensions
            {
                public static void Add(this C @this, int item) { }
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    [|c.|]Add(10);
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            static class Extensions
            {
                public static void Add(this C @this, int item) { }
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1,
                        10
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_MultiArgAdd_FoldedAsBraceListElement()
        // Pass 3b of the IDE0017+IDE0028 unification consolidated the member-init and
        // collection-init walks. PR 5's single-arg restriction (which only fired in the
        // deleted member-init walk) no longer applies; the unified walk's Add detection
        // reuses IDE0028's matcher, which accepts multi-arg Add and synthesizes a
        // `{ a, b }` brace-list element initializer. Under csharplang#10185 the resulting
        // `new C { X = 1, { 10, 20 } }` binds (the brace-list maps to `Add(10, 20)`), so
        // the analyzer can offer the mixed fold. This test pins the new behavior; the
        // pre-Pass-3b version that asserted the fold was rejected for multi-arg Add is
        // captured in git history under the same test name.
        => new MixedTest
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int a, int b) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    [|c.|]Add(10, 20);
                }
            }
            """,
            FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int a, int b) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1,
                        {
                            10,
                            20
                        }
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_MixedInitializer_SuppressedWhenCollectionPreferenceDisabled()
        // IDE0400 (mixed object/collection initializer) requires BOTH `PreferObjectInitializer`
        // and `PreferCollectionInitializer` to be enabled — disabling either is treated as the
        // user signaling they don't want the corresponding pure form, and the mixed form is a
        // strict superset of both. `TryGetMixedInitializerNotification` returns null when
        // `PreferCollectionInitializer` is false, suppressing the IDE0400 diagnostic without
        // routing back to IDE0017 (which would propose an invalid fix that the user has opted
        // out of by disabling collection-initializer preference).
        => new Test
        {
            TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M()
                {
                    var c = new C();
                    c.X = 1;
                    c.Add(10);
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            Options = { { CodeStyleOptions2.PreferCollectionInitializer, false } },
        }.RunAsync();

    [Fact]
    public Task TestSubsequentAddInvocation_MixedInitializer_MemberOnlyUnderPreview_StillReportsLegacyIDE0017()
        // Regression guard for the routing: under Preview, a pure-member-only fold should
        // still surface IDE0017 (legacy), not IDE0400. The Add-fold path simply doesn't fire
        // because there's no `c.Add(...)` statement to recognize.
        => new Test
        {
            TestCode = """
            class C
            {
                public int X { get; set; }
                public int Y { get; set; }
            }

            class Program
            {
                static void M()
                {
                    var c = [|new|] C();
                    [|c.|]X = 1;
                    [|c.|]Y = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public int X { get; set; }
                public int Y { get; set; }
            }

            class Program
            {
                static void M()
                {
                    var c = new C
                    {
                        X = 1,
                        Y = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    // Pre-Pass-3b this file held `TestSubsequentAddInvocation_PureAddSequence_YieldsToCollectionInitializer`,
    // which verified that the (now-deleted) use-object-initializer analyzer explicitly
    // refused to report on pure-Add sequences so the use-collection-initializer analyzer
    // could own them. Pass 3b collapsed both analyzers into a single
    // <c>CSharpUseCollectionInitializerDiagnosticAnalyzer</c> that picks the right
    // diagnostic ID by synthesis shape, so the "yield to IDE0028" assertion is now a
    // structural invariant of the architecture rather than a behavior worth pinning here.
    // The pure-Add path is covered end-to-end by
    // <c>UseCollectionInitializerTests.TestOnVariableDeclarator_PureAddSequenceUnderPreview</c>.

    #endregion

    [Fact]
    public Task TestSubsequentCompoundStatement_StacksOntoExistingEqualsInitializer()
        // `{ i = 1 } + c.i += 5` is a valid stack per spec ("= before any compound"); the analyzer
        // folds the subsequent compound and continues to fold further unrelated targets.
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C { i = 1 };
                    [|c.|]i += 5;
                    [|c.|]j = 7;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        i += 5,
                        j = 7
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSubsequentCompoundStatement_StopsAfterEqualsAtSubsequentEquals()
        // After `{ i = 1 } + c.i = 5` is invalid (would duplicate `=`), so the analyzer stops at
        // the repeat-name `c.i = 5`. Nothing past that point is folded either; no diagnostic since
        // there's nothing to do.
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C { i = 1 };
                    c.i = 5;
                    c.j = 7;
                }
            }
            """, LanguageVersion.Preview);

    [Fact]
    public Task TestSubsequentCompoundStatement_StopsAfterCompoundAtSubsequentEquals()
        // `{ i += 1 } + c.i = 5` would be `{ i += 1, i = 5 }`, which violates the "= before any
        // compound" ordering rule. The analyzer stops at the offending `c.i = 5`. The earlier
        // existing initializer is left untouched (no further valid fold on either name).
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                int i;

                void M()
                {
                    var c = new C { i += 1 };
                    c.i = 5;
                }
            }
            """, LanguageVersion.Preview);

    [Fact]
    public Task TestSubsequentCompoundStatement_NestedInitializerIsExclusive()
        // `{ Inner = { X = 1 } }` is the spec's exclusive nested-init form. Any further fold on
        // `Inner` (compound or otherwise) is rejected.
        => TestMissingInRegularAndScriptAsync("""
            class Inner
            {
                public int X;
            }

            class C
            {
                public Inner Inner = new Inner();
            }

            class D
            {
                void M()
                {
                    var c = new C { Inner = { X = 1 } };
                    c.Inner = null;
                }
            }
            """, LanguageVersion.Preview);

    [Fact]
    public Task TestMissingBeforeCSharp3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int i;
                int j;

                void M()
                {
                    C c = new C();
                    c.j = 1;
                }
            }
            """, LanguageVersion.CSharp2);

    [Fact]
    public Task TestFixAllInDocument1()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                public C() { }
                public C(System.Action a) { }

                void M()
                {
                    var v = [|new|] C(() => {
                        var v2 = [|new|] C();
                        [|v2.|]i = 1;
                    });
                    [|v.|]j = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                public C() { }
                public C(System.Action a) { }

                void M()
                {
                    var v = new C(() =>
                    {
                        var v2 = new C
                        {
                            i = 1
                        };
                    })
                    {
                        j = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestFixAllInDocument2()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                System.Action j;

                void M()
                {
                    var v = [|new|] C();
                    [|v.|]j = () => {
                        var v2 = [|new|] C();
                        [|v2.|]i = 1;
                    };
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                System.Action j;

                void M()
                {
                    var v = new C
                    {
                        j = () =>
                        {
                            var v2 = new C
                            {
                                i = 1
                            };
                        }
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestFixAllInDocument3()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = [|new|] C();
                    [|array[0].|]i = 1;
                    [|array[0].|]j = 2;
                    array[1] = [|new|] C();
                    [|array[1].|]i = 3;
                    [|array[1].|]j = 4;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = new C
                    {
                        i = 1,
                        j = 2
                    };
                    array[1] = new C
                    {
                        i = 3,
                        j = 4
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestTrivia1()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1; // Goo
                    [|c.|]j = 2; // Bar
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = new C
                    {
                        i = 1, // Goo
                        j = 2 // Bar
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46670")]
    public Task TestTriviaRemoveLeadingBlankLinesForFirstProperty()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = [|new|] C();

                    //Goo
                    [|c.|]i = 1;

                    //Bar
                    [|c.|]j = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = new C
                    {
                        //Goo
                        i = 1,

                        //Bar
                        j = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15459")]
    public Task TestMissingInNonTopLevelObjectInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            class C {
            	int a;
            	C Add(int x) {
            		var c = Add(new int());
            		c.a = 1;
            		return c;
            	}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17853")]
    public Task TestMissingForDynamic()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Dynamic;

            class C
            {
                void Goo()
                {
                    dynamic body = new ExpandoObject();
                    body.content = new ExpandoObject();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public Task TestMissingAcrossPreprocessorDirective()
        => TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public void M()
                {
                    var goo = new Goo();
            #if true
                    goo.Value = "";
            #endif
                }

                public string Value { get; set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public Task TestAvailableInsidePreprocessorDirective()
        => new Test
        {
            TestCode = """
            public class Goo
            {
                public void M()
                {
            #if true
                    var goo = [|new|] Goo();
                    [|goo.|]Value = "";
            #endif
                }

                public string Value { get; set; }
            }
            """,
            FixedCode = """
            public class Goo
            {
                public void M()
                {
            #if true
                    var goo = new Goo
                    {
                        Value = ""
                    };
            #endif
                }

                public string Value { get; set; }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19253")]
    public Task TestKeepBlankLinesAfter()
        => new Test
        {
            TestCode = """
            class Goo
            {
                public int Bar { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    var goo = [|new|] Goo();
                    [|goo.|]Bar = 1;

                    int horse = 1;
                }
            }
            """,
            FixedCode = """
            class Goo
            {
                public int Bar { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    var goo = new Goo
                    {
                        Bar = 1
                    };

                    int horse = 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")]
    public Task TestWithExplicitImplementedInterfaceMembers1()
        => TestMissingInRegularAndScriptAsync(
            """
            interface IExample {
                string Name { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = new C();
                    e.Name = string.Empty;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")]
    public Task TestWithExplicitImplementedInterfaceMembers2()
        => TestMissingInRegularAndScriptAsync(
            """
            interface IExample {
                string Name { get; set; }
                string LastName { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
                public string LastName { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = new C();
                    e.Name = string.Empty;
                    e.LastName = string.Empty;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")]
    public Task TestWithExplicitImplementedInterfaceMembers3()
        => new Test
        {
            TestCode = """
            interface IExample {
                string Name { get; set; }
                string LastName { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
                public string LastName { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = [|new|] C();
                    [|e.|]LastName = string.Empty;
                    e.Name = string.Empty;
                }
            }
            """,
            FixedCode = """
            interface IExample {
                string Name { get; set; }
                string LastName { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
                public string LastName { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = new C
                    {
                        LastName = string.Empty
                    };
                    e.Name = string.Empty;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37675")]
    public Task TestDoNotOfferForUsingDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            class C : System.IDisposable
            {
                int i;

                void M()
                {
                    using var c = new C();
                    c.i = 1;
                }

                public void Dispose()
                {
                }
            }
            """);

    [Fact]
    public Task TestImplicitObject()
        => new Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = [|new|]();
                    [|c.|]i = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = new()
                    {
                        i = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61066")]
    public Task TestInTopLevelStatements()
        => new Test
        {
            TestCode = """
            MyClass cl = [|new|]();
            [|cl.|]MyProperty = 5;

            class MyClass
            {
                public int MyProperty { get; set; }
            }
            """,
            FixedCode = """
            MyClass cl = new()
            {
                MyProperty = 5
            };

            class MyClass
            {
                public int MyProperty { get; set; }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/72094")]
    public async Task TestWithConflictingSeverityConfigurationEntries(bool enabled)
    {
        string testCode, fixedCode;
        if (enabled)
        {
            testCode =
                """
                class C
                {
                    int i;
            
                    void M()
                    {
                        var c = [|new|] C();
                        c.i = 1;
                    }
                }
                """;

            fixedCode =
                """
                class C
                {
                    int i;
            
                    void M()
                    {
                        var c = new C
                        {
                            i = 1
                        };
                    }
                }
                """;
        }
        else
        {
            testCode =
                """
                class C
                {
                    int i;
            
                    void M()
                    {
                        var c = new C();
                        c.i = 1;
                    }
                }
                """;
            fixedCode = testCode;
        }

        var globalConfig =
            $"""
            is_global = true

            dotnet_style_object_initializer = true:suggestion
            dotnet_diagnostic.IDE0017.severity = none

            build_property.EnableCodeStyleSeverity = {enabled}
            """;

        var test = new Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", globalConfig),
                }
            },
            FixedState = { Sources = { fixedCode } },
            LanguageVersion = LanguageVersion.CSharp12,
        };

        await test.RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task TestFallbackSeverityConfiguration(bool enabled)
    {
        var testCode =
            """
            class C
            {
                int i;
            
                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                int i;
            
                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                }
            }
            """;
        await new Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
            is_global = true

            dotnet_style_object_initializer = true
            dotnet_diagnostic.IDE0017.severity = warning

            build_property.EnableCodeStyleSeverity = {enabled}
            """),
                }
            },
            FixedState = { Sources = { fixedCode } },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions1()
        => new Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S = i
                            .ToString();
                        [|c.|]T = i.
                            ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S = i
                                .ToString(),
                            T = i.
                                ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions2()
        => new Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S = i
                            .ToString()
                            .ToString();
                        [|c.|]T = i.
                            ToString().
                            ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S = i
                                .ToString()
                                .ToString(),
                            T = i.
                                ToString().
                                ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions3()
        => new Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S =
                            i.ToString();
                        [|c.|]T =
                            i.ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S =
                                i.ToString(),
                            T =
                                i.ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions4()
        => new Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S =
                            i.ToString()
                             .ToString();
                        [|c.|]T =
                            i.ToString()
                             .ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S =
                                i.ToString()
                                 .ToString(),
                            T =
                                i.ToString()
                                 .ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
}
