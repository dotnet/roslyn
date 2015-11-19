// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected void Test(string initial, string expected, int index = 0, bool compareTokens = true)
        {
            Test(initial, expected, Options.Script, index, compareTokens);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFix1()
        {
            Test(
                @"void Foo() { Bar([|1 + 1|]); Bar(1 + 1); }",
                @"void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(1 + 1); }",
                index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFix2()
        {
            Test(
                @"void Foo() { Bar([|1 + 1|]); Bar(1 + 1); }",
                @"void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(V); }",
                index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFieldFix1()
        {
            var code =
@"int i = ([|1 + 1|]) + (1 + 1);";

            var expected =
@"private const int {|Rename:V|} = 1 + 1;
int i = V + (1 + 1);";

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFieldFix2()
        {
            var code =
@"int i = ([|1 + 1|]) + (1 + 1);";

            var expected =
@"private const int {|Rename:V|} = 1 + 1;
int i = V + V;";

            Test(code, expected, index: 1, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestParameterFix1()
        {
            Test(
                @"void Bar(int i = [|1 + 1|], int j = 1 + 1) { }",
                @"private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = 1 + 1) { }",
                index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestParameterFix2()
        {
            Test(
                @"void Bar(int i = [|1 + 1|], int j = 1 + 1) { }",
                @"private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = V) { }",
                index: 1);
        }

        [WpfFact]
        public void TestAttributeFix1()
        {
            Test(
                @"[Foo([|1 + 1|], 1 + 1)]void Bar() { }",
                @"private const int {|Rename:V|} = 1 + 1; [Foo(V, 1 + 1)]void Bar() { }",
                index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestAttributeFix2()
        {
            Test(
                @"[Foo([|1 + 1|], 1 + 1)]void Bar() { }",
                @"private const int {|Rename:V|} = 1 + 1; [Foo(V, V)]void Bar() { }",
                index: 1);
        }

        [WorkItem(541287)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestBlockFormatting()
        {
            Test(
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
            var {|Rename:v|} = i + 1;
            Console.WriteLine(v);
        }
    }
}
",
index: 1,
compareTokens: false);
        }

        [WorkItem(546465)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestPreserveTrivia()
        {
            Test(
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
