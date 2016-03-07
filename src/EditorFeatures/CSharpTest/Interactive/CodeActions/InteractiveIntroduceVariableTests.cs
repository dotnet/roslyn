// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.IntroduceVariable;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.IntroduceVariable
{
    public class InteractiveIntroduceVariableTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new IntroduceVariableCodeRefactoringProvider();
        }

        protected Task TestAsync(string initial, string expected, int index = 0, bool compareTokens = true)
        {
            return TestAsync(initial, expected, Options.Script, index, compareTokens);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix1()
        {
            await TestAsync(
                @"void Foo() { Bar([|1 + 1|]); Bar(1 + 1); }",
                @"void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(1 + 1); }",
                index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix2()
        {
            await TestAsync(
                @"void Foo() { Bar([|1 + 1|]); Bar(1 + 1); }",
                @"void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(V); }",
                index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldFix1()
        {
            var code =
@"int i = ([|1 + 1|]) + (1 + 1);";

            var expected =
@"private const int {|Rename:V|} = 1 + 1;
int i = V + (1 + 1);";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldFix2()
        {
            var code =
@"int i = ([|1 + 1|]) + (1 + 1);";

            var expected =
@"private const int {|Rename:V|} = 1 + 1;
int i = V + V;";

            await TestAsync(code, expected, index: 1, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestParameterFix1()
        {
            await TestAsync(
                @"void Bar(int i = [|1 + 1|], int j = 1 + 1) { }",
                @"private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = 1 + 1) { }",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestParameterFix2()
        {
            await TestAsync(
                @"void Bar(int i = [|1 + 1|], int j = 1 + 1) { }",
                @"private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = V) { }",
                index: 1);
        }

        [Fact]
        public async Task TestAttributeFix1()
        {
            await TestAsync(
                @"[Foo([|1 + 1|], 1 + 1)]void Bar() { }",
                @"private const int {|Rename:V|} = 1 + 1; [Foo(V, 1 + 1)]void Bar() { }",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestAttributeFix2()
        {
            await TestAsync(
                @"[Foo([|1 + 1|], 1 + 1)]void Bar() { }",
                @"private const int {|Rename:V|} = 1 + 1; [Foo(V, V)]void Bar() { }",
                index: 1);
        }

        [WorkItem(541287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541287")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestBlockFormatting()
        {
            await TestAsync(
@"using System;
 
class C
{
    public static void Main()
    {
        for (int i = 0; i < 10; i++)
            Console.WriteLine([|i+1|]);
    }
}
",
@"using System;
 
class C
{
    public static void Main()
    {
        for (int i = 0; i < 10; i++)
        {
            int {|Rename:v|} = i + 1;
            Console.WriteLine(v);
        }
    }
}
",
index: 1,
compareTokens: false);
        }

        [WorkItem(546465, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546465")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreserveTrivia()
        {
            await TestAsync(
@"class C
{
    void M(params string[] args)
    {
        M(
            ""a"",
            [|""b""|],
            ""c"");
    }
}
",
@"class C
{
    private const string {|Rename:V|} = ""b"";

    void M(params string[] args)
    {
        M(
            ""a"",
            V,
            ""c"");
    }
}
",
compareTokens: false);
        }
    }
}
