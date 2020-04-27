// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UsePatternCombinators;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternCombinators
{
    public class CSharpUsePatternCombinatorsDiagnosticAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private static readonly ParseOptions CSharp9 = TestOptions.RegularPreview;

        private static readonly OptionsCollection PreferPatternMatching = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.PreferPatternMatching, true }
        };

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUsePatternCombinatorsDiagnosticAnalyzer(), new CSharpUsePatternCombinatorsCodeFixProvider());

        private Task TestAllMissingOnExpressionAsync(string expression, ParseOptions parseOptions = null, bool enabled = true)
            => TestMissingAsync(FromExpression(expression), new TestParameters(
                parseOptions: parseOptions ?? CSharp9, options: enabled ? PreferPatternMatching : null));

        private Task TestAllAsync(string initialMarkup, string expectedMarkup)
            => TestInRegularAndScriptAsync(initialMarkup, expectedMarkup,
                parseOptions: CSharp9, options: PreferPatternMatching);

        private Task TestAllOnExpressionAsync(string expression, string expected)
            => TestAllAsync(FromExpression(expression), FromExpression(expected));

        private static string FromExpression(string expression)
        {
            const string initialMarkup = @"
using System;
using System.Collections.Generic;
class C
{
    bool field = {|FixAllInDocument:EXPRESSION|};
    bool Method() => EXPRESSION;
    bool Prop1 => EXPRESSION;
    bool Prop2 { get } = EXPRESSION;
    void If() { if (EXPRESSION) ; }
    void Argument1() => Test(EXPRESSION);
    void Argument2() => Test(() => EXPRESSION);
    void Argument3() => Test(_ => EXPRESSION);
    void For() { for (; EXPRESSION; ); }
    void Local() { var local = EXPRESSION; }
    void Conditional() { _ = EXPRESSION ? EXPRESSION : EXPRESSION; }
    void Assignment() { _ = EXPRESSION; }
    void Do() { do ; while (EXPRESSION); }
    void While() { while (EXPRESSION) ; }
    bool When() => o switch { _ when EXPRESSION => EXPRESSION };
    bool Return() { return EXPRESSION; }
    IEnumerable<bool> YieldReturn() { yield return EXPRESSION; }
    Func<object, bool> SimpleLambda() => o => EXPRESSION;
    Func<bool> ParenthesizedLambda() => () => EXPRESSION;
    int i;
    int? nullable;
    object o;
}
";
            return initialMarkup.Replace("EXPRESSION", expression);
        }

        [InlineData("i == 0")]
        [InlineData("i > 0")]
        [InlineData("o is C")]
        [InlineData("o is C c")]
        [InlineData("o != null")]
        [InlineData("!(o is C c)")]
        [InlineData("!(o is null)")]
        [InlineData("o is int ii || o is long jj")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMissingOnExpression(string expression)
        {
            await TestAllMissingOnExpressionAsync(expression);
        }

        [InlineData("o is int ii && o is long jj", "o is int ii and long jj")]
        [InlineData("!(o is C)", "o is not C")]
        [InlineData("!(o is C _)", "o is not C _")]
        [InlineData("i == (0x02 | 0x04) || i != 0", "i is (0x02 | 0x04) or not 0")]
        [InlineData("i == 1 || 2 == i", "i is 1 or 2")]
        [InlineData("i == (short)1 || (short)2 == i", "i is ((short)1) or ((short)2)")]
        [InlineData("nullable == 1 || 2 == nullable", "nullable is 1 or 2")]
        [InlineData("i != 1 || 2 != i", "i is not (1 and 2)")]
        [InlineData("i != 1 && 2 != i", "i is not (1 or 2)")]
        [InlineData("!(i != 1 && 2 != i)", "i is 1 or 2")]
        [InlineData("i < 1 && 2 <= i", "i is < 1 and >= 2")]
        [InlineData("i < 1 && 2 <= i && i is not 0", "i is < 1 and >= 2 and not 0")]
        [InlineData("(int.MaxValue - 1D) < i && i > 0", "i is > (int.MaxValue - 1D) and > 0")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestOnExpression(string expression, string expected)
        {
            await TestAllOnExpressionAsync(expression, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMissingIfDisabled()
        {
            await TestAllMissingOnExpressionAsync("o == 1 || o == 2", enabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMissingOnCSharp8()
        {
            await TestAllMissingOnExpressionAsync("o == 1 || o == 2", parseOptions: TestOptions.Regular8);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMultilineTrivia_01()
        {
            await TestAllAsync(
@"class C
{
    bool M0(int variable)
    {
        return {|FixAllInDocument:variable == 0 || /*1*/
               variable == 1 || /*2*/
               variable == 2|}; /*3*/
    }
    bool M1(int variable)
    {
        return variable != 0 && /*1*/
               variable != 1 && /*2*/
               variable != 2; /*3*/
    }
}",
@"class C
{
    bool M0(int variable)
    {
        return variable is 0 or /*1*/
               1 or /*2*/
               2; /*3*/
    }
    bool M1(int variable)
    {
        return variable is not (0 or /*1*/
               1 or /*2*/
               2); /*3*/
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMultilineTrivia_02()
        {
            await TestAllAsync(
@"class C
{
    bool M0(int variable)
    {
        return {|FixAllInDocument:variable == 0 /*1*/
            || variable == 1 /*2*/
            || variable == 2|}; /*3*/
    }
    bool M1(int variable)
    {
        return variable != 0 /*1*/
            && variable != 1 /*2*/
            && variable != 2; /*3*/
    }
}",
@"class C
{
    bool M0(int variable)
    {
        return variable is 0 /*1*/
            or 1 /*2*/
            or 2; /*3*/
    }
    bool M1(int variable)
    {
        return variable is not (0 /*1*/
            or 1 /*2*/
            or 2); /*3*/
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestParenthesized()
        {
            await TestAllAsync(
@"class C
{
    bool M0(int v)
    {
        return {|FixAllInDocument:(v == 0 || v == 1 || v == 2)|};
    }
    bool M1(int v)
    {
        return (v == 0) || (v == 1) || (v == 2);
    }
}",
@"class C
{
    bool M0(int v)
    {
        return (v is 0 or 1 or 2);
    }
    bool M1(int v)
    {
        return v is 0 or 1 or 2;
    }
}");
        }
    }
}
