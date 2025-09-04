// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.IntroduceVariable;

[Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
public sealed class InteractiveIntroduceVariableTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new IntroduceVariableCodeRefactoringProvider();

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => GetNestedActions(actions);

    private Task TestAsync(string initial, string expected, int index = 0)
        => TestAsync(initial, expected, new TestParameters(TestOptions.Script, null, index: index));

    [Fact]
    public Task TestMethodFix1()
        => TestAsync(
            """
            void Goo()
            {
                Bar([|1 + 1|]);
                Bar(1 + 1);
            }
            """,
            """
            void Goo()
            {
                const int {|Rename:V|} = 1 + 1;
                Bar(V);
                Bar(1 + 1);
            }
            """,
            index: 2);

    [Fact]
    public Task TestMethodFix2()
        => TestAsync(
            """
            void Goo()
            {
                Bar([|1 + 1|]);
                Bar(1 + 1);
            }
            """,
            """
            void Goo()
            {
                const int {|Rename:V|} = 1 + 1;
                Bar(V);
                Bar(V);
            }
            """,
            index: 3);

    [Fact]
    public Task TestFieldFix1()
        => TestAsync(@"int i = ([|1 + 1|]) + (1 + 1);", """
            private const int {|Rename:V|} = 1 + 1;
            int i = V + (1 + 1);
            """, index: 0);

    [Fact]
    public Task TestFieldFix2()
        => TestAsync(@"int i = ([|1 + 1|]) + (1 + 1);", """
            private const int {|Rename:V|} = 1 + 1;
            int i = V + V;
            """, index: 1);

    [Fact]
    public Task TestParameterFix1()
        => TestAsync(
            """
            void Bar(int i = [|1 + 1|], int j = 1 + 1)
            {
            }
            """,
            """
            private const int {|Rename:V|} = 1 + 1;

            void Bar(int i = V, int j = 1 + 1)
            {
            }
            """,
            index: 0);

    [Fact]
    public Task TestParameterFix2()
        => TestAsync(
            """
            void Bar(int i = [|1 + 1|], int j = 1 + 1)
            {
            }
            """,
            """
            private const int {|Rename:V|} = 1 + 1;

            void Bar(int i = V, int j = V)
            {
            }
            """,
            index: 1);

    [Fact]
    public Task TestAttributeFix1()
        => TestAsync(
            """
            [Goo([|1 + 1|], 1 + 1)]
            void Bar()
            {
            }
            """,
            """
            private const int {|Rename:V|} = 1 + 1;

            [Goo(V, 1 + 1)]
            void Bar()
            {
            }
            """,
            index: 0);

    [Fact]
    public Task TestAttributeFix2()
        => TestAsync(
            """
            [Goo([|1 + 1|], 1 + 1)]
            void Bar()
            {
            }
            """,
            """
            private const int {|Rename:V|} = 1 + 1;

            [Goo(V, V)]
            void Bar()
            {
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541287")]
    public Task TestBlockFormatting()
        => TestAsync(
            """
            using System;

            class C
            {
                public static void Main()
                {
                    for (int i = 0; i < 10; i++)
                        Console.WriteLine([|i+1|]);
                }
            }
            """,
            """
            using System;

            class C
            {
                public static void Main()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        int {|Rename:value|} = i + 1;
                        Console.WriteLine(value);
                    }
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546465")]
    public Task TestPreserveTrivia()
        => TestAsync(
            """
            class C
            {
                void M(params string[] args)
                {
                    M(
                        "a",
                        [|"b"|],
                        "c");
                }
            }
            """,
            """
            class C
            {
                private const string {|Rename:V|} = "b";

                void M(params string[] args)
                {
                    M(
                        "a",
                        V,
                        "c");
                }
            }
            """);
}
