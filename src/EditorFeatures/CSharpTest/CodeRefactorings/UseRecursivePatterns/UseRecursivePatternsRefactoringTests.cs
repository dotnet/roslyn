// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseRecursivePatterns
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
    public class UseRecursivePatternsRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseRecursivePatternsCodeRefactoringProvider();

        private static string CreateExpressionMarkup(string actual)
        {
            return @"
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
            if (" + actual + @") {}
        }
    }
}";
        }

        [Theory]
        [InlineData("NS.C.SCP1.P1 == 1 [||]&& NS.C.SCP1.P2 == 2", "NS.C.SCP1 is { P1: 1, P2: 2 }")]
        [InlineData("SCP1.SCP2.CP1 == null [||]&& SCP1.SCP2.CP2 == null", "SCP1.SCP2 is { CP1: null, CP2: null }")]
        [InlineData("this.P1 == 1 [||]&& this.P2 == 2", "this is { P1: 1, P2: 2 }")]
        [InlineData("this.P1 < 1 [||]&& this.P2 <= 2", "this is { P1: < 1, P2: <= 2 }")]
        [InlineData("this.P1 > 1 [||]&& this.P2 >= 2", "this is { P1: > 1, P2: >= 2 }")]
        [InlineData("this.P1 != 1 [||]&& this.P2 != 2", "this is { P1: not 1, P2: not 2 }")]
        [InlineData("this.P1 == 1 [||]&& this.CP2.P3 == 3", "this is { P1: 1, CP2: { P3: 3 } }")]
        [InlineData("this.CP1.P1 == 1 [||]&& this.CP1.CP2.P3 == 3", "this.CP1 is { P1: 1, CP2: { P3: 3 } }")]
        public async Task TestLogicalAndExpression(string actual, string expected)
        {
            var initialMarkup = CreateExpressionMarkup(actual);
            var expectedMarkup = CreateExpressionMarkup(expected);

            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Theory]
        [InlineData("SCP1.SCP2.CP1 == null [||]&& SCP1.SCP2 == null")]
        [InlineData("NS.C.SCP1 == null [||]&& NS.C.SCP2 == null")]
        [InlineData("NS.C.SCP1.P1 == 1 [||]&& NS.C.SCP2.P1 == 2")]
        public async Task TestLogicalAndExpressionMissing(string actual)
        {
            var initialMarkup = CreateExpressionMarkup(actual);

            await TestMissingInRegularAndScriptAsync(initialMarkup);
        }

        [Theory]
        [InlineData("{ CP1: var c } [||]when c.P1 == 1", "{ CP1: { P1: 1 } c }")]
        public async Task TestWhenClause(string actual, string expected)
        {
            var initialMarkup = CreateSwitchExpressionMarkup(actual);
            var expectedMarkup = CreateSwitchExpressionMarkup(expected);

            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        private static string CreateSwitchExpressionMarkup(string actual)
        {
            return @"
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
            _ = this switch
            {
                 " + actual + @" => 0
            };
        }
    }
}";
        }
    }
}
