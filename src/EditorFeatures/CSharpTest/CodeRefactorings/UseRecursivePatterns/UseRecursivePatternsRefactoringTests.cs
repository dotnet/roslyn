// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseRecursivePatterns
{
    using VerifyCS = CSharpCodeRefactoringVerifier<UseRecursivePatternsCodeRefactoringProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseRecursivePatterns)]
    public class UseRecursivePatternsRefactoringTests
    {
        private static Task VerifyAsync(string initialMarkup, string expectedMarkup)
        {
            return new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
            }.RunAsync();
        }

        private static Task VerifyMissingAsync(string initialMarkup)
        {
            return VerifyAsync(initialMarkup, initialMarkup);
        }

        [Theory]
        [InlineData("NS.C.SCP1.P1 == 1 && NS.C.SCP1.P2 == 2", "NS.C.SCP1 is { P1: 1, P2: 2 }")]
        [InlineData("this.P1 == 1 && this.P2 == 2", "this is { P1: 1, P2: 2 }")]
        [InlineData("this.P1 < 1 && this.P2 <= 2", "this is { P1: < 1, P2: <= 2 }")]
        [InlineData("this.P1 > 1 && this.P2 >= 2", "this is { P1: > 1, P2: >= 2 }")]
        [InlineData("this.P1 != 1 && this.P2 != 2", "this is { P1: not 1, P2: not 2 }")]
        [InlineData("this.P1 == 1 && this.CP2.P3 == 3", "this is { P1: 1, CP2: { P3: 3 } }")]
        [InlineData("this.CP1.P1 == 1 && this.CP1.CP2.P3 == 3", "this.CP1 is { P1: 1, CP2: { P3: 3 } }")]
        public async Task TestLogicalAndExpression(string actual, string expected)
        {
            await VerifyAsync(WrapInIfStatement(actual, "&&"), WrapInIfStatement(expected, "&&"));
        }

        [Theory]
        [InlineData("this.CP1 is var c [||]&& c.P1 == 0", "this.CP1 is { P1: 0 } c")]
        [InlineData("this.P1 == 1 && this.CP1 is var c [||]&& c.P1 == 0 && this.P2 == 2", "this.P1 == 1 && this.CP1 is { P1: 0 } c && this.P2 == 2")]
        [InlineData("this.P1 == 1 && this.CP1.P1 == 1 [||]&& this.CP1.CP2.P3 == 3", "this.P1 == 1 && this.CP1 is { P1: 1, CP2: { P3: 3 } }")]
        [InlineData("this.P1 == 1 && this.CP1.P1 == 1 [||]&& this.CP1.CP2.P3 == 3 && this.P2 == 2", "this.P1 == 1 && this.CP1 is { P1: 1, CP2: { P3: 3 } } && this.P2 == 2")]
        public async Task TestLogicalAndExpressionChildren(string actual, string expected)
        {
            await VerifyAsync(WrapInIfStatement(actual, entry: null), WrapInIfStatement(expected, entry: null));
        }

        [Theory]
        [InlineData("NS.C.SCP1 == null && NS.C.SCP2 == null")]
        [InlineData("NS.C.SCP1.P1 == 1 && NS.C.SCP2.P1 == 2")]
        public async Task TestLogicalAndExpressionMissing(string actual)
        {
            await VerifyMissingAsync(WrapInIfStatement(actual, "&&"));
        }

        [Theory]
        [InlineData("{ CP1: var c } when c.P1 is 1", "{ CP1: { P1: 1 } c }")]
        [InlineData("{ CP1: var c } when c.P1 == 1", "{ CP1: { P1: 1 } c }")]
        [InlineData("{ CP1: var c } when c.P1 == 1 && c.P2 == 2", "{ CP1: { P1: 1 } c } when c.P2 == 2")]
        [InlineData("{ CP1: C c } when c.P1 == 1", "{ CP1: C { P1: 1 } c }")]
        [InlineData("{ CP1: C { P2: 2 } c } when c.P1 == 1", "{ CP1: C { P2: 2, P1: 1 } c }")]
        public async Task TestWhenClause(string actual, string expected)
        {
            await VerifyAsync(WrapInSwitchArm(actual, "when"), WrapInSwitchArm(expected, "when"));
            await VerifyAsync(WrapInSwitchArm(actual, "=>"), WrapInSwitchArm(expected, "=>"));
            await VerifyAsync(WrapInSwitchLabel(actual, "when"), WrapInSwitchLabel(expected, "when"));
            await VerifyAsync(WrapInSwitchLabel(actual, "case"), WrapInSwitchLabel(expected, "case"));
        }

        [Theory]
        [InlineData("{ CP1: var c } when c is { P1: 1 }")]
        public async Task TestWhenClauseMissing(string actual)
        {
            await VerifyMissingAsync(WrapInSwitchArm(actual, "when"));
            await VerifyMissingAsync(WrapInSwitchArm(actual, "=>"));
            await VerifyMissingAsync(WrapInSwitchLabel(actual, "when"));
            await VerifyMissingAsync(WrapInSwitchLabel(actual, "case"));
        }

        private static string WrapInIfStatement(string actual, string entry)
        {
            var markup =
@"
            if (" + actual + @") {}
";
            return CreateMarkup(markup, entry);
        }

        private static string WrapInSwitchArm(string actual, string entry)
        {
            var markup =
@"
            _ = this switch
            {
                " + actual + @" => 0
            };
";
            return CreateMarkup(markup, entry);
        }

        private static string WrapInSwitchLabel(string actual, string entry)
        {
            var markup =
@"
            switch (this)
            {
                case " + actual + @":
                    break;
            };
";
            return CreateMarkup(markup, entry);
        }

        private static string CreateMarkup(string actual, string entry)
        {
            var markup = @"
namespace NS
{
    class C
    {
        public int P1, P2, P3;
        public C CP1, CP2, CP3;
        public static C SCP1, SCP2;
        public static int SP1, SP2;

        void M()
        {
            " + actual + @"
        }
    }
}";
            return entry is null ? markup : markup.Replace(entry, "[||]" + entry);
        }
    }
}
