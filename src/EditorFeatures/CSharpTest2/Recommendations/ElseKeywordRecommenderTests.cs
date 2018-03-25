// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ElseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPreprocessor1()
        {
            await VerifyAbsenceAsync(
@"class C {
#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPreprocessorFollowedBySkippedTokens()
        {
            await VerifyKeywordAsync(
@"#if GOO
#$$
dasd
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHash()
        {
            await VerifyKeywordAsync(
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHashAndSpace()
        {
            await VerifyKeywordAsync(
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
$$"));
        }

        class Statements : DataAttribute
        {
            public override IEnumerable<object[]> GetData(MethodInfo testMethod)
                => new[] { new[] { "Console.WriteLine();" }, new[] { "{ }" }, new[] { "while (true) { }" } };
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    {statement}
$$
else"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfNestedIfStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfNestedIfStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$
    else"));
        }

        [WorkItem(25336, "https://github.com/dotnet/roslyn/issues/25336")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfNestedIfElseStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [WorkItem(25336, "https://github.com/dotnet/roslyn/issues/25336")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfNestedIfElseStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$
else"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestNotAfterIfNestedIfElseElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
else
    {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestNotAfterIfStatementElse(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    {statement}
else
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestNotAfterIfElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfElseNestedIfStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        {statement}
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterIfElseNestedIfStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        {statement}
    $$
    else"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestNotAfterIfElseNestedIfElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterWhileIfWhileNestedIfElseStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"while (true)
    if (true)
        while (true)
            if (true)
                Console.WriteLine();
            else
                {statement}
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestAfterWhileIfWhileNestedIfElseStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"while (true)
    if (true)
        while (true)
            if (true)
                Console.WriteLine();
            else
                {statement}
    $$
    else"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [Statements]
        public async Task TestNotAfterWhileIfWhileNestedIfElseElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"while (true)
    if (true)
        while (true)
            if (true)
                Console.WriteLine();
            else
                Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMemberAccess()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)string.$$"));
        }
    }
}
