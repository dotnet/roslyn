// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseRecursivePatterns
{
    using VerifyCS = CSharpCodeRefactoringVerifier<UseRecursivePatternsCodeRefactoringProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseRecursivePatterns)]
    public class UseRecursivePatternsRefactoringTests
    {
        private static Task VerifyAsync(
            string initialMarkup,
            string expectedMarkup,
            bool skipCodeActionValidation = false,
            LanguageVersion languageVersion = LanguageVersion.CSharp9)
        {
            return new VerifyCS.Test
            {
                LanguageVersion = languageVersion,
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = skipCodeActionValidation
                    ? CodeActionValidationMode.None
                    : CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        private static Task VerifyMissingAsync(string initialMarkup)
        {
            return VerifyAsync(initialMarkup, initialMarkup);
        }

        [Theory]
        [InlineData("a.b.c.d && a.b.c.a", "a.b.c is { d: n, a: n }")]
        [InlineData("a?.b.c.d && a.b.c.a", "a?.b.c is { d: n, a: n }")]
        [InlineData("a.b?.c.d && a.b.c.a", "a.b?.c is { d: n, a: n }")]
        [InlineData("a.b.c?.d && a.b.c.a", "a.b.c is { d: n, a: n }")]
        [InlineData("a.b?.c?.d && a.b.c.a", "a.b?.c is { d: n, a: n }")]
        [InlineData("a?.b.c?.d && a.b.c.a", "a?.b.c is { d: n, a: n }")]
        [InlineData("a?.b?.c.d && a.b.c.a", "a?.b?.c is { d: n, a: n }")]
        [InlineData("a?.b?.c?.d && a.b.c.a", "a?.b?.c is { d: n, a: n }")]

        [InlineData("a.b.c.d && a.b.a", "a.b is { c: { d: n }, a: n }")]
        [InlineData("a?.b.c.d && a.b.a", "a?.b is { c: { d: n }, a: n }")]
        [InlineData("a.b?.c.d && a.b.a", "a.b is { c: { d: n }, a: n }")]
        [InlineData("a.b.c?.d && a.b.a", "a.b is { c: { d: n }, a: n }")]
        [InlineData("a.b?.c?.d && a.b.a", "a.b is { c: { d: n }, a: n }")]
        [InlineData("a?.b.c?.d && a.b.a", "a?.b is { c: { d: n }, a: n }")]
        [InlineData("a?.b?.c.d && a.b.a", "a?.b is { c: { d: n }, a: n }")]
        [InlineData("a?.b?.c?.d && a.b.a", "a?.b is { c: { d: n }, a: n }")]

        [InlineData("a.b.c.d && a.a", "a is { b: { c: { d: n } }, a: n }")]
        [InlineData("a?.b.c.d && a.a", "a is { b: { c: { d: n } }, a: n }")]
        [InlineData("a.b?.c.d && a.a", "a is { b: { c: { d: n } }, a: n }")]
        [InlineData("a.b.c?.d && a.a", "a is { b: { c: { d: n } }, a: n }")]
        [InlineData("a.b?.c?.d && a.a", "a is { b: { c: { d: n } }, a: n }")]
        [InlineData("a?.b.c?.d && a.a", "a is { b: { c: { d: n } }, a: n }")]
        [InlineData("a?.b?.c.d && a.a", "a is { b: { c: { d: n } }, a: n }")]
        [InlineData("a?.b?.c?.d && a.a", "a is { b: { c: { d: n } }, a: n }")]

        [InlineData("a.b.c.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]
        [InlineData("a?.b.c.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]
        [InlineData("a.b?.c.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]
        [InlineData("a.b.c?.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]
        [InlineData("a.b?.c?.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]
        [InlineData("a?.b.c?.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]
        [InlineData("a?.b?.c.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]
        [InlineData("a?.b?.c?.d && b", "this is { a: { b: { c: { d: n } } }, b: n }")]

        [InlineData("a.b.c.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]
        [InlineData("a?.b.c.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]
        [InlineData("a.b?.c.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]
        [InlineData("a.b.c?.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]
        [InlineData("a.b?.c?.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]
        [InlineData("a?.b.c?.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]
        [InlineData("a?.b?.c.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]
        [InlineData("a?.b?.c?.d && b", "this is { a.b.c.d: n, b: n }", LanguageVersion.CSharp10)]

        [InlineData("a.b.m().d && a.b.m().a", "a.b.m() is { d: n, a: n }")]
        [InlineData("a.m().c.d && a.m().a", "a.m() is { c: { d: n }, a: n }")]
        [InlineData("a?.m().c.d && a?.m().a", "a?.m() is { c: { d: n }, a: n }")]
        [InlineData("a.m()?.c.d && a.m().a", "a.m() is { c: { d: n }, a: n }")]
        [InlineData("a.m().c?.d && a.m().a", "a.m() is { c: { d: n }, a: n }")]
        [InlineData("a.m()?.c?.d && a.m().a", "a.m() is { c: { d: n }, a: n }")]
        [InlineData("a?.m().c?.d && a?.m().a", "a?.m() is { c: { d: n }, a: n }")]
        [InlineData("a?.m()?.c.d && a?.m().a", "a?.m() is { c: { d: n }, a: n }")]
        [InlineData("a?.m()?.c?.d && a?.m().a", "a?.m() is { c: { d: n }, a: n }")]
        public async Task TestLogicalAndExpression_Receiver(string actual, string expected, LanguageVersion languageVersion = LanguageVersion.CSharp9)
        {
            await VerifyAsync(WrapInIfStatement("n == " + actual + " == n", "&&"), WrapInIfStatement(expected), languageVersion: languageVersion);
        }

        [Theory]
        [InlineData("this.P1 < 1 && 2 >= this.P2", "this is { P1: < 1, P2: <= 2 }")]
        [InlineData("this.P1 > 1 && 2 <= this.P2", "this is { P1: > 1, P2: >= 2 }")]
        [InlineData("this.P1 <= 1 && 2 > this.P2", "this is { P1: <= 1, P2: < 2 }")]
        [InlineData("this.P1 >= 1 && 2 < this.P2", "this is { P1: >= 1, P2: > 2 }")]
        // Nested
        [InlineData("this.CP1?.P1 < 1 && 2 >= this.CP2.P2", "this is { CP1: { P1: < 1 }, CP2: { P2: <= 2 } }")]
        [InlineData("this.CP1?.P1 > 1 && 2 <= this.CP2.P2", "this is { CP1: { P1: > 1 }, CP2: { P2: >= 2 } }")]
        [InlineData("this.CP1.P1 <= 1 && 2 > this.CP2?.P2", "this is { CP1: { P1: <= 1 }, CP2: { P2: < 2 } }")]
        [InlineData("this.CP1.P1 >= 1 && 2 < this.CP2?.P2", "this is { CP1: { P1: >= 1 }, CP2: { P2: > 2 } }")]
        public async Task TestLogicalAndExpression_Relational(string actual, string expected)
        {
            await VerifyAsync(WrapInIfStatement(actual, "&&"), WrapInIfStatement(expected));
        }

        [Theory]
        [InlineData("!B1 && B2", "this is { B1: false, B2: true }")]
        [InlineData("CP1.B1 && !CP2.B2", "this is { CP1: { B1: true }, CP2: { B2: false } }")]
        public async Task TestLogicalAndExpression_Boolean(string actual, string expected)
        {
            await VerifyAsync(WrapInIfStatement(actual, "&&"), WrapInIfStatement(expected, "&&"));
        }

        [Theory]
        [InlineData("this.P1 == 1 && 2 == this.P2", "this is { P1: 1, P2: 2 }")]
        [InlineData("this.cf != null && this.cf.C != 0", "this.cf is { C: not 0 }")]
        [InlineData("cf != null && cf.C != 0", "cf is { C: not 0 }")]
        [InlineData("this.P1 != 1 && 2 != this.P2", "this is { P1: not 1, P2: not 2 }")]
        [InlineData("this.CP1.P1 == 1 && 2 == this.CP2.P2", "this is { CP1: { P1: 1 }, CP2: { P2: 2 } }")]
        [InlineData("this.CP1.P1 != 1 && 2 != this.CP2.P2", "this is { CP1: { P1: not 1 }, CP2: { P2: not 2 } }")]
        public async Task TestLogicalAndExpression_Equality(string actual, string expected)
        {
            await VerifyAsync(WrapInIfStatement(actual, "&&"), WrapInIfStatement(expected, "&&"));
        }

        [Theory]
        [InlineData("NS.C.SCP1.P1 == 1 && NS.C.SCP1.P2 == 2", "NS.C.SCP1 is { P1: 1, P2: 2 }")]
        [InlineData("NS.C.SCP1.CP1.P1 == 1 && NS.C.SCP1.CP2.P2 == 2", "NS.C.SCP1 is { CP1: { P1: 1 }, CP2: { P2: 2 } }")]
        public async Task TestLogicalAndExpression_StaticMembers(string actual, string expected)
        {
            await VerifyAsync(WrapInIfStatement(actual, "&&"), WrapInIfStatement(expected, "&&"));
        }

        [Theory]
        [InlineData("this.B1 && this.CP1.P1 == 1 [||]&& this.CP1.CP2.P3 == 3 && B2", "this.B1 && this.CP1 is { P1: 1, CP2: { P3: 3 } } && B2")]
        [InlineData("this.B1 || this.CP1.P1 == 1 [||]&& this.CP1.CP2.P3 == 3 || B2", "this.B1 || this.CP1 is { P1: 1, CP2: { P3: 3 } } || B2")]
        public async Task TestLogicalAndExpression_Chain(string actual, string expected)
        {
            await VerifyAsync(WrapInIfStatement(actual, entry: null), WrapInIfStatement(expected, entry: null));
        }

        [Theory]
        [InlineData("NS.C.SCP1 == null && NS.C.SCP2 == null")]
        [InlineData("NS.C.SCP1.P1 == 1 && NS.C.SCP2.P1 == 2")]
        [InlineData("base.P1 == 1 && base.P2 == 2")]
        [InlineData("base.B1 && base.B2")]
        public async Task TestLogicalAndExpression_Invalid(string actual)
        {
            await VerifyMissingAsync(WrapInIfStatement(actual, "&&"));
        }

        [Theory]
        [InlineData("{ a: var x }", "x is { b: n }", "{ a: { b: n } x }")]
        [InlineData("{ a: C x }", "x is { b: n }", "{ a: C { b: n } x }")]
        [InlineData("{ a: { b: n } x }", "x is C", "{ a: C { b: n } x }")]
        [InlineData("{ a: { b: n } x }", "x is { a: n }", "{ a: { b: n, a: n } x }")]
        [InlineData("{ a: var x }", "x.c is { b: n }", "{ a: { c: { b: n } } x }")]
        [InlineData("{ a: C x }", "x.c is { b: n }", "{ a: C { c: { b: n } } x }")]
        [InlineData("{ a: { b: n } x }", "x.c is C", "{ a: { b: n, c: C } x }", true)]
        [InlineData("{ a: { b: n } x }", "x.c is { a: n }", "{ a: { b: n, c: { a: n } } x }")]
        [InlineData("{ a: var x }", "x == null", "{ a: var x and null }")]
        public async Task TestVariableDesignation(string pattern, string expression, string expected, bool skipCodeActionValidation = false)
        {
            await ValidateAsync(WrapInSwitchArm($"{pattern} when {expression}", "when"), WrapInSwitchArm($"{expected}"));
            await ValidateAsync(WrapInSwitchArm($"{pattern} when {expression}", "=>"), WrapInSwitchArm($"{expected}"));
            await ValidateAsync(WrapInSwitchLabel($"{pattern} when {expression}", "when"), WrapInSwitchLabel($"{expected}"));
            await ValidateAsync(WrapInSwitchLabel($"{pattern} when {expression}", "case"), WrapInSwitchLabel($"{expected}"));
            await ValidateAsync(WrapInIfStatement($"this is {pattern} && {expression}", "&&"), WrapInIfStatement($"this is {expected}"));

            await ValidateAsync(WrapInSwitchArm($"{pattern} when {expression} && B1 && B2", "when"), WrapInSwitchArm($"{expected} when B1 && B2"));
            await ValidateAsync(WrapInSwitchArm($"{pattern} when {expression} && B1 && B2", "=>"), WrapInSwitchArm($"{expected} when B1 && B2"));
            await ValidateAsync(WrapInSwitchLabel($"{pattern} when {expression} && B1 && B2", "when"), WrapInSwitchLabel($"{expected} when B1 && B2"));
            await ValidateAsync(WrapInSwitchLabel($"{pattern} when {expression} && B1 && B2", "case"), WrapInSwitchLabel($"{expected} when B1 && B2"));
            await ValidateAsync(WrapInIfStatement($"B1 && this is {pattern} [||]&& {expression} && B2"), WrapInIfStatement($"B1 && this is {expected} && B2"));

            Task ValidateAsync(string initialMarkup, string expectedMarkup)
                => VerifyAsync(initialMarkup, expectedMarkup, skipCodeActionValidation);
        }

        private static string WrapInIfStatement(string actual, string? entry = null)
        {
            var markup =
@"
            if (" + actual + @") {}
";
            return CreateMarkup(markup, entry);
        }

        private static string WrapInSwitchArm(string actual, string? entry = null)
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

        private static string WrapInSwitchLabel(string actual, string? entry = null)
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

        private static string CreateMarkup(string actual, string? entry = null)
        {
            var markup = @"
namespace NS
{
    class C : B
    {
        void Test()
        {
            " + actual + @"
        }
    }
    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
        public D cf = null;
    }

    class D
    {
        public int C = 0;
    }
}";
            return entry is null ? markup : markup.Replace(entry, "[||]" + entry);
        }
    }
}
